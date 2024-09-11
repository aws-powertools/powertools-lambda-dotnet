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
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class PowertoolsConfigurationsExtension.
/// </summary>
internal static class PowertoolsConfigurationsExtension
{
    /// <summary>
    ///     Gets the log level.
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="logLevel">The log level.</param>
    /// <returns>LogLevel.</returns>
    internal static LogLevel GetLogLevel(this IPowertoolsConfigurations powertoolsConfigurations,
        LogLevel? logLevel = null)
    {
        if (logLevel.HasValue)
            return logLevel.Value;

        if (Enum.TryParse((powertoolsConfigurations.LogLevel ?? "").Trim(), true, out LogLevel result))
            return result;

        return LoggingConstants.DefaultLogLevel;
    }

    /// <summary>
    /// Lambda Log Level Mapper
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <returns></returns>
    internal static LogLevel GetLambdaLogLevel(this IPowertoolsConfigurations powertoolsConfigurations)
    {
        AwsLogLevelMapper.TryGetValue((powertoolsConfigurations.AWSLambdaLogLevel ?? "").Trim().ToUpper(),
            out var awsLogLevel);

        if (Enum.TryParse(awsLogLevel, true, out LogLevel result))
        {
            return result;
        }

        return LogLevel.None;
    }

    /// <summary>
    /// Determines the logger output case based on configuration and input.
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools configurations.</param>
    /// <param name="loggerOutputCase">Optional explicit logger output case.</param>
    /// <returns>The determined LoggerOutputCase.</returns>
    internal static LoggerOutputCase GetLoggerOutputCase(this IPowertoolsConfigurations powertoolsConfigurations,
        LoggerOutputCase? loggerOutputCase = null)
    {
        if (loggerOutputCase.HasValue)
            return loggerOutputCase.Value;

        if (Enum.TryParse((powertoolsConfigurations.LoggerOutputCase ?? "").Trim(), true, out LoggerOutputCase result))
            return result;

        return LoggingConstants.DefaultLoggerOutputCase;
    }

    /// <summary>
    ///     Maps AWS log level to .NET log level
    /// </summary>
    private static readonly Dictionary<string, string> AwsLogLevelMapper = new()
    {
        { "TRACE", "TRACE" },
        { "DEBUG", "DEBUG" },
        { "INFO", "INFORMATION" },
        { "WARN", "WARNING" },
        { "ERROR", "ERROR" },
        { "FATAL", "CRITICAL" }
    };

    /// <summary>
    ///     Gets the current configuration.
    /// </summary>
    /// <returns>AWS.Lambda.Powertools.Logging.LoggerConfiguration.</returns>
    public static LoggerConfiguration SetCurrentConfig(this IPowertoolsConfigurations powertoolsConfigurations,
        LoggerConfiguration config, ISystemWrapper systemWrapper)
    {
        config ??= new LoggerConfiguration();

        var logLevel = powertoolsConfigurations.GetLogLevel(config?.MinimumLevel);
        var lambdaLogLevel = powertoolsConfigurations.GetLambdaLogLevel();
        var lambdaLogLevelEnabled = powertoolsConfigurations.LambdaLogLevelEnabled();

        if (lambdaLogLevelEnabled && logLevel < lambdaLogLevel)
        {
            systemWrapper.LogLine(
                $"Current log level ({logLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({lambdaLogLevel}). This can lead to data loss, consider adjusting them.");
        }

        // set service
        var service = config?.Service ?? powertoolsConfigurations.Service;
        config.Service = service;
        
        // set output case
        var loggerOutputCase = powertoolsConfigurations.GetLoggerOutputCase(config?.LoggerOutputCase);
        config.LoggerOutputCase = loggerOutputCase;
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(config.LoggerOutputCase);

        // log level
        var minLogLevel = logLevel;
        if (lambdaLogLevelEnabled)
        {
            minLogLevel = lambdaLogLevel;
        }

        config.MinimumLevel = minLogLevel;
        
        // set sampling rate
        config = SetSamplingRate(powertoolsConfigurations, config, systemWrapper, minLogLevel);
        return config;
    }

    /// <summary>
    /// Set sampling rate
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <param name="config"></param>
    /// <param name="systemWrapper"></param>
    /// <param name="minLogLevel"></param>
    /// <returns></returns>
    private static LoggerConfiguration SetSamplingRate(IPowertoolsConfigurations powertoolsConfigurations,
        LoggerConfiguration config, ISystemWrapper systemWrapper, LogLevel minLogLevel)
    {
        var samplingRate = config.SamplingRate ?? powertoolsConfigurations.LoggerSampleRate;
        config.SamplingRate = samplingRate;

        switch (samplingRate)
        {
            case null:
                return config;
            case < 0 or > 1:
            {
                if (minLogLevel is LogLevel.Debug or LogLevel.Trace)
                    systemWrapper.LogLine(
                        $"Skipping sampling rate configuration because of invalid value. Sampling rate: {samplingRate.Value}");
                config.SamplingRate = null;
                return config;
            }
            case 0:
                return config;
        }

        var sample = systemWrapper.GetRandom();
        
        if (!(samplingRate.Value > sample)) return config;
        
        systemWrapper.LogLine(
            $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate.Value}, Sampler Value: {sample}.");
        config.MinimumLevel = LogLevel.Debug;

        return config;
    }

    /// <summary>
    /// Determines whether [is lambda log level enabled].
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <returns></returns>
    internal static bool LambdaLogLevelEnabled(this IPowertoolsConfigurations powertoolsConfigurations)
    {
        return powertoolsConfigurations.GetLambdaLogLevel() != LogLevel.None;
    }
}