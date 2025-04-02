using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Extension methods for configuring the Powertools logger
/// </summary>
public static class PowertoolsLoggingBuilderExtensions
{
    private static readonly ConcurrentBag<PowertoolsLoggerProvider> AllProviders = new();
    private static readonly object _lock = new();
    private static PowertoolsLoggerConfiguration _currentConfig = new();

    internal static void UpdateConfiguration(PowertoolsLoggerConfiguration config)
    {
        lock (_lock)
        {
            // Update the shared configuration
            _currentConfig = config;

            // Notify all providers about the change
            foreach (var provider in AllProviders)
            {
                provider.UpdateConfiguration(config);
            }
        }
    }
    
    internal static PowertoolsLoggerConfiguration GetCurrentConfiguration()
    {
        lock (_lock)
        {
            // Return a copy to prevent external modification
            return _currentConfig.Clone();
        }
    }

    public static ILoggingBuilder AddPowertoolsLogger(
        this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddSingleton<IPowertoolsEnvironment, PowertoolsEnvironment>();
        builder.Services.TryAddSingleton<IPowertoolsConfigurations>(sp =>
            new PowertoolsConfigurations(sp.GetRequiredService<IPowertoolsEnvironment>()));

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, PowertoolsLoggerProvider>(provider =>
            {
                var powertoolsConfigurations = provider.GetRequiredService<IPowertoolsConfigurations>();

                var loggerProvider = new PowertoolsLoggerProvider(
                    _currentConfig, 
                    powertoolsConfigurations);
                
                lock (_lock)
                {
                    AllProviders.Add(loggerProvider);
                }

                return loggerProvider;
            }));
    
        LoggerProviderOptions.RegisterProviderOptions
            <PowertoolsLoggerConfiguration, PowertoolsLoggerProvider>(builder.Services);

        return builder;
    }

    /// <summary>
    ///     Adds the Powertools logger to the logging builder.
    /// </summary>
    public static ILoggingBuilder AddPowertoolsLogger(
        this ILoggingBuilder builder,
        Action<PowertoolsLoggerConfiguration> configure)
    {
        // Add configuration
        builder.AddPowertoolsLogger();

        // Create initial configuration
        var options = new PowertoolsLoggerConfiguration();
        configure(options);

        // IMPORTANT: Set the minimum level directly on the builder
        if (options.MinimumLogLevel != LogLevel.None)
        {
            builder.SetMinimumLevel(options.MinimumLogLevel);
        }

        builder.Services.Configure(configure);

        UpdateConfiguration(options);

        // If buffering is enabled, register buffer providers
        if (options?.LogBuffering?.Enabled == true)
        {
            // Add a filter for the buffer provider
            builder.AddFilter<BufferingLoggerProvider>(
                null,
                options.LogBuffering.BufferAtLogLevel);

            // Register the buffer provider as an enumerable service
            // Using singleton to ensure it's properly tracked
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, BufferingLoggerProvider>(provider =>
                {
                    var powertoolsConfigurations = provider.GetRequiredService<IPowertoolsConfigurations>();

                    var bufferingProvider = new BufferingLoggerProvider(
                        _currentConfig, powertoolsConfigurations
                    );
                
                    lock (_lock)
                    {
                        AllProviders.Add(bufferingProvider);
                    }

                    return bufferingProvider;
                }));
        }
        

        return builder;
    }
    
    /// <summary>
    ///     Resets all providers and clears the configuration.
    ///     This is useful for testing purposes to ensure a clean state.
    /// </summary>
    internal static void ResetAllProviders()
    {
        lock (_lock)
        {
            // Clear the provider collection
            AllProviders.Clear();

            // Reset the current configuration to default
            _currentConfig = new PowertoolsLoggerConfiguration();
        }
    }
}