using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
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

        // Register the provider
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <PowertoolsLoggerConfiguration, LoggerProvider>(builder.Services);

        // Apply configuration if provided
        if (configure != null)
        {
            // Create and apply configuration
            var options = new PowertoolsLoggerConfiguration();
            configure(options);
            
            // Configure options for DI
            builder.Services.Configure(configure);

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

        return builder;
    }
}