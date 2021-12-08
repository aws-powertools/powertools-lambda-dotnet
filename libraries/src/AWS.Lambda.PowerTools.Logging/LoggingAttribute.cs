using System;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging
{
    [AttributeUsage(AttributeTargets.Method)]
    public class LoggingAttribute : MethodAspectAttribute
    {
        private LogLevel? _logLevel;
        private double? _samplingRate;
        
        public string ServiceName { get; set; }
        public bool LogEvent { get; set; }

        public LogLevel LogLevel
        {
            get => _logLevel ?? PowerToolsConfigurationsExtension.DefaultLogLevel;
            set => _logLevel = value;
        }
        public double SamplingRate
        {
            get => _samplingRate.GetValueOrDefault();
            set => _samplingRate = value;
        }

        protected override IMethodAspectHandler CreateHandler()
        {
            return new LoggingAspectHandler
            (
                ServiceName,
                _logLevel,
                _samplingRate,
                LogEvent,
                PowerToolsConfigurations.Instance,
                SystemWrapper.Instance
            );
        }
    }
}