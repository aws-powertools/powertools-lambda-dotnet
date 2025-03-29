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
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class Logger.
/// </summary>
public static partial class Logger
{
    private static readonly object _lock = new object();

    // Static constructor to ensure initialization happens before first use
    static Logger()
    {
        LoggerInstance = GetPowertoolsLogger();
        // Factory and logger will be lazily initialized when first accessed
    }

    // Get the current logger instance
    private static ILogger LoggerInstance;

    /// <summary>
    /// Configure with an existing logger factory
    /// </summary>
    /// <param name="loggerFactory">The factory to use</param>
    internal static void Configure(ILoggerFactory loggerFactory)
    {
        if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
        LoggerFactoryHolder.SetFactory(loggerFactory);
    }

    /// <summary>
    /// Configure using a configuration action
    /// </summary>
    /// <param name="configure"></param>
    internal static void Configure(Action<PowertoolsLoggerConfiguration> configure)
    {
        lock (_lock)
        {
            var config = GetCurrentConfiguration();
            configure(config);
            PowertoolsLoggingBuilderExtensions.UpdateConfiguration(config);
        }
    }
    
    public static PowertoolsLoggerConfiguration GetCurrentConfiguration()
    {
        return PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
    }


    // /// <summary>
    // /// Get a logger for a specific type
    // /// </summary>
    // /// <typeparam name="T">The type to create logger for</typeparam>
    // /// <returns>A configured logger</returns>
    // internal static ILogger GetLogger<T>() => GetLogger(typeof(T).Name);
    //
    // /// <summary>
    // /// Get a logger for a specific category
    // /// </summary>
    // /// <param name="category">The category name</param>
    // /// <returns>A configured logger</returns>
    // internal static ILogger GetLogger(string category)
    // {
    //     return LoggerFactoryHolder.GetOrCreateFactory().CreateLogger(category);
    // }
    //
    /// <summary>
    /// Get the Powertools logger instance
    /// </summary>
    /// <returns>The configured Powertools logger</returns>
    internal static ILogger GetPowertoolsLogger()
    {
        return LoggerFactoryHolder.GetOrCreateFactory().CreatePowertoolsLogger();
    }

    /// <summary>
    /// Configure logger output case (snake_case, camelCase, PascalCase)
    /// </summary>
    /// <param name="outputCase">The case to use for the output</param>
    public static void UseOutputCase(LoggerOutputCase outputCase)
    {
        Configure(config => {
            config.LoggerOutputCase = outputCase;
        });
    }

    /// <summary>
    /// Configures the minimum log level
    /// </summary>
    /// <param name="logLevel">The minimum log level to display</param>
    public static void UseMinimumLogLevel(LogLevel logLevel)
    {
        Configure(config => {
            config.MinimumLogLevel = logLevel;
        });
    }

    /// <summary>
    /// Configures the service name
    /// </summary>
    /// <param name="serviceName">The service name to use in logs</param>
    public static void UseServiceName(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        Configure(config => {
            config.Service = serviceName;
        });
    }

    /// <summary>
    /// Sets the sampling rate for logs
    /// </summary>
    /// <param name="samplingRate">The rate (0.0 to 1.0) for sampling</param>
    public static void UseSamplingRate(double samplingRate)
    {
        if (samplingRate < 0 || samplingRate > 1)
            throw new ArgumentOutOfRangeException(nameof(samplingRate), "Sampling rate must be between 0 and 1");

        Configure(config => {
            config.SamplingRate = samplingRate;
        });
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

        Configure(config => {
            config.LogBuffering = logBuffering;
        });
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

        Configure(config => {
            config.JsonOptions = jsonOptions;
        });
    }
#endif
    
    /// <summary>
    /// Reset the logger for testing
    /// </summary>
    internal static void Reset()
    {
        LoggerFactoryHolder.Reset();
    }
}