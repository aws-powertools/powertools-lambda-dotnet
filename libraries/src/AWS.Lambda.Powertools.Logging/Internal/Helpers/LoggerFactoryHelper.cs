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
    internal static ILoggerFactory CreateAndConfigureFactory(PowertoolsLoggerConfiguration configuration)
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = configuration.Service;
                config.TimestampFormat = configuration.TimestampFormat;
                config.MinimumLogLevel = configuration.MinimumLogLevel;
                config.InitialLogLevel = configuration.InitialLogLevel;
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
            
            // When sampling is enabled, set the factory minimum level to Debug
            // so that all logs can reach our PowertoolsLogger for dynamic filtering
            if (configuration.SamplingRate > 0)
            {
                builder.AddFilter(null, LogLevel.Debug);
                builder.SetMinimumLevel(LogLevel.Debug);
            }
            else if (configuration.MinimumLogLevel != LogLevel.None)
            {
                builder.AddFilter(null, configuration.MinimumLogLevel);
                builder.SetMinimumLevel(configuration.MinimumLogLevel);
            }
            else
            {
                // No explicit level configured — let everything through the factory
                // so the provider can filter based on POWERTOOLS_LOG_LEVEL env var
                builder.AddFilter(null, LogLevel.Trace);
                builder.SetMinimumLevel(LogLevel.Trace);
            }
        });
        
        LoggerFactoryHolder.SetFactory(factory);

        return factory;
    }
}