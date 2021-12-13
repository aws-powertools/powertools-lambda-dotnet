using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.PowerTools.Logging
{
    public class LoggerConfiguration : IOptions<LoggerConfiguration>
    {
        public string Service { get; set; }
        public LogLevel? MinimumLevel { get; set; }
        public double? SamplingRate { get; set; }
        
        LoggerConfiguration IOptions<LoggerConfiguration>.Value => this;
    }
}