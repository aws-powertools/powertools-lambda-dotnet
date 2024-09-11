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
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using AWS.Lambda.Powertools.Logging.Serializers;
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
    private readonly string _name;

    /// <summary>
    ///     The current configuration
    /// </summary>
    private static LoggerConfiguration _currentConfig;

    /// <summary>
    ///     The Powertools for AWS Lambda (.NET) configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsLogger" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="systemWrapper">The system wrapper.</param>
    /// <param name="currentConfig"></param>
    public PowertoolsLogger(
        string name,
        IPowertoolsConfigurations powertoolsConfigurations,
        ISystemWrapper systemWrapper,
        LoggerConfiguration currentConfig)
    {
        (_name, _powertoolsConfigurations, _systemWrapper, _currentConfig) = (name,
            powertoolsConfigurations, systemWrapper, currentConfig);

        _powertoolsConfigurations.SetExecutionEnvironment(this);
    }

    /// <summary>
    ///     Sets the minimum level.
    /// </summary>
    /// <value>The minimum level.</value>
    private LogLevel MinimumLevel =>
        _currentConfig.MinimumLevel ?? LoggingConstants.DefaultLogLevel;

    /// <summary>
    ///     Sets the service.
    /// </summary>
    /// <value>The service.</value>
    private string Service =>
        !string.IsNullOrWhiteSpace(_currentConfig.Service)
            ? _currentConfig.Service
            : _powertoolsConfigurations.Service;

    internal PowertoolsLoggerScope CurrentScope { get; private set; }

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
    ///     Extract provided scope keys
    /// </summary>
    /// <typeparam name="TState">The type of the t state.</typeparam>
    /// <param name="state">The state.</param>
    /// <returns>Key/Value pair of provided scope keys</returns>
    private static Dictionary<string, object> GetScopeKeys<TState>(TState state)
    {
        var keys = new Dictionary<string, object>();

        if (state is null)
            return keys;

        switch (state)
        {
            case IEnumerable<KeyValuePair<string, string>> pairs:
            {
                foreach (var (key, value) in pairs)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        keys.TryAdd(key, value);
                }

                break;
            }
            case IEnumerable<KeyValuePair<string, object>> pairs:
            {
                foreach (var (key, value) in pairs)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        keys.TryAdd(key, value);
                }

                break;
            }
            default:
            {
                foreach (var property in state.GetType().GetProperties())
                {
                    keys.TryAdd(property.Name, property.GetValue(state));
                }

                break;
            }
        }

        return keys;
    }

    /// <summary>
    ///     Determines whether the specified log level is enabled.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <returns>bool.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= MinimumLevel;
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
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (!IsEnabled(logLevel))
            return;

        var timestamp = DateTime.UtcNow;
        var message = CustomFormatter(state, exception, out var customMessage) && customMessage is not null
            ? customMessage
            : formatter(state, exception);

        var logFormatter = Logger.GetFormatter();
        var logEntry = logFormatter is null
            ? GetLogEntry(logLevel, timestamp, message, exception)
            : GetFormattedLogEntry(logLevel, timestamp, message, exception, logFormatter);


#if NET8_0_OR_GREATER
        _systemWrapper.LogLine(PowertoolsLoggingSerializer.Serialize(logEntry, typeof(object)));
#else
        _systemWrapper.LogLine(PowertoolsLoggingSerializer.Serialize(logEntry));
