using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Extension methods for configuring the Powertools logger
/// </summary>
public static class BuilderExtensions
{
    // Track if we're in the middle of configuration to prevent recursion
    private static bool _configuring = false;

    /// <summary>
    ///     Adds the Powertools logger to the logging builder.
    /// </summary>
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

        // If buffering is enabled, register buffer providers
        if (options?.LogBuffering?.Enabled == true)
        {
            // Add a filter for the buffer provider
            builder.AddFilter<BufferingLoggerProvider>(
                null, 
                options.LogBuffering.BufferAtLogLevel);
                
            // Register the inner provider factory
            builder.Services.TryAddSingleton<ILoggerProvider>(sp => 
                new BufferingLoggerProvider(
                    // Create a new PowertoolsLoggerProvider specifically for buffering
                    new PowertoolsLoggerProvider(
                        sp.GetRequiredService<IOptionsMonitor<PowertoolsLoggerConfiguration>>(),
                        sp.GetRequiredService<IPowertoolsConfigurations>(),
                        sp.GetRequiredService<ISystemWrapper>()
                    ),
                    sp.GetRequiredService<IOptionsMonitor<PowertoolsLoggerConfiguration>>()
                )
            );
        }

        // Register the regular provider
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, PowertoolsLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <PowertoolsLoggerConfiguration, PowertoolsLoggerProvider>(builder.Services);
    }
}