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
using System.Threading;
using AWS.Lambda.Powertools.Common;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Holds and manages the shared logger factory instance
/// </summary>
internal static class LoggerFactoryHolder
{
    private static ILoggerFactory? _factory;
    private static readonly object _lock = new object();
    private static bool _isConfigured = false;

    /// <summary>
    /// Gets or creates the shared logger factory
    /// </summary>
    public static ILoggerFactory GetOrCreateFactory()
    {
        lock (_lock)
        {
            if (_factory == null)
            {
                _factory = LoggerFactory.Create(builder => builder.AddPowertoolsLogger());
            }
            return _factory;
        }
    }
    
    public static void SetFactory(ILoggerFactory factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        lock (_lock)
        {
            _factory = factory;
            _isConfigured = true;
        }
    }

    /// <summary>
    /// Automatically called when GetOrCreateFactory is used
    /// </summary>
    public static void ConfigureFromEnvironment(IPowertoolsConfigurations configurations, ISystemWrapper systemWrapper)
    {
        // Only configure once
        if (_isConfigured) return;

        // Create initial configuration
        var config = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();

        // Apply environment configuration if available
        if (configurations != null)
        {
            ApplyPowertoolsConfig(config, configurations, systemWrapper);
            PowertoolsLoggingBuilderExtensions.UpdateConfiguration(config);
        }

        _isConfigured = true;
    }

    /// <summary>
    /// Apply Powertools configuration from environment variables to the logger configuration
    /// </summary>
    private static void ApplyPowertoolsConfig(PowertoolsLoggerConfiguration config, 
        IPowertoolsConfigurations configurations, ISystemWrapper systemWrapper)
    {
        var logLevel = configurations.GetLogLevel(LogLevel.None);
        var lambdaLogLevel = configurations.GetLambdaLogLevel();
        var lambdaLogLevelEnabled = configurations.LambdaLogLevelEnabled();

        // Check for explicit config
        bool hasExplicitLevel = config.MinimumLogLevel != LogLevel.None;

        // Warn if Lambda log level doesn't match
        if (lambdaLogLevelEnabled && hasExplicitLevel && config.MinimumLogLevel < lambdaLogLevel)
        {
            systemWrapper.LogLine(
                $"Current log level ({config.MinimumLogLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({lambdaLogLevel}). This can lead to data loss, consider adjusting them.");
        }

        // Set service from environment if not explicitly set
        if (string.IsNullOrEmpty(config.Service))
        {
            config.Service = configurations.Service;
        }

        // Set output case from environment if not explicitly set
        if (config.LoggerOutputCase == LoggerOutputCase.Default)
        {
            var loggerOutputCase = configurations.GetLoggerOutputCase(config.LoggerOutputCase);
            config.LoggerOutputCase = loggerOutputCase;
        }

        // Set log level from environment ONLY if not explicitly set
        if (!hasExplicitLevel)
        {
            var minLogLevel = lambdaLogLevelEnabled ? lambdaLogLevel : logLevel;
            config.MinimumLogLevel = minLogLevel != LogLevel.None ? minLogLevel : LoggingConstants.DefaultLogLevel;
        }
        
        config.XRayTraceId = configurations.XRayTraceId;
        config.LogEvent = configurations.LoggerLogEvent;
        
        // Configure the log level key based on output case
        config.LogLevelKey = configurations.LambdaLogLevelEnabled() &&
                             config.LoggerOutputCase == LoggerOutputCase.PascalCase
            ? "LogLevel"
            : LoggingConstants.KeyLogLevel;
            
        ProcessSamplingRate(config, configurations, systemWrapper);
    }

    /// <summary>
    /// Process sampling rate configuration
    /// </summary>
    private static void ProcessSamplingRate(PowertoolsLoggerConfiguration config, IPowertoolsConfigurations configurations, ISystemWrapper systemWrapper)
    {
        var samplingRate = config.SamplingRate > 0 
            ? config.SamplingRate 
            : configurations.LoggerSampleRate;
            
        samplingRate = ValidateSamplingRate(samplingRate, config.MinimumLogLevel, systemWrapper);
        config.SamplingRate = samplingRate;

        // Only notify if sampling is configured
        if (samplingRate > 0)
        {
            double sample = systemWrapper.GetRandom();
            
            // Instead of changing log level, just indicate sampling status
            if (sample <= samplingRate)
            {
                systemWrapper.LogLine(
                    $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate}, Sampler Value: {sample}.");
                config.MinimumLogLevel = LogLevel.Debug;
            }
        }
    }

    /// <summary>
    /// Validate sampling rate
    /// </summary>
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
    /// Resets the factory holder for testing
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            var oldFactory = Interlocked.Exchange(ref _factory, null);
            oldFactory?.Dispose();
            _isConfigured = false;
        }
    }
}