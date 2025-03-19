using System;
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
        Action<PowertoolsLoggerConfiguration>? configure = null,
        bool fromLoggerConfigure = false)
    {
        // Add configuration
        builder.AddConfiguration();

        // Apply configuration if provided
        if (configure != null)
        {
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

            // Register services
            RegisterServices(builder);

            // Apply the output case configuration
            PowertoolsLoggingSerializer.ConfigureNamingPolicy(options.LoggerOutputCase);

            // Configure static Logger (if not already in a configuration cycle)
            if (!fromLoggerConfigure && !_configuring)
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
        }
        else
        {
            // Register services even if no configuration was provided
            RegisterServices(builder);
        }

        return builder;
    }

    private static void RegisterServices(ILoggingBuilder builder)
    {
        // Register ISystemWrapper if not already registered
        builder.Services.TryAddSingleton<ISystemWrapper, SystemWrapper>();
        
        // Register IPowertoolsEnvironment if it exists
        builder.Services.TryAddSingleton<IPowertoolsEnvironment, PowertoolsEnvironment>();
        
        // Register IPowertoolsConfigurations with all its dependencies
        builder.Services.TryAddSingleton<IPowertoolsConfigurations>(sp => 
            new PowertoolsConfigurations(sp.GetRequiredService<IPowertoolsEnvironment>()));

        // Register the provider
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <PowertoolsLoggerConfiguration, LoggerProvider>(builder.Services);
    }
}