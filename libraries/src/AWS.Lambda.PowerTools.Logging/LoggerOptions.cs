using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging
{
    public class LoggerOptions
    {
        public LogLevel LogLevel { get; set; }
        public double SamplingRate { get; set; }
        public string Service { get; set; }
        public LoggerOptions()
        {
            string envLogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");// ?? "Information";
            var isLogLevel = Enum.TryParse(envLogLevel, out LogLevel parseLogLevel) & Enum.IsDefined(typeof(LogLevel), parseLogLevel);
            LogLevel = (isLogLevel) ? parseLogLevel : LogLevel.Information;

            string envSamplingRate = Environment.GetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE");// ?? "0.0";
            SamplingRate = Double.Parse(envSamplingRate ?? "0.0");

            Service = Environment.GetEnvironmentVariable("POWERTOOLS_SERVICE_NAME") ?? "service_undefined";
        }


    }
}