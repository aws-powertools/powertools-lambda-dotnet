using Microsoft.Extensions.Logging;

namespace Amazon.LambdaPowertools.Logging
{
    public class LoggerOptions
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;
        public double SamplingRate { get; set; } = 0.0;
        public string Service { get; set; } = "service_undefined";
    }
}