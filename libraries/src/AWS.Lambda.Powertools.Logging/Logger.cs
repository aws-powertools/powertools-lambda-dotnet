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

    // Add a backing field
    private static bool _isConfigured = false;

    // Properties to access the lazy-initialized instances
    private static ILoggerFactory Factory => _factoryLazy.Value;
    private static ILogger LoggerInstance => _defaultLoggerLazy.Value;

    /// <summary>
    /// Gets a value indicating whether the logger is configured.
    /// </summary>
    /// <value><c>true</c> if the logger is configured; otherwise, <c>false</c>.</value>
    public static bool IsConfigured => _isConfigured;

    // Add this field to the Logger class
    private static PowertoolsLoggerConfiguration _currentConfig;

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

        // Store the configuration
        _currentConfig = options.Clone();
        
        // Create a system wrapper if needed
        var systemWrapper = options.LoggerOutput ?? new SystemWrapper();

        // Create a factory with our provider
        var factory = LoggerFactory.Create(builder => 
        {
            // Set minimum level directly on builder
            if (options.MinimumLevel != LogLevel.None)
            {
                builder.SetMinimumLevel(options.MinimumLevel);
            }
            
            // Add our provider - the config's OutputLogger will be used
            builder.Services.AddSingleton<ISystemWrapper>(systemWrapper);
            
            builder.AddPowertoolsLogger(config => 
            {
                config.Service = _currentConfig.Service;
                config.MinimumLevel = _currentConfig.MinimumLevel;
                config.LoggerOutputCase = _currentConfig.LoggerOutputCase;
                config.SamplingRate = _currentConfig.SamplingRate;
                config.LoggerOutput = _currentConfig.LoggerOutput;
                config.JsonOptions = _currentConfig.JsonOptions;
#if NET8_0_OR_GREATER
                config.JsonContext = _currentConfig.JsonContext;
#endif
            }, true);
        });

        // Update factory and logger
        Interlocked.Exchange(ref _factoryLazy,
            new Lazy<ILoggerFactory>(() => factory));

        Interlocked.Exchange(ref _defaultLoggerLazy,
            new Lazy<ILogger>(() => Factory.CreateLogger("PowertoolsLogger")));

        _isConfigured = true;
    }

    // Add this method to the Logger class
    // Get the current configuration
    public static PowertoolsLoggerConfiguration GetConfiguration()
    {
        // Ensure logger is initialized
        _ = LoggerInstance;
        
        // Create a new configuration with current settings
        if (_currentConfig == null)
        {
            _currentConfig = new PowertoolsLoggerConfiguration();
        }
        
        return _currentConfig;
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