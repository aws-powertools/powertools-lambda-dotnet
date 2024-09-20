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
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     The current scope
    /// </summary>
    internal PowertoolsLoggerScope CurrentScope { get; private set; }

    /// <summary>
    ///     Private constructor - Is initialized on CreateLogger
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="systemWrapper">The system wrapper.</param>
    private PowertoolsLogger(
        string name,
        IPowertoolsConfigurations powertoolsConfigurations,
        ISystemWrapper systemWrapper)
    {
        _name = name;
        _powertoolsConfigurations = powertoolsConfigurations;
        _systemWrapper = systemWrapper;

        _powertoolsConfigurations.SetExecutionEnvironment(this);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsLogger" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="systemWrapper">The system wrapper.</param>
    internal static PowertoolsLogger CreateLogger(string name,
        IPowertoolsConfigurations powertoolsConfigurations,
        ISystemWrapper systemWrapper)
    {
        return new PowertoolsLogger(name, powertoolsConfigurations, systemWrapper);
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
    public bool IsEnabled(LogLevel logLevel) => _powertoolsConfigurations.IsLogLevelEnabled(logLevel);

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
        var logEntry = new Dictionary<string, object>();

        // Add Custom Keys
        foreach (var (key, value) in Logger.GetAllKeys())
        {
            logEntry.TryAdd(key, value);
        }

        // Add Lambda Context Keys
        if (LoggingLambdaContext.Instance is not null)
        {
            AddLambdaContextKeys(logEntry);
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

        var keyLogLevel = GetLogLevelKey();

        logEntry.TryAdd(LoggingConstants.KeyTimestamp, timestamp.ToString("o"));
        logEntry.TryAdd(keyLogLevel, logLevel.ToString());
        logEntry.TryAdd(LoggingConstants.KeyService, _powertoolsConfigurations.CurrentConfig().Service);
        logEntry.TryAdd(LoggingConstants.KeyLoggerName, _name);
        logEntry.TryAdd(LoggingConstants.KeyMessage, message);
        if (_powertoolsConfigurations.CurrentConfig().SamplingRate > 0)
            logEntry.TryAdd(LoggingConstants.KeySamplingRate, _powertoolsConfigurations.CurrentConfig().SamplingRate);
        if (exception != null)
            logEntry.TryAdd(LoggingConstants.KeyException, exception);

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
    private object GetFormattedLogEntry(LogLevel logLevel, DateTime timestamp, object message,
        Exception exception, ILogFormatter logFormatter)
    {
        if (logFormatter is null)
            return null;

        var logEntry = new LogEntry
        {
            Timestamp = timestamp,
            Level = logLevel,
            Service = _powertoolsConfigurations.CurrentConfig().Service,
            Name = _name,
            Message = message,
            Exception = exception,
            SamplingRate = _powertoolsConfigurations.CurrentConfig().SamplingRate,
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
    ///     Gets the log level key.
    /// </summary>
    /// <returns>System.String.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetLogLevelKey()
    {
        return _powertoolsConfigurations.LambdaLogLevelEnabled() &&
               _powertoolsConfigurations.CurrentConfig().LoggerOutputCase == LoggerOutputCase.PascalCase
            ? "LogLevel"
            : LoggingConstants.KeyLogLevel;
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
        logEntry.TryAdd(LoggingConstants.KeyFunctionVersion, context.FunctionVersion);
        logEntry.TryAdd(LoggingConstants.KeyFunctionMemorySize, context.MemoryLimitInMB);
        logEntry.TryAdd(LoggingConstants.KeyFunctionArn, context.InvokedFunctionArn);
        logEntry.TryAdd(LoggingConstants.KeyFunctionRequestId, context.AwsRequestId);
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
                foreach (var property in state.GetType().GetProperties())
                {
                    keys.TryAdd(property.Name, property.GetValue(state));
                }

                break;
        }

        return keys;
    }
}