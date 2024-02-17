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
using System.ComponentModel;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using Microsoft.Extensions.Logging;
using DateOnlyConverter = AWS.Lambda.Powertools.Logging.Internal.Converters.DateOnlyConverter;
using TimeOnlyConverter = AWS.Lambda.Powertools.Logging.Internal.Converters.TimeOnlyConverter;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class PowertoolsLogger. This class cannot be inherited.
///     Implements the <see cref="Microsoft.Extensions.Logging.ILogger" />
/// </summary>
/// <seealso cref="Microsoft.Extensions.Logging.ILogger" />
internal sealed class PowertoolsLogger : ILogger
{
    /// <summary>
    ///     The get current configuration
    /// </summary>
    private readonly Func<LoggerConfiguration> _getCurrentConfig;

    /// <summary>
    ///     The name
    /// </summary>
    private readonly string _name;

    /// <summary>
    ///     The current configuration
    /// </summary>
    private LoggerConfiguration _currentConfig;

    /// <summary>
    ///     The Powertools for AWS Lambda (.NET) configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     The JsonSerializer options
    /// </summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    private LogLevel _lambdaLogLevel;
    private LogLevel _logLevel;
    private bool _lambdaLogLevelEnabled;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsLogger" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="systemWrapper">The system wrapper.</param>
    /// <param name="getCurrentConfig">The get current configuration.</param>
    public PowertoolsLogger(
        string name,
        IPowertoolsConfigurations powertoolsConfigurations,
        ISystemWrapper systemWrapper,
        Func<LoggerConfiguration> getCurrentConfig)
    {
        (_name, _powertoolsConfigurations, _systemWrapper, _getCurrentConfig) = (name,
            powertoolsConfigurations, systemWrapper, getCurrentConfig);

        _powertoolsConfigurations.SetExecutionEnvironment(this);
        _currentConfig = GetCurrentConfig();

        if (_lambdaLogLevelEnabled && _logLevel < _lambdaLogLevel)
        {
            var message =
                $"Current log level ({_logLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({_lambdaLogLevel}). This can lead to data loss, consider adjusting them.";
            this.LogWarning(message);
        }
    }

    private LoggerConfiguration CurrentConfig => _currentConfig ??= GetCurrentConfig();

    /// <summary>
    ///     Sets the minimum level.
    /// </summary>
    /// <value>The minimum level.</value>
    private LogLevel MinimumLevel =>
        CurrentConfig.MinimumLevel ?? LoggingConstants.DefaultLogLevel;

    /// <summary>
    ///     Sets the service.
    /// </summary>
    /// <value>The service.</value>
    private string Service =>
        !string.IsNullOrWhiteSpace(CurrentConfig.Service)
            ? CurrentConfig.Service
            : _powertoolsConfigurations.Service;

    /// <summary>
    ///     Get JsonSerializer options.
    /// </summary>
    /// <value>The current configuration.</value>
    private JsonSerializerOptions JsonSerializerOptions =>
        _jsonSerializerOptions ??= BuildJsonSerializerOptions();

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

#if NET8_0_OR_GREATER

        var logEntry = logFormatter is null
            ? GetLogEntry(logLevel, timestamp, ToDictionary(message), exception)
            : ToDictionary(GetFormattedLogEntry(logLevel, timestamp, ToDictionary(message), exception, logFormatter));
            
        _systemWrapper.LogLine(JsonSerializer.Serialize(logEntry, JsonSerializerOptions));
        
        static object ToDictionary(object values)
        {
            if (values is string)
            {
                return values;
            }
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (values != null)
            {
                foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(values))
                {
                    object obj = propertyDescriptor.GetValue(values);
                    if (obj.GetType().Name.Contains("Anonymous"))
                    {
                        dict.Add(propertyDescriptor.Name,ToDictionary(obj));
                    }
                    else
                    {
                        dict.Add(propertyDescriptor.Name, obj);
                    }
                }
            }

            return dict;
        }
