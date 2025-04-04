/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class PowertoolsLogger. This class cannot be inherited.
///     Implements the <see cref="Microsoft.Extensions.Logging.ILogger" />
/// </summary>
/// <seealso cref="Microsoft.Extensions.Logging.ILogger" />
internal sealed class PowertoolsLogger : ILogger
{
    /// <summary>
    ///     The name
    /// </summary>
    private readonly string _categoryName;

    /// <summary>
    ///     The current configuration
    /// </summary>
    private readonly Func<PowertoolsLoggerConfiguration> _currentConfig;

    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The current scope
    /// </summary>
    internal PowertoolsLoggerScope CurrentScope { get; private set; }

    /// <summary>
    ///     Private constructor - Is initialized on CreateLogger
    /// </summary>
    /// <param name="categoryName">The name.</param>
    /// <param name="getCurrentConfig"></param>
    /// <param name="powertoolsConfigurations"></param>
    public PowertoolsLogger(
        string categoryName,
        Func<PowertoolsLoggerConfiguration> getCurrentConfig,
        IPowertoolsConfigurations powertoolsConfigurations)
    {
        _categoryName = categoryName;
        _currentConfig = getCurrentConfig;
        _powertoolsConfigurations = powertoolsConfigurations;
    }

    /// <summary>
    ///     Begins the scope.
    /// </summary>
    /// <typeparam name="TState">The type of the t state.</typeparam>
    /// <param name="state">The state.</param>
    /// <returns>System.IDisposable.</returns>
    public IDisposable BeginScope<TState>(TState state)
    {
        CurrentScope = new PowertoolsLoggerScope(this, GetScopeKeys(state));
        return CurrentScope;
    }

    /// <summary>
    ///     Ends the scope.
    /// </summary>
    internal void EndScope()
    {
        CurrentScope = null;
    }

    /// <summary>
    ///     Determines whether the specified log level is enabled.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <returns>bool.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(LogLevel logLevel)
    {
        var config = _currentConfig();
        
        //if Buffering is enabled and the log level is below the buffer threshold, skip logging only if bellow error
        if (logLevel <= config.LogBuffering?.BufferAtLogLevel
            && config.LogBuffering?.BufferAtLogLevel != LogLevel.Error
            && config.LogBuffering?.BufferAtLogLevel != LogLevel.Critical)
        {
            return false;
        }
        
        // If we have no explicit minimum level, use the default
        var effectiveMinLevel = config.MinimumLogLevel != LogLevel.None
            ? config.MinimumLogLevel
            : LoggingConstants.DefaultLogLevel;
        
        // Log diagnostic info for Debug/Trace levels
        if (logLevel <= LogLevel.Debug)
        {
            return logLevel >= effectiveMinLevel;
        }
        
        // Standard check
        return logLevel >= effectiveMinLevel;
    }

    /// <summary>
    ///     Writes a log entry.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a <see cref="T:System.String" /> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }
        
