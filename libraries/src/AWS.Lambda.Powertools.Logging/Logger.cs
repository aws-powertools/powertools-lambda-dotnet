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
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    // static Logger()
    // {
    //     // Create default configuration with sensible defaults
    //     var defaultConfig = new PowertoolsLoggerConfiguration
    //     {
    //         MinimumLevel = LogLevel.Information, // Default to Information level
    //         Service = "LambdaFunction",          // Default service name
    //         LoggerOutputCase = LoggerOutputCase.SnakeCase,  // Default case
    //         SamplingRate = 1.0                  // Default to log everything
    //     };
    //     
    //     // Initialize with default factory
    //     _factoryLazy = new Lazy<ILoggerFactory>(() => 
    //         LoggerFactory.Create(builder => 
    //             builder.AddPowertoolsLogger(config => 
    //             {
    //                 config.MinimumLevel = defaultConfig.MinimumLevel;
    //                 config.Service = defaultConfig.Service;
    //                 config.LoggerOutputCase = defaultConfig.LoggerOutputCase;
    //                 config.SamplingRate = defaultConfig.SamplingRate;
    //             })),
    //         LazyThreadSafetyMode.ExecutionAndPublication);
    //     
    //     _defaultLoggerLazy = new Lazy<ILogger>(() => 
    //         _factoryLazy.Value.CreateLogger("PowertoolsLogger"));
    //         
    //     // Not yet explicitly configured
    //     _isConfigured = false;
    // }

    // Flag to track if custom configuration has been applied
    private static bool _isConfigured;

    // Properties to access the lazy-initialized instances
    private static ILoggerFactory Factory => _factoryLazy.Value;
    private static ILogger LoggerInstance => _defaultLoggerLazy.Value;

    /// <summary>
    /// Indicates whether the Logger has been configured with custom settings
    /// </summary>
    public static bool IsConfigured => _isConfigured;


    // Allow manual configuration using options
    public static void Configure(Action<PowertoolsLoggerConfiguration> configureOptions)
    {
        var options = new PowertoolsLoggerConfiguration();
        configureOptions(options);
        Configure(options);
    }
    
    // Configure with existing factory
    public static void Configure(ILoggerFactory loggerFactory)
    {
        Interlocked.Exchange(ref _factoryLazy,
            new Lazy<ILoggerFactory>(() => loggerFactory));

        Interlocked.Exchange(ref _defaultLoggerLazy,
            new Lazy<ILogger>(() => Factory.CreateLogger("PowertoolsLogger")));

        _isConfigured = true;
    }

    // Directly configure from a PowertoolsLoggerConfiguration
    internal static void Configure(PowertoolsLoggerConfiguration options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        // Create a factory with our provider
        var factory = LoggerFactory.Create(builder => 
        {
            // Use AddPowertoolsLogger but with fromLoggerConfigure=true to prevent recursion
            builder.AddPowertoolsLogger(config => 
            {
                config.Service = options.Service;
                config.MinimumLevel = options.MinimumLevel;
                config.LoggerOutputCase = options.LoggerOutputCase;
                config.SamplingRate = options.SamplingRate;
                // Copy other properties as needed
            }, true);
        });

        // Update factory and logger
        Interlocked.Exchange(ref _factoryLazy,
            new Lazy<ILoggerFactory>(() => factory));

        Interlocked.Exchange(ref _defaultLoggerLazy,
            new Lazy<ILogger>(() => Factory.CreateLogger("PowertoolsLogger")));

        _isConfigured = true;
    }


    // Get a logger for a specific category
    public static ILogger GetLogger<T>() => GetLogger(typeof(T).Name);

    public static ILogger GetLogger(string category) => Factory.CreateLogger(category);
    
    // For testing purposes
    // internal static void Reset()
    // {
    //     Interlocked.Exchange(ref _factoryLazy, 
    //         new Lazy<PowertoolsLoggerFactory>(() => new PowertoolsLoggerFactory()));
        
    //     Interlocked.Exchange(ref _defaultLoggerLazy,
    //         new Lazy<ILogger>(() => Factory.CreateLogger<PowertoolsLogger>()));
    // }
}