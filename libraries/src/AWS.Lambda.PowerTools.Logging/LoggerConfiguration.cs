using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.PowerTools.Logging
{
    public class LoggerConfiguration : IOptions<LoggerConfiguration>
    {
        /// <summary>
        /// Service name is used for logging.
        /// This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
        /// </summary>
        public string Service { get; set; }
        
        /// <summary>
        /// Specify the minimum log level for logging (Information, by default).
        /// This can be also set using the environment variable <c>LOG_LEVEL</c>.
        /// </summary>
        public LogLevel? MinimumLevel { get; set; }
        
        /// <summary>
        /// Dynamically set a percentage of logs to DEBUG level.
        /// This can be also set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
        /// </summary>
        public double? SamplingRate { get; set; }
        
        LoggerConfiguration IOptions<LoggerConfiguration>.Value => this;
    }
}