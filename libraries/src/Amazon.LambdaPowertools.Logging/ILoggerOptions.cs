using System;

namespace Amazon.LambdaPowertools.Logging
{
    public interface ILoggerOptions
    {
        /// <summary>
        /// Logging level for Logger, and also avialable as a logging key
        /// Default "INFO", env:LOG_LEVEL
        /// </summary>
        LogLevel Level { get; }

        /// <summary>
        /// Sampling rate for dynamically set log level as DEBUG for a given request
        /// Default 0.0, env:POWERTOOLS_LOGGER_SAMPLE_RATE
        /// </summary>
        double SamplingRate { get; set; }
        
        /// <summary>
        /// Service metadata that will be used as a logging key
        /// Default "service_undefined", env:POWERTOOLS_SERVICE_NAME
        /// </summary>
        string Service { get; set; }
    }
}