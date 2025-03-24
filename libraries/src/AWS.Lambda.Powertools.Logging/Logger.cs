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
using System.Text.Json;
using System.Threading;
using AWS.Lambda.Powertools.Common;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class Logger.
/// </summary>
public static partial class Logger
{
    // Use Lazy<T> for thread-safe initialization
    private static Lazy<ILoggerFactory> _factoryLazy;
    private static Lazy<ILogger> _defaultLoggerLazy;

    // Static constructor to ensure initialization
    static Logger()
    {
        // Initialize with default configuration (ensures we never have null fields)
        InitializeWithDefaults();
    }

    // Properties to access the lazy-initialized instances
    private static ILoggerFactory Factory => _factoryLazy.Value;
    private static ILogger LoggerInstance => _defaultLoggerLazy.Value;

    // Add this field to the Logger class
    private static PowertoolsLoggerConfiguration _currentConfig;

    // Initialize with default settings
    private static void InitializeWithDefaults()
    {
        _currentConfig = new PowertoolsLoggerConfiguration();

        // Create default factory with minimal configuration
        _factoryLazy = new Lazy<ILoggerFactory>(() =>
            PowertoolsLoggerFactory.Create(_currentConfig));

        _defaultLoggerLazy = new Lazy<ILogger>(() =>
            Factory.CreatePowertoolsLogger());
    }

    // Allow manual configuration using options
    internal static void Configure(Action<PowertoolsLoggerConfiguration> configureOptions)
    {
        var options = new PowertoolsLoggerConfiguration();
        configureOptions(options);
        Configure(options);
    }

    // Configure with existing factory
    internal static void Configure(ILoggerFactory loggerFactory)
    {
        Interlocked.Exchange(ref _factoryLazy,
            new Lazy<ILoggerFactory>(() => loggerFactory));

        Interlocked.Exchange(ref _defaultLoggerLazy,
            new Lazy<ILogger>(() => Factory.CreatePowertoolsLogger()));
    }

    // Directly configure from a PowertoolsLoggerConfiguration
    internal static void Configure(PowertoolsLoggerConfiguration options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        // Store current config
        _currentConfig = options;

        // Update factory and logger
        Interlocked.Exchange(ref _factoryLazy,
            new Lazy<ILoggerFactory>(() => PowertoolsLoggerFactory.Create(_currentConfig)));

        Interlocked.Exchange(ref _defaultLoggerLazy,
            new Lazy<ILogger>(() => Factory.CreatePowertoolsLogger()));
    }

    // Get the current configuration
    internal static PowertoolsLoggerConfiguration GetConfiguration()
    {
        // Ensure logger is initialized
        _ = LoggerInstance;

        return _currentConfig;
    }

    // Get a logger for a specific category
    internal static ILogger GetLogger<T>() => GetLogger(typeof(T).Name);

    internal static ILogger GetLogger(string category) => Factory.CreateLogger(category);
    
    internal static ILogger GetPowertoolsLogger() => Factory.CreatePowertoolsLogger();

    /// <summary>
    /// Sets a custom output for the static logger.
    /// Useful for testing to redirect logs to a test output.
    /// </summary>
    /// <param name="loggerOutput">The custom output implementation</param>
    public static void UseOutput(ISystemWrapper loggerOutput)
    {
        if (loggerOutput == null)
            throw new ArgumentNullException(nameof(loggerOutput));

        _currentConfig.LoggerOutput = loggerOutput;
        Configure(_currentConfig);
    }

    /// <summary>
    /// Configure logger output case (snake_case, camelCase, PascalCase)
    /// </summary>
    /// <param name="outputCase">The case to use for the output</param>
    public static void UseOutputCase(LoggerOutputCase outputCase)
    {
        _currentConfig.LoggerOutputCase = outputCase;
        Configure(_currentConfig);
    }
    
    /// <summary>
    /// Configures the minimum log level
    /// </summary>
    /// <param name="logLevel">The minimum log level to display</param>
    public static void UseMinimumLogLevel(LogLevel logLevel)
    {
        _currentConfig.MinimumLogLevel = logLevel;
        Configure(_currentConfig);
    }

    /// <summary>
    /// Configures the service name
    /// </summary>
    /// <param name="serviceName">The service name to use in logs</param>
    public static void UseServiceName(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));
        
        _currentConfig.Service = serviceName;
        Configure(_currentConfig);
    }

    /// <summary>
    /// Sets the sampling rate for logs
    /// </summary>
    /// <param name="samplingRate">The rate (0.0 to 1.0) for sampling</param>
    public static void UseSamplingRate(double samplingRate)
    {
        if (samplingRate < 0 || samplingRate > 1)
            throw new ArgumentOutOfRangeException(nameof(samplingRate), "Sampling rate must be between 0 and 1");
        
        _currentConfig.SamplingRate = samplingRate;
        Configure(_currentConfig);
    }

    /// <summary>
    /// Log buffering options.
    /// <code>
    /// Logger.UseLogBuffering(new LogBufferingOptions
    /// {
    ///     Enabled = true,
    ///     BufferAtLogLevel = LogLevel.Debug
    /// });
    /// </code>
    /// </summary>
    public static void UseLogBuffering(LogBufferingOptions logBuffering)
    {
        if (logBuffering == null)
            throw new ArgumentNullException(nameof(logBuffering));

        // Update the current configuration
        _currentConfig.LogBuffering = logBuffering;

        // Reconfigure to apply changes
        Configure(_currentConfig);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Configure JSON serialization options
    /// </summary>
    /// <param name="jsonOptions">The JSON options to use</param>
    public static void UseJsonOptions(JsonSerializerOptions jsonOptions)
    {
        if (jsonOptions == null)
            throw new ArgumentNullException(nameof(jsonOptions));
            
        // Update the current configuration
        _currentConfig.JsonOptions = jsonOptions;
    }
#endif

    // For testing purposes
    internal static void Reset()
    {
        InitializeWithDefaults();
    }
}