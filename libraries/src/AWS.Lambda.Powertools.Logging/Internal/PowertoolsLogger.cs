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
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
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
    ///     The get current configuration
    /// </summary>
    private readonly Func<LoggerConfiguration> _getCurrentConfig;

    /// <summary>
    ///     The name
    /// </summary>
    private readonly string _name;

    /// <summary>
    ///     The power tools configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     The current configuration
    /// </summary>
    private LoggerConfiguration _currentConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsLogger" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="powertoolsConfigurations">The power tools configurations.</param>
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
    }

    /// <summary>
    ///     Sets the current configuration.
    /// </summary>
    /// <value>The current configuration.</value>
    private LoggerConfiguration CurrentConfig =>
        _currentConfig ??= GetCurrentConfig();

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
    ///     Begins the scope.
    /// </summary>
    /// <typeparam name="TState">The type of the t state.</typeparam>
    /// <param name="state">The state.</param>
    /// <returns>System.IDisposable.</returns>
    public IDisposable BeginScope<TState>(TState state)
    {
        return default!;
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
    ///     Logs the specified log level.
    /// </summary>
    /// <typeparam name="TState">The type of the t state.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="state">The state.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="formatter">The formatter.</param>
    /// <exception cref="ArgumentNullException">nameof(formatter)</exception>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (!IsEnabled(logLevel))
            return;

        var message = new Dictionary<string, object>(StringComparer.Ordinal);

        // Add Custom Keys
        foreach (var (key, value) in Logger.GetAllKeys())
            message.TryAdd(key, value);

        // Add Lambda Context Keys
        if (PowertoolsLambdaContext.Instance is not null)
        {
            message.TryAdd(LoggingConstants.KeyFunctionName, PowertoolsLambdaContext.Instance.FunctionName);
            message.TryAdd(LoggingConstants.KeyFunctionVersion, PowertoolsLambdaContext.Instance.FunctionVersion);
            message.TryAdd(LoggingConstants.KeyFunctionMemorySize, PowertoolsLambdaContext.Instance.MemoryLimitInMB);
            message.TryAdd(LoggingConstants.KeyFunctionArn, PowertoolsLambdaContext.Instance.InvokedFunctionArn);
            message.TryAdd(LoggingConstants.KeyFunctionRequestId, PowertoolsLambdaContext.Instance.AwsRequestId);
        }

        message.TryAdd(LoggingConstants.KeyTimestamp, DateTime.UtcNow.ToString("o"));
        message.TryAdd(LoggingConstants.KeyLogLevel, logLevel.ToString());
        message.TryAdd(LoggingConstants.KeyService, Service);
        message.TryAdd(LoggingConstants.KeyLoggerName, _name);
        message.TryAdd(LoggingConstants.KeyMessage,
            CustomFormatter(state, exception, out var customMessage) && customMessage is not null
                ? customMessage
                : formatter(state, exception));
        if (CurrentConfig.SamplingRate.HasValue)
            message.TryAdd(LoggingConstants.KeySamplingRate, CurrentConfig.SamplingRate.Value);
        if (exception != null)
            message.TryAdd(LoggingConstants.KeyException, exception.Message);

        _systemWrapper.LogLine(JsonSerializer.Serialize(message));
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
        var minimumLevel = _powertoolsConfigurations.GetLogLevel(currConfig?.MinimumLevel);
        var samplingRate = currConfig?.SamplingRate ?? _powertoolsConfigurations.LoggerSampleRate;

        var config = new LoggerConfiguration
        {
            Service = currConfig?.Service,
            MinimumLevel = minimumLevel,
            SamplingRate = samplingRate
        };

        if (!samplingRate.HasValue)
            return config;

        if (samplingRate.Value < 0 || samplingRate.Value > 1)
        {
            if (minimumLevel is LogLevel.Debug or LogLevel.Trace)
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
    ///     Customs the formatter.
    /// </summary>
    /// <typeparam name="TState">The type of the t state.</typeparam>
    /// <param name="state">The state.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message.</param>
    /// <returns>bool.</returns>
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
}