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
using System.Collections.Concurrent;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class LoggerProvider. This class cannot be inherited.
///     Implements the <see cref="T:Microsoft.Extensions.Logging.ILoggerProvider" />
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Logging.ILoggerProvider" />
[ProviderAlias("PowertoolsLogger")]
internal sealed class PowertoolsLoggerProvider : ILoggerProvider
{
    /// <summary>
    ///     The powertools configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     The loggers
    /// </summary>
    private readonly ConcurrentDictionary<string, PowertoolsLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    private readonly IDisposable? _onChangeToken;
    private PowertoolsLoggerConfiguration _currentConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsLoggerProvider" /> class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="powertoolsConfigurations"></param>
    /// <param name="systemWrapper"></param>
    public PowertoolsLoggerProvider(IOptionsMonitor<PowertoolsLoggerConfiguration> config,
        IPowertoolsConfigurations powertoolsConfigurations,
        ISystemWrapper? systemWrapper = null)
    {
        // Use custom system wrapper if provided through config
        var currentConfig = config.CurrentValue;
        _systemWrapper = currentConfig.LoggerOutput ?? systemWrapper ?? new SystemWrapper();
        
        _powertoolsConfigurations = powertoolsConfigurations;
        _currentConfig = currentConfig;

        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
        ApplyPowertoolsConfig(_currentConfig);
    }

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        _powertoolsConfigurations.SetExecutionEnvironment(typeof(PowertoolsLogger));
        
        return _loggers.GetOrAdd(categoryName, name => new PowertoolsLogger(name,
            () => _currentConfig,
            _systemWrapper));
    }

    internal PowertoolsLoggerConfiguration GetCurrentConfig()
    {
        var config = _currentConfig;

        ApplyPowertoolsConfig(config);

        return config;
    }

    private void ApplyPowertoolsConfig(PowertoolsLoggerConfiguration config)
    {
        var logLevel = _powertoolsConfigurations.GetLogLevel(LogLevel.None);
        var lambdaLogLevel = _powertoolsConfigurations.GetLambdaLogLevel();
        var lambdaLogLevelEnabled = _powertoolsConfigurations.LambdaLogLevelEnabled();

        // Check for explicit config
        bool hasExplicitLevel = config.MinimumLogLevel != LogLevel.None;

        // Warn if Lambda log level doesn't match
        if (lambdaLogLevelEnabled && hasExplicitLevel && config.MinimumLogLevel < lambdaLogLevel)
        {
            _systemWrapper.LogLine(
                $"Current log level ({config.MinimumLogLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({lambdaLogLevel}). This can lead to data loss, consider adjusting them.");
        }

        // Set service from environment if not explicitly set
        if (string.IsNullOrEmpty(config.Service))
        {
            config.Service = _powertoolsConfigurations.Service;
        }

        // Set output case from environment if not explicitly set
        if (config.LoggerOutputCase == LoggerOutputCase.Default)
        {
            var loggerOutputCase = _powertoolsConfigurations.GetLoggerOutputCase(config.LoggerOutputCase);
            config.LoggerOutputCase = loggerOutputCase;
        }

        // Set log level from environment ONLY if not explicitly set
        if (!hasExplicitLevel)
        {
            var minLogLevel = lambdaLogLevelEnabled ? lambdaLogLevel : logLevel;
            config.MinimumLogLevel = minLogLevel != LogLevel.None ? minLogLevel : LoggingConstants.DefaultLogLevel;
        }
        
        config.XRayTraceId = _powertoolsConfigurations.XRayTraceId;
        config.LogEvent = _powertoolsConfigurations.LoggerLogEvent;
        
        // Configure the log level key based on output case
        config.LogLevelKey = _powertoolsConfigurations.LambdaLogLevelEnabled() &&
                              config.LoggerOutputCase == LoggerOutputCase.PascalCase
            ? "LogLevel"
            : LoggingConstants.KeyLogLevel;
            
        // Handle sampling rate - BUT DON'T MODIFY MINIMUM LEVEL
        ProcessSamplingRate(config);
    }

    private void ProcessSamplingRate(PowertoolsLoggerConfiguration config)
    {
        var samplingRate = config.SamplingRate > 0 
            ? config.SamplingRate 
            : _powertoolsConfigurations.LoggerSampleRate;
            
        samplingRate = ValidateSamplingRate(samplingRate, config.MinimumLogLevel, _systemWrapper);
        config.SamplingRate = samplingRate;

        // Only notify if sampling is configured
        if (samplingRate > 0)
        {
            double sample = _systemWrapper.GetRandom();
            
            // Instead of changing log level, just indicate sampling status
            if (sample <= samplingRate)
            {
                _systemWrapper.LogLine(
                    $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate}, Sampler Value: {sample}.");
                config.MinimumLogLevel = LogLevel.Debug;
            }
        }
    }

    private static double ValidateSamplingRate(double samplingRate, LogLevel minLogLevel, ISystemWrapper systemWrapper)
    {
        if (samplingRate < 0 || samplingRate > 1)
        {
            if (minLogLevel is LogLevel.Debug or LogLevel.Trace)
            {
                systemWrapper.LogLine(
                    $"Skipping sampling rate configuration because of invalid value. Sampling rate: {samplingRate}");
            }

            return 0;
        }

        return samplingRate;
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}