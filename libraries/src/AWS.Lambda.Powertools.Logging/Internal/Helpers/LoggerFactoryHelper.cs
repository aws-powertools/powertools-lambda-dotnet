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
                config.Service = configuration.Service;
                config.SamplingRate = configuration.SamplingRate;
                config.MinimumLogLevel = configuration.MinimumLogLevel;
                config.LoggerOutputCase = configuration.LoggerOutputCase;
                config.JsonOptions = configuration.JsonOptions;
                config.TimestampFormat = configuration.TimestampFormat;
                config.LogFormatter = configuration.LogFormatter;
                config.LogLevelKey = configuration.LogLevelKey;
                config.LogBuffering = configuration.LogBuffering;
            });
        });

        // Configure the static logger with the factory
        // Logger.Configure(factory);

        return factory;
    }
}