        _currentConfig().LogOutput.WriteLine(LogEntryString(logLevel, state, exception, formatter));
    }
    
    internal void LogLine(string message)
    {
        _currentConfig().LogOutput.WriteLine(message);
    }

    internal string LogEntryString<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var logEntry = LogEntry(logLevel, state, exception, formatter);
        return _currentConfig().Serializer.Serialize(logEntry, typeof(object));
    }
    
    internal object LogEntry<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var timestamp = DateTime.UtcNow;

        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        // Extract structured parameters for template-style logging
        var structuredParameters = ExtractStructuredParameters(state, out string messageTemplate);
        
        // Format the message
        var message = CustomFormatter(state, exception, out var customMessage) && customMessage is not null
            ? customMessage
            : formatter(state, exception);

        // Get log entry
        var logFormatter = _currentConfig().LogFormatter;
        var logEntry = logFormatter is null
            ? GetLogEntry(logLevel, timestamp, message, exception, structuredParameters)
            : GetFormattedLogEntry(logLevel, timestamp, message, exception, logFormatter, structuredParameters);
        return logEntry;
    }

    /// <summary>
    ///     Gets a log entry.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="timestamp">Entry timestamp.</param>
    /// <param name="message">The message to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="structuredParameters">The parameters for structured formatting</param>
    private Dictionary<string, object> GetLogEntry(LogLevel logLevel, DateTime timestamp, object message,
        Exception exception, Dictionary<string, object> structuredParameters = null)
    {
        var logEntry = new Dictionary<string, object>();

        var config = _currentConfig();
        logEntry.TryAdd(config.LogLevelKey, logLevel.ToString());
        logEntry.TryAdd(LoggingConstants.KeyMessage, message);
        logEntry.TryAdd(LoggingConstants.KeyTimestamp, timestamp.ToString( config.TimestampFormat ?? "o"));
        logEntry.TryAdd(LoggingConstants.KeyService, config.Service);
        logEntry.TryAdd(LoggingConstants.KeyColdStart, _powertoolsConfigurations.IsColdStart);
        
        // Add Lambda Context Keys
        if (LoggingLambdaContext.Instance is not null)
        {
            AddLambdaContextKeys(logEntry);
        }
        
        
        if(! string.IsNullOrWhiteSpace(_powertoolsConfigurations.XRayTraceId))
            logEntry.TryAdd(LoggingConstants.KeyXRayTraceId,
                _powertoolsConfigurations.XRayTraceId.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Replace("Root=", ""));
        logEntry.TryAdd(LoggingConstants.KeyLoggerName, _categoryName);
        
        if (config.SamplingRate > 0)
            logEntry.TryAdd(LoggingConstants.KeySamplingRate, config.SamplingRate);
        
        // Add Custom Keys
        foreach (var (key, value) in this.GetAllKeys())
        {
            logEntry.TryAdd(key, value);
        }
        
        
        // Add Extra Fields
        if (CurrentScope?.ExtraKeys is not null)
        {
            foreach (var (key, value) in CurrentScope.ExtraKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    logEntry.TryAdd(key, value);
                }
            }
        }

        // Add structured parameters
        if (structuredParameters != null && structuredParameters.Count > 0)
        {
            foreach (var (key, value) in structuredParameters)
            {
                if (!string.IsNullOrWhiteSpace(key) && key != "json")
                {
                    logEntry.TryAdd(key, value);
                }
            }
        }
        
        // Use the AddExceptionDetails method instead of adding exception directly
        if (exception != null)
        {
            logEntry.TryAdd(LoggingConstants.KeyException, exception);
        }

        return logEntry;
    }

    /// <summary>
    ///     Gets a formatted log entry. For custom log formatter
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="timestamp">Entry timestamp.</param>
    /// <param name="message">The message to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="logFormatter">The custom log entry formatter.</param>
    /// <param name="structuredParameters">The structured parameters.</param>
    private object GetFormattedLogEntry(LogLevel logLevel, DateTime timestamp, object message,
        Exception exception, ILogFormatter logFormatter, Dictionary<string, object> structuredParameters)
    {
        if (logFormatter is null)
            return null;

        var config = _currentConfig();
        var logEntry = new LogEntry
        {
            Timestamp = timestamp,
            Level = logLevel,
            Service = config.Service,
            Name = _categoryName,
            Message = message,
            Exception = exception,  // Keep this to maintain compatibility
            SamplingRate = config.SamplingRate,
        };

        var extraKeys = new Dictionary<string, object>();

        // Add Custom Keys
        foreach (var (key, value) in this.GetAllKeys())
        {
            switch (key)
            {
                case LoggingConstants.KeyColdStart:
                    logEntry.ColdStart = (bool)value;
                    break;
                case LoggingConstants.KeyXRayTraceId:
                    logEntry.XRayTraceId = value as string;
                    break;
                case LoggingConstants.KeyCorrelationId:
                    logEntry.CorrelationId = value as string;
                    break;
                default:
                    extraKeys.TryAdd(key, value);
                    break;
            }
        }

        // Add Extra Fields
        if (CurrentScope?.ExtraKeys is not null)
        {
            foreach (var (key, value) in CurrentScope.ExtraKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    extraKeys.TryAdd(key, value);
                }
            }
        }
        
        // Add structured parameters
        if (structuredParameters != null && structuredParameters.Count > 0)
        {
            foreach (var (key, value) in structuredParameters)
            {
                if (!string.IsNullOrWhiteSpace(key) && key != "json")
                {
                    extraKeys.TryAdd(key, value);
                }
            }
        }
        
        // Add detailed exception information
        if (exception != null)
        {
            var exceptionDetails = new Dictionary<string, object>();
            exceptionDetails.TryAdd(LoggingConstants.KeyException, exception);
            
            // Add exception details to extra keys
            foreach (var (key, value) in exceptionDetails)
            {
                extraKeys.TryAdd(key, value);
            }
        }

        if (extraKeys.Any())
            logEntry.ExtraKeys = extraKeys;

        // Add Lambda Context Keys
        if (LoggingLambdaContext.Instance is not null)
        {
            logEntry.LambdaContext = CreateLambdaContext();
        }

        try
        {
            var logObject = logFormatter.FormatLogEntry(logEntry);
            if (logObject is null)
                throw new LogFormatException($"{logFormatter.GetType().FullName} returned Null value.");
                
#if NET8_0_OR_GREATER
            return PowertoolsLoggerHelpers.ObjectToDictionary(logObject);
#else
            return logObject;
#endif
        }
        catch (Exception e)
        {
            throw new LogFormatException(
                $"{logFormatter.GetType().FullName} raised an exception: {e.Message}.", e);
        }
    }

    /// <summary>
    ///     Formats message for a log entry.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be formatted.</typeparam>
    /// <param name="state">The entry to be formatted. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="message">The formatted message</param>
    /// <returns>bool</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CustomFormatter<TState>(TState state, Exception exception, out object message)
    {
        message = null;
        if (exception is not null)
            return false;

#if NET8_0_OR_GREATER
        var stateKeys = (state as IEnumerable<KeyValuePair<string, object>>)?
            .ToDictionary(i => i.Key, i => PowertoolsLoggerHelpers.ObjectToDictionary(i.Value));
#else
        var stateKeys = (state as IEnumerable<KeyValuePair<string, object>>)?
            .ToDictionary(i => i.Key, i => i.Value);
#endif

        if (stateKeys is null || stateKeys.Count != 2)
            return false;

        if (!stateKeys.TryGetValue("{OriginalFormat}", out var originalFormat))
            return false;

        if (originalFormat?.ToString() != LoggingConstants.KeyJsonFormatter)
            return false;

        message = stateKeys.First(k => k.Key != "{OriginalFormat}").Value;

        return true;
    }

    /// <summary>
    ///     Adds the lambda context keys.
    /// </summary>
    /// <param name="logEntry">The log entry.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddLambdaContextKeys(Dictionary<string, object> logEntry)
    {
        var context = LoggingLambdaContext.Instance;
        logEntry.TryAdd(LoggingConstants.KeyFunctionName, context.FunctionName);
        logEntry.TryAdd(LoggingConstants.KeyFunctionMemorySize, context.MemoryLimitInMB);
        logEntry.TryAdd(LoggingConstants.KeyFunctionArn, context.InvokedFunctionArn);
        logEntry.TryAdd(LoggingConstants.KeyFunctionRequestId, context.AwsRequestId);
        logEntry.TryAdd(LoggingConstants.KeyFunctionVersion, context.FunctionVersion);
    }

    /// <summary>
    ///     Creates the lambda context.
    /// </summary>
    /// <returns>LogEntryLambdaContext.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LogEntryLambdaContext CreateLambdaContext()
    {
        var context = LoggingLambdaContext.Instance;
        return new LogEntryLambdaContext
        {
            FunctionName = context.FunctionName,
            FunctionVersion = context.FunctionVersion,
            MemoryLimitInMB = context.MemoryLimitInMB,
            InvokedFunctionArn = context.InvokedFunctionArn,
            AwsRequestId = context.AwsRequestId,
        };
    }

    /// <summary>
    ///     Gets the scope keys.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="state">The state.</param>
    /// <returns>Dictionary&lt;System.String, System.Object&gt;.</returns>
    private static Dictionary<string, object> GetScopeKeys<TState>(TState state)
    {
        var keys = new Dictionary<string, object>();

        if (state is null)
            return keys;

        switch (state)
        {
            case IEnumerable<KeyValuePair<string, string>> stringPairs:
                foreach (var (key, value) in stringPairs)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        keys.TryAdd(key, value);
                }
                break;
            
            case IEnumerable<KeyValuePair<string, object>> objectPairs:
                foreach (var (key, value) in objectPairs)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        keys.TryAdd(key, value);
                }
                break;
            
            default:
                // Skip property reflection for primitive types, strings and value types
                if (state is string || 
                    (state.GetType().IsPrimitive) || 
                    state is ValueType)
                {
                    // Don't extract properties from primitives or strings
                    break;
                }
            
                // For complex objects, use reflection to get properties
                foreach (var property in state.GetType().GetProperties())
                {
                    try
                    {
                        keys.TryAdd(property.Name, property.GetValue(state));
                    }
                    catch
                    {
                        // Safely ignore reflection exceptions
                    }
                }
                break;
        }

        return keys;
    }

    /// <summary>
    /// Extracts structured parameter key-value pairs from the log state
    /// </summary>
    private Dictionary<string, object> ExtractStructuredParameters<TState>(TState state, out string messageTemplate)
    {
        messageTemplate = string.Empty;
        var parameters = new Dictionary<string, object>();
        
        if (state is IEnumerable<KeyValuePair<string, object>> stateProps)
        {
            // Dictionary to store format specifiers for each parameter
            var formatSpecifiers = new Dictionary<string, string>();
            
            // First pass - extract template and identify format specifiers
            foreach (var prop in stateProps)
            {
                // The original message template is stored with key "{OriginalFormat}"
                if (prop.Key == "{OriginalFormat}" && prop.Value is string template)
                {
                    messageTemplate = template;
                    
                    // Extract format specifiers from the template
                    var matches = System.Text.RegularExpressions.Regex.Matches(
                        template, 
                        @"{([@\w]+)(?::([^{}]+))?}");
                    
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        string paramName = match.Groups[1].Value;
                        if (match.Groups.Count > 2 && match.Groups[2].Success)
                        {
                            formatSpecifiers[paramName] = match.Groups[2].Value;
                        }
                    }
                    
                    continue;
                }
            }
            
            // Second pass - process values with extracted format specifiers
            foreach (var prop in stateProps)
            {
                if (prop.Key == "{OriginalFormat}")
                    continue;
                
                // Extract parameter name without braces
                string paramName = ExtractParameterName(prop.Key);
                if (string.IsNullOrEmpty(paramName))
                    continue;
                
                // Handle special serialization designators (like @)
                bool useStructuredSerialization = paramName.StartsWith("@");
                string actualParamName = useStructuredSerialization ? paramName.Substring(1) : paramName;
                
                // Apply formatting if a format specifier exists and the value can be formatted
                if (!useStructuredSerialization && 
                    formatSpecifiers.TryGetValue(paramName, out string format) && 
                    prop.Value is IFormattable formattable)
                {
                    // Format the value using the specified format
                    string formattedValue = formattable.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    
                    // Try to preserve the numeric type if possible
                    if (double.TryParse(formattedValue, out double numericValue))
                    {
                        parameters[actualParamName] = numericValue;
                    }
                    else
                    {
                        parameters[actualParamName] = formattedValue;
                    }
                }
                else if (useStructuredSerialization)
                {
                    // Serialize the entire object
                    parameters[actualParamName] = prop.Value;
                }
                else
                {
                    // For regular objects, convert to string if it's not a primitive type
                    if (prop.Value != null && 
                        !(prop.Value is string) && 
                        !(prop.Value is ValueType) &&
                        !(prop.Value.GetType().IsPrimitive))
                    {
                        parameters[actualParamName] = prop.Value.ToString();
                    }
                    else
                    {
                        // For primitives, use the value directly
                        parameters[actualParamName] = prop.Value;
                    }
                }
            }
        }
        
        return parameters;
    }

    /// <summary>
    /// Extracts the parameter name from a template placeholder (e.g. "{paramName}" or "{paramName:format}")
    /// </summary>
    private string ExtractParameterName(string key)
    {
        // If it's already a proper parameter name without braces, return it
        if (!key.StartsWith("{") || !key.EndsWith("}"))
            return key;
            
        // Remove the braces
        var nameWithPossibleFormat = key.Substring(1, key.Length - 2);
        
        // If there's a format specifier, remove it
        var colonIndex = nameWithPossibleFormat.IndexOf(':');
        return colonIndex > 0 
            ? nameWithPossibleFormat.Substring(0, colonIndex) 
            : nameWithPossibleFormat;
    }
}