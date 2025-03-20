using System;
using System.Linq;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging;

public static class BuilderExtensions
{
    // Track if we're in the middle of configuration to prevent recursion
    private static bool _configuring = false;

    // Single base method that all other overloads call
    public static ILoggingBuilder AddPowertoolsLogger(
        this ILoggingBuilder builder,
        Action<PowertoolsLoggerConfiguration>? configure = null)
    {
        // Add configuration
        builder.AddConfiguration();

        // If no configuration was provided, register services with defaults
        if (configure == null)
        {
            RegisterServices(builder);
            return builder;
        }
        
        // Create initial configuration
        var options = new PowertoolsLoggerConfiguration();
        configure(options);

        // IMPORTANT: Set the minimum level directly on the builder
        if (options.MinimumLogLevel != LogLevel.None)
        {
            builder.SetMinimumLevel(options.MinimumLogLevel);
        }

        // Configure options for DI
        builder.Services.Configure(configure);

        // Register services with the options
        RegisterServices(builder, options);

        // Apply the output case configuration
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(options.LoggerOutputCase);

        // Configure static Logger (if not already in a configuration cycle)
        if (!_configuring)
        {
            try
            {
                _configuring = true;
                Logger.Configure(options);
            }
            finally
            {
                _configuring = false;
            }
        }

        return builder;
    }

    private static void RegisterServices(ILoggingBuilder builder, PowertoolsLoggerConfiguration options = null)
    {
        // Register ISystemWrapper if not already registered
        builder.Services.TryAddSingleton<ISystemWrapper, SystemWrapper>();

        // Register IPowertoolsEnvironment if it exists
        builder.Services.TryAddSingleton<IPowertoolsEnvironment, PowertoolsEnvironment>();

        // Register IPowertoolsConfigurations with all its dependencies
        builder.Services.TryAddSingleton<IPowertoolsConfigurations>(sp =>
            new PowertoolsConfigurations(sp.GetRequiredService<IPowertoolsEnvironment>()));

        // If buffering is enabled, register it before the standard provider
        if (options?.LogBufferingOptions?.Enabled == true)
        {
            // Add a filter for the buffer provider to capture logs at the buffer threshold
            builder.AddFilter<BufferingLoggerProvider>(
                null, 
                options.LogBufferingOptions.BufferAtLogLevel);

            // Register the buffering provider
            builder.Services.AddSingleton<ILoggerProvider>(sp =>
            {
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<PowertoolsLoggerConfiguration>>();
                var powertoolsConfigs = sp.GetService<IPowertoolsConfigurations>() ?? 
                                       new PowertoolsConfigurations(sp.GetService<IPowertoolsEnvironment>() ?? 
                                                                  new PowertoolsEnvironment());
                var output = sp.GetService<ISystemWrapper>() ?? new SystemWrapper();
        
                // Create a dedicated provider for buffering
                var powerToolsProvider = new PowertoolsLoggerProvider(optionsMonitor, powertoolsConfigs, output);
        
                // Return the buffering provider
                return new BufferingLoggerProvider(
                    powerToolsProvider,
                    optionsMonitor);
            });
        }

        // Register the regular provider
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, PowertoolsLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <PowertoolsLoggerConfiguration, PowertoolsLoggerProvider>(builder.Services);
    }
}