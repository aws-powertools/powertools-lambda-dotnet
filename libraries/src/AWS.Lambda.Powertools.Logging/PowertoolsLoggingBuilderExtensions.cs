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

    public static void UpdateConfiguration(PowertoolsLoggerConfiguration config)
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
    
    public static PowertoolsLoggerConfiguration GetCurrentConfiguration()
    {
        lock (_lock)
        {
            // Return a copy to prevent external modification
            return new PowertoolsLoggerConfiguration
            {
                Service = _currentConfig.Service,
                SamplingRate = _currentConfig.SamplingRate,
                MinimumLogLevel = _currentConfig.MinimumLogLevel,
                LoggerOutputCase = _currentConfig.LoggerOutputCase,
                JsonOptions = _currentConfig.JsonOptions,
                TimestampFormat = _currentConfig.TimestampFormat,
                LogFormatter = _currentConfig.LogFormatter,
                LogLevelKey = _currentConfig.LogLevelKey,
                LogBuffering = _currentConfig.LogBuffering
                
            };
        }
    }

    public static ILoggingBuilder AddPowertoolsLogger(
        this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        // Register ISystemWrapper if not already registered
        // builder.Services.TryAddSingleton<ISystemWrapper>(provider =>
        // {
        //     // Check if there's a pending mock system first
        //     var mockSystem = PowertoolsLoggerTestFixture.GetSystemWrapper();
        //     return mockSystem ?? new SystemWrapper();
        // });
        
        builder.Services.TryAddSingleton<ISystemWrapper, SystemWrapper>();
        
        builder.Services.TryAddSingleton<IPowertoolsEnvironment, PowertoolsEnvironment>();
        // Register IPowertoolsConfigurations with all its dependencies
        builder.Services.TryAddSingleton<IPowertoolsConfigurations>(sp =>
            new PowertoolsConfigurations(sp.GetRequiredService<IPowertoolsEnvironment>()));

        // Register the regular provider
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, PowertoolsLoggerProvider>(provider =>
            {
                var powertoolsConfigurations = provider.GetRequiredService<IPowertoolsConfigurations>();
                var systemWrapper = provider.GetRequiredService<ISystemWrapper>();

                var loggerProvider = new PowertoolsLoggerProvider(
                    new TrackedOptionsMonitor(_currentConfig, UpdateConfiguration), powertoolsConfigurations,
                    systemWrapper);
                lock (_lock)
                {
                    AllProviders.Add(loggerProvider);
                }

                return loggerProvider;
            }));
        
        builder.Services.ConfigureOptions<ConfigureLoggingOptions>();

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
                    var systemWrapper = provider.GetRequiredService<ISystemWrapper>();

                    var bufferingProvider = new BufferingLoggerProvider(
                        new TrackedOptionsMonitor(_currentConfig, UpdateConfiguration),
                        powertoolsConfigurations,
                        systemWrapper
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
    
    internal static void UpdateSystemInAllProviders(ISystemWrapper system)
    {
        if (system == null) return;
    
        lock (_lock)
        {
            foreach (var provider in AllProviders)
            {
                provider.UpdateSystem(system);
            }
        }
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
    
    private class ConfigureLoggingOptions : IConfigureOptions<LoggerFilterOptions>
    {
        private readonly IPowertoolsConfigurations _configurations;
        private readonly ISystemWrapper _systemWrapper;

        public ConfigureLoggingOptions(IPowertoolsConfigurations configurations, ISystemWrapper systemWrapper)
        {
            _configurations = configurations;
            _systemWrapper = systemWrapper;
        }

        public void Configure(LoggerFilterOptions options)
        {
            // This runs when IOptions<LoggerFilterOptions> is resolved
            LoggerFactoryHolder.ConfigureFromEnvironment(_configurations,_systemWrapper);
        }
    }

    private class TrackedOptionsMonitor : IOptionsMonitor<PowertoolsLoggerConfiguration>
    {
        private PowertoolsLoggerConfiguration _config;
        private readonly Action<PowertoolsLoggerConfiguration> _updateCallback;
        private readonly List<Action<PowertoolsLoggerConfiguration, string>> _listeners = new();

        public TrackedOptionsMonitor(
            PowertoolsLoggerConfiguration config,
            Action<PowertoolsLoggerConfiguration> updateCallback)
        {
            _config = config;
            _updateCallback = updateCallback;
        }

        public PowertoolsLoggerConfiguration CurrentValue => _config;

        public IDisposable OnChange(Action<PowertoolsLoggerConfiguration, string?> listener)
        {
            _listeners.Add(listener);
            return new ListenerDisposable(_listeners, listener);
        }

        public PowertoolsLoggerConfiguration Get(string? name) => _config;

        private class ListenerDisposable : IDisposable
        {
            private readonly List<Action<PowertoolsLoggerConfiguration, string?>> _listeners;
            private readonly Action<PowertoolsLoggerConfiguration, string?> _listener;

            public ListenerDisposable(
                List<Action<PowertoolsLoggerConfiguration, string?>> listeners,
                Action<PowertoolsLoggerConfiguration, string?> listener)
            {
                _listeners = listeners;
                _listener = listener;
            }

            public void Dispose()
            {
                _listeners.Remove(_listener);
            }
        }
    }
}