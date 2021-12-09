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
        private bool? _logEvent;
        
        public string ServiceName { get; set; }

        public LogLevel LogLevel
        {
            get => _logLevel ?? LoggingConstants.DefaultLogLevel;
            set => _logLevel = value;
        }
        public double SamplingRate
        {
            get => _samplingRate.GetValueOrDefault();
            set => _samplingRate = value;
        }
        
        public bool LogEvent
        {
            get => _logEvent.GetValueOrDefault();
            set => _logEvent = value;
        }
        
        public string CorrelationIdPath { get; set; }

        protected override IMethodAspectHandler CreateHandler()
        {
            return new LoggingAspectHandler
            (
                ServiceName,
                _logLevel,
                _samplingRate,
                _logEvent,
                CorrelationIdPath,
                PowerToolsConfigurations.Instance,
                SystemWrapper.Instance
            );
        }
    }
}