#else
        var logEntry = logFormatter is null
            ? GetLogEntry(logLevel, timestamp, message, exception)
            : GetFormattedLogEntry(logLevel, timestamp, message, exception, logFormatter);

        _systemWrapper.LogLine(JsonSerializer.Serialize(logEntry, JsonSerializerOptions));
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
        if (PowertoolsLambdaContext.Instance is not null)
        {
            logEntry.TryAdd(LoggingConstants.KeyFunctionName, PowertoolsLambdaContext.Instance.FunctionName);
            logEntry.TryAdd(LoggingConstants.KeyFunctionVersion, PowertoolsLambdaContext.Instance.FunctionVersion);
            logEntry.TryAdd(LoggingConstants.KeyFunctionMemorySize, PowertoolsLambdaContext.Instance.MemoryLimitInMB);
            logEntry.TryAdd(LoggingConstants.KeyFunctionArn, PowertoolsLambdaContext.Instance.InvokedFunctionArn);
            logEntry.TryAdd(LoggingConstants.KeyFunctionRequestId, PowertoolsLambdaContext.Instance.AwsRequestId);
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
        // If ALC is enabled and PascalCase we need to convert Level to LogLevel for it to be parsed and sent to CW
        if (_lambdaLogLevelEnabled && CurrentConfig.LoggerOutputCase == LoggerOutputCase.PascalCase)
        {
            keyLogLevel = "LogLevel";
        }

        logEntry.TryAdd(LoggingConstants.KeyTimestamp, timestamp.ToString("o"));
        logEntry.TryAdd(keyLogLevel, logLevel.ToString());
        logEntry.TryAdd(LoggingConstants.KeyService, Service);
        logEntry.TryAdd(LoggingConstants.KeyLoggerName, _name);
        logEntry.TryAdd(LoggingConstants.KeyMessage, message);

        if (CurrentConfig.SamplingRate.HasValue)
            logEntry.TryAdd(LoggingConstants.KeySamplingRate, CurrentConfig.SamplingRate.Value);
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
            SamplingRate = CurrentConfig.SamplingRate,
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
        if (PowertoolsLambdaContext.Instance is not null)
        {
            logEntry.LambdaContext = new LogEntryLambdaContext
            {
                FunctionName = PowertoolsLambdaContext.Instance.FunctionName,
                FunctionVersion = PowertoolsLambdaContext.Instance.FunctionVersion,
                MemoryLimitInMB = PowertoolsLambdaContext.Instance.MemoryLimitInMB,
                InvokedFunctionArn = PowertoolsLambdaContext.Instance.InvokedFunctionArn,
                AwsRequestId = PowertoolsLambdaContext.Instance.AwsRequestId,
            };
        }

        try
        {
            var logObject = logFormatter.FormatLogEntry(logEntry);
            if (logObject is null)
                throw new LogFormatException($"{logFormatter.GetType().FullName} returned Null value.");
            return logObject;
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
    ///     Gets the current configuration.
    /// </summary>
    /// <returns>AWS.Lambda.Powertools.Logging.LoggerConfiguration.</returns>
    private LoggerConfiguration GetCurrentConfig()
    {
        var currConfig = _getCurrentConfig();
        _logLevel = _powertoolsConfigurations.GetLogLevel(currConfig?.MinimumLevel);
        var samplingRate = currConfig?.SamplingRate ?? _powertoolsConfigurations.LoggerSampleRate;
        var loggerOutputCase = _powertoolsConfigurations.GetLoggerOutputCase(currConfig?.LoggerOutputCase);
        _lambdaLogLevel = _powertoolsConfigurations.GetLambdaLogLevel();
        _lambdaLogLevelEnabled = _lambdaLogLevel != LogLevel.None;

        var minLogLevel = _logLevel;
        if (_lambdaLogLevelEnabled)
        {
            minLogLevel = _lambdaLogLevel;
        }

        var config = new LoggerConfiguration
        {
            Service = currConfig?.Service,
            MinimumLevel = minLogLevel,
            SamplingRate = samplingRate,
            LoggerOutputCase = loggerOutputCase
        };

        if (!samplingRate.HasValue)
            return config;

        if (samplingRate.Value < 0 || samplingRate.Value > 1)
        {
            if (minLogLevel is LogLevel.Debug or LogLevel.Trace)
                _systemWrapper.LogLine(
                    $"Skipping sampling rate configuration because of invalid value. Sampling rate: {samplingRate.Value}");
            config.SamplingRate = null;
            return config;
        }

        if (samplingRate.Value == 0)
            return config;

        var sample = _systemWrapper.GetRandom();
        if (samplingRate.Value > sample)
        {
            _systemWrapper.LogLine(
                $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate.Value}, Sampler Value: {sample}.");
            config.MinimumLevel = LogLevel.Debug;
        }

        return config;
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

        var stateKeys = (state as IEnumerable<KeyValuePair<string, object>>)?
            .ToDictionary(i => i.Key, i => i.Value);

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
    ///     Builds JsonSerializer options.
    /// </summary>
    private JsonSerializerOptions BuildJsonSerializerOptions()
    {
        var jsonOptions = CurrentConfig.LoggerOutputCase switch
        {
            LoggerOutputCase.CamelCase => new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            },
            LoggerOutputCase.PascalCase => new JsonSerializerOptions
            {
                PropertyNamingPolicy = PascalCaseNamingPolicy.Instance,
                DictionaryKeyPolicy = PascalCaseNamingPolicy.Instance
            },
            _ => new JsonSerializerOptions
            {
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                DictionaryKeyPolicy = SnakeCaseNamingPolicy.Instance
            }
        };
        jsonOptions.Converters.Add(new ByteArrayConverter());
        jsonOptions.Converters.Add(new ExceptionConverter());
        jsonOptions.Converters.Add(new MemoryStreamConverter());
        jsonOptions.Converters.Add(new ConstantClassConverter());
        jsonOptions.Converters.Add(new DateOnlyConverter());
        jsonOptions.Converters.Add(new TimeOnlyConverter());
        
        jsonOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

#if NET8_0_OR_GREATER
        jsonOptions.TypeInfoResolver = PowertoolsSourceGenerationContext.Default;
#endif
        return jsonOptions;
    }
}