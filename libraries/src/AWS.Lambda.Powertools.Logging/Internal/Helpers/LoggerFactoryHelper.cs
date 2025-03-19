using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal.Helpers;

/// <summary>
/// Helper class for creating and configuring logger factories
/// </summary>
internal static class LoggerFactoryHelper
{
    /// <summary>
    /// Creates and configures a logger factory with the provided configuration
    /// </summary>
    /// <param name="configuration">The Powertools logger configuration to apply</param>
    /// <returns>The configured logger factory</returns>
    public static ILoggerFactory CreateAndConfigureFactory(PowertoolsLoggerConfiguration configuration)
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.CopyFrom(configuration);
            });
        });

        // Configure the static logger with the factory
        Logger.Configure(factory);
        
        // Apply formatter if one is specified
        if (configuration.LogFormatter != null)
        {
            Logger.UseFormatter(configuration.LogFormatter);
        }
        
        return factory;
    }
}