#endif
    }

    /// <summary>
    ///     Gets a log entry.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="timestamp">Entry timestamp.</param>
    /// <param name="message">The message to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    private Dictionary<string, object> GetLogEntry(LogLevel logLevel, DateTime timestamp, object message,
        Exception exception)
    {
        var logEntry = new Dictionary<string, object>(StringComparer.Ordinal);

        // Add Custom Keys
        foreach (var (key, value) in Logger.GetAllKeys())
            logEntry.TryAdd(key, value);

        // Add Lambda Context Keys
        if (LoggingLambdaContext.Instance is not null)
        {
            logEntry.TryAdd(LoggingConstants.KeyFunctionName, LoggingLambdaContext.Instance.FunctionName);
            logEntry.TryAdd(LoggingConstants.KeyFunctionVersion, LoggingLambdaContext.Instance.FunctionVersion);
            logEntry.TryAdd(LoggingConstants.KeyFunctionMemorySize, LoggingLambdaContext.Instance.MemoryLimitInMB);
            logEntry.TryAdd(LoggingConstants.KeyFunctionArn, LoggingLambdaContext.Instance.InvokedFunctionArn);
            logEntry.TryAdd(LoggingConstants.KeyFunctionRequestId, LoggingLambdaContext.Instance.AwsRequestId);
        }

        // Add Extra Fields
        if (CurrentScope?.ExtraKeys is not null)
        {
            foreach (var (key, value) in CurrentScope.ExtraKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    logEntry.TryAdd(key, value);
            }
        }

        var keyLogLevel = LoggingConstants.KeyLogLevel;
        var lambdaLogLevelEnabled = _powertoolsConfigurations.LambdaLogLevelEnabled();
        
        // If ALC is enabled and PascalCase we need to convert Level to LogLevel for it to be parsed and sent to CW
        if (lambdaLogLevelEnabled && _currentConfig.LoggerOutputCase == LoggerOutputCase.PascalCase)
        {
            keyLogLevel = "LogLevel";
        }

        logEntry.TryAdd(LoggingConstants.KeyTimestamp, timestamp.ToString("o"));
        logEntry.TryAdd(keyLogLevel, logLevel.ToString());
        logEntry.TryAdd(LoggingConstants.KeyService, Service);
        logEntry.TryAdd(LoggingConstants.KeyLoggerName, _name);
        logEntry.TryAdd(LoggingConstants.KeyMessage, message);

        if (_currentConfig.SamplingRate.HasValue)
            logEntry.TryAdd(LoggingConstants.KeySamplingRate, _currentConfig.SamplingRate.Value);
        if (exception != null)
            logEntry.TryAdd(LoggingConstants.KeyException, exception);

        return logEntry;
    }

    /// <summary>
    ///     Gets a formatted log entry.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="timestamp">Entry timestamp.</param>
    /// <param name="message">The message to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="logFormatter">The custom log entry formatter.</param>
    private object GetFormattedLogEntry(LogLevel logLevel, DateTime timestamp, object message,
        Exception exception, ILogFormatter logFormatter)
    {
        if (logFormatter is null)
            return null;

        var logEntry = new LogEntry
        {
            Timestamp = timestamp,
            Level = logLevel,
            Service = Service,
            Name = _name,
            Message = message,
            Exception = exception,
            SamplingRate = _currentConfig.SamplingRate,
        };

        var extraKeys = new Dictionary<string, object>();

        // Add Custom Keys
        foreach (var (key, value) in Logger.GetAllKeys())
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
                    extraKeys.TryAdd(key, value);
            }
        }

        if (extraKeys.Any())
            logEntry.ExtraKeys = extraKeys;

        // Add Lambda Context Keys
        if (LoggingLambdaContext.Instance is not null)
        {
            logEntry.LambdaContext = new LogEntryLambdaContext
            {
                FunctionName = LoggingLambdaContext.Instance.FunctionName,
                FunctionVersion = LoggingLambdaContext.Instance.FunctionVersion,
                MemoryLimitInMB = LoggingLambdaContext.Instance.MemoryLimitInMB,
                InvokedFunctionArn = LoggingLambdaContext.Instance.InvokedFunctionArn,
                AwsRequestId = LoggingLambdaContext.Instance.AwsRequestId,
            };
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
    ///     Clears the configuration.
    /// </summary>
    internal void ClearConfig()
    {
        _currentConfig = null;
    }

    /// <summary>
    ///     Formats message for a log entry.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be formatted.</typeparam>
    /// <param name="state">The entry to be formatted. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="message">The formatted message</param>
    /// <returns>bool</returns>
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
}