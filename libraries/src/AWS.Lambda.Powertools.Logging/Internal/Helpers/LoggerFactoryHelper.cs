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
                config.TimestampFormat = configuration.TimestampFormat;
                config.MinimumLogLevel = configuration.MinimumLogLevel;
                config.SamplingRate = configuration.SamplingRate;
                config.LoggerOutputCase = configuration.LoggerOutputCase;
                config.LogLevelKey = configuration.LogLevelKey;
                config.LogFormatter = configuration.LogFormatter;
                config.JsonOptions = configuration.JsonOptions;
                config.LogBuffering = configuration.LogBuffering;
                config.LogOutput = configuration.LogOutput;
                config.XRayTraceId = configuration.XRayTraceId;
                config.LogEvent = configuration.LogEvent;
            });
            
            // Use current filter level or level from config
            if (configuration.MinimumLogLevel != LogLevel.None)
            {
                builder.AddFilter(null, configuration.MinimumLogLevel);
                builder.SetMinimumLevel(configuration.MinimumLogLevel);
            }
        });
        
        LoggerFactoryHolder.SetFactory(factory);

        return factory;
    }
}