using System;
using System.Collections.Concurrent;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
/// Extension methods to configure and add the Powertools logger to an <see cref="ILoggingBuilder"/>.
/// </summary>
/// <remarks>
/// This class provides methods to integrate the AWS Lambda Powertools logging capabilities
/// with the standard .NET logging framework.
/// </remarks>
/// <example>
/// Basic usage:
/// <code>
/// builder.Logging.AddPowertoolsLogger();
/// </code>
/// </example>
public static class PowertoolsLoggingBuilderExtensions
{
    private static readonly ConcurrentBag<PowertoolsLoggerProvider> AllProviders = new();
    private static readonly object Lock = new();
    private static PowertoolsLoggerConfiguration _currentConfig = new();

    internal static void UpdateConfiguration(PowertoolsLoggerConfiguration config)
    {
        lock (Lock)
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
        lock (Lock)
        {
            // Return a copy to prevent external modification
            return _currentConfig;
        }
    }

    /// <summary>
    ///     Adds the Powertools logger to the logging builder with default configuration.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    /// <returns>The logging builder for further configuration.</returns>
    /// <remarks>
    ///     This method registers the Powertools logger with default settings. The logger will output 
    ///     structured JSON logs that integrate well with AWS CloudWatch and other log analysis tools.
    /// </remarks>
    /// <example>
    ///     Add the Powertools logger to your Lambda function:
    ///     <code>
    ///     var builder = new HostBuilder()
    ///         .ConfigureLogging(logging =>
    ///         {
    ///             logging.AddPowertoolsLogger();
    ///         });
    ///     </code>
    ///     
    ///     Using with minimal API:
    ///     <code>
    ///     var builder = WebApplication.CreateBuilder(args);
    ///     builder.Logging.AddPowertoolsLogger();
    ///     </code>
    /// </example>
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

                lock (Lock)
                {
                    AllProviders.Add(loggerProvider);
                }

                return loggerProvider;
            }));

        return builder;
    }

    /// <summary>
    ///     Adds the Powertools logger to the logging builder with default configuration.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    /// <returns>The logging builder for further configuration.</returns>
    /// <remarks>
    ///     This method registers the Powertools logger with default settings. The logger will output 
    ///     structured JSON logs that integrate well with AWS CloudWatch and other log analysis tools.
    /// </remarks>
    /// <example>
    ///     Add the Powertools logger to your Lambda function:
    ///     <code>
    ///     var builder = new HostBuilder()
    ///         .ConfigureLogging(logging =>
    ///         {
    ///             logging.AddPowertoolsLogger();
    ///         });
    ///     </code>
    ///     
    ///     Using with minimal API:
    ///     <code>
    ///     var builder = WebApplication.CreateBuilder(args);
    ///     builder.Logging.AddPowertoolsLogger();
    ///     </code>
    /// With custom configuration:
    ///     <code>
    ///     builder.Logging.AddPowertoolsLogger(options => 
    ///     {
    ///         options.MinimumLogLevel = LogLevel.Information;
    ///         options.LoggerOutputCase = LoggerOutputCase.PascalCase;
    ///         options.IncludeLogLevel = true;
    ///     });
    ///     </code>
    /// 
    ///     With log buffering:
    ///     <code>
    ///     builder.Logging.AddPowertoolsLogger(options => 
    ///     {
    ///         options.LogBuffering = new LogBufferingOptions
    ///         {
    ///             Enabled = true,
    ///             BufferAtLogLevel = LogLevel.Debug    
    ///         };
    ///     });
    ///     </code>
    /// </example>
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
        if (options.LogBuffering?.Enabled == true)
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

                    lock (Lock)
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
        lock (Lock)
        {
            // Clear the provider collection
            AllProviders.Clear();

            // Reset the current configuration to default
            _currentConfig = new PowertoolsLoggerConfiguration();
        }
    }
}