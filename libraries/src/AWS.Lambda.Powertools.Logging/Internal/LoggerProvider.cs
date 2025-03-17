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
internal sealed class LoggerProvider : ILoggerProvider
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
    ///     Initializes a new instance of the <see cref="LoggerProvider" /> class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="powertoolsConfigurations"></param>
    /// <param name="systemWrapper"></param>
    public LoggerProvider(IOptionsMonitor<PowertoolsLoggerConfiguration> config,
        IPowertoolsConfigurations powertoolsConfigurations,
        ISystemWrapper systemWrapper)
    {
        _currentConfig = config.CurrentValue;
        _powertoolsConfigurations = powertoolsConfigurations;
        _systemWrapper = systemWrapper;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);

        // TODO: FIx this
        // It was moved bellow
        // _powertoolsConfigurations.SetCurrentConfig(_currentConfig, systemWrapper);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggerProvider" /> class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    public LoggerProvider(IOptionsMonitor<PowertoolsLoggerConfiguration> config)
        : this(config, PowertoolsConfigurations.Instance, SystemWrapper.Instance)
    {
    }

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new PowertoolsLogger(name,
            GetCurrentConfig,
            _systemWrapper));
    }

    private PowertoolsLoggerConfiguration GetCurrentConfig()
    {
        var config = _currentConfig;

        ApplyPowertoolsConfig(config);

        return config;
    }

    private void ApplyPowertoolsConfig(PowertoolsLoggerConfiguration config)
    {
        var logLevel = _powertoolsConfigurations.GetLogLevel(config.MinimumLevel);
        var lambdaLogLevel = _powertoolsConfigurations.GetLambdaLogLevel();
        var lambdaLogLevelEnabled = _powertoolsConfigurations.LambdaLogLevelEnabled();

        if (lambdaLogLevelEnabled && logLevel < lambdaLogLevel)
        {
            _systemWrapper.LogLine(
                $"Current log level ({logLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({lambdaLogLevel}). This can lead to data loss, consider adjusting them.");
        }

        // // Set service
        config.Service ??= _powertoolsConfigurations.Service;


        // // Set output case
        if (config.LoggerOutputCase == LoggerOutputCase.Default)
        {
            var loggerOutputCase = _powertoolsConfigurations.GetLoggerOutputCase(config.LoggerOutputCase);
            config.LoggerOutputCase = loggerOutputCase;
            // TODO: Fix this
        }

        PowertoolsLoggingSerializer.ConfigureNamingPolicy(config.LoggerOutputCase);

        //

        //
        // // Set log level
        // var minLogLevel = lambdaLogLevelEnabled ? lambdaLogLevel : logLevel;
        // config.MinimumLevel = minLogLevel;

        config.LogLevelKey = _powertoolsConfigurations.LambdaLogLevelEnabled() &&
                             config.LoggerOutputCase == LoggerOutputCase.PascalCase
            ? "LogLevel"
            : LoggingConstants.KeyLogLevel;

        // Set sampling rate
        // var samplingRate = config.SamplingRate > 0 ? config.SamplingRate : _powertoolsConfigurations.LoggerSampleRate;
        // samplingRate = ValidateSamplingRate(samplingRate, minLogLevel, _systemWrapper);
        //
        // config.SamplingRate = samplingRate;
        //
        // if (samplingRate > 0)
        // {
        //     double sample = _systemWrapper.GetRandom();
        //
        //     if (sample <= samplingRate)
        //     {
        //         _systemWrapper.LogLine(
        //             $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate}, Sampler Value: {sample}.");
        //         config.MinimumLevel = LogLevel.Debug;
        //     }
        // }
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