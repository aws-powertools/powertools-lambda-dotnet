using System;
using System.Linq;
using Amazon.Lambda.Core;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    internal class LoggingAspectHandler : IMethodAspectHandler
    {
        private readonly string _serviceName;
        private readonly LogLevel? _logLevel;
        private readonly double? _samplingRate;
        private readonly bool _logEvent;
        private readonly ISystemWrapper _systemWrapper;
        private readonly LogLevelHelper _logLevelHelper;

        private static bool _isColdStart = true;
        private static bool _initializeContext = true;
        private bool _isContextInitialized;
        
        internal LoggingAspectHandler
        (
            string serviceName,
            LogLevel? logLevel,
            double? samplingRate,
            bool logEvent,
            IPowerToolsConfigurations powerToolsConfigurations,
            ISystemWrapper systemWrapper
        )
        {
            _serviceName = serviceName;
            _logLevel = logLevel;
            _samplingRate = samplingRate;
            _logEvent = logEvent;
            _systemWrapper = systemWrapper;
            _logLevelHelper = new LogLevelHelper(powerToolsConfigurations);
        }

        private bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None && logLevel >= _logLevelHelper.GetLogLevel(_logLevel);
        
        public void OnEntry(AspectEventArgs eventArgs)
        {
            Logger.LoggerProvider ??= new LoggerProvider(
                new LoggerConfiguration
                {
                    ServiceName = _serviceName,
                    MinimumLevel = _logLevel,
                    SamplingRate = _samplingRate
                });

            if (!_initializeContext) return;
            
            Logger.AppendKey("ColdStart", _isColdStart);

            if (eventArgs.Args.FirstOrDefault(x => x is ILambdaContext) is ILambdaContext context)
            {
                Logger.AppendKey("FunctionName", context.FunctionName);
                Logger.AppendKey("FunctionVersion", context.FunctionVersion);
                Logger.AppendKey("FunctionMemorySize", context.MemoryLimitInMB);
                Logger.AppendKey("FunctionArn", context.InvokedFunctionArn);
                Logger.AppendKey("FunctionRequestId", context.AwsRequestId);
                //Logger.AppendKey("xray_trace_id", context.);
            }
            else if (IsEnabled(LogLevel.Debug))
            {
                _systemWrapper.LogLine(
                    $"Skipping Lambda Context injection because ILambdaContext context parameter not found.");
            }

            _isColdStart = false;
            _initializeContext = false;
            _isContextInitialized = true;

            if (!_logEvent) return;
            
            var eventArg = eventArgs.Args.FirstOrDefault(x => x is not ILambdaContext);
            if (eventArg is not null)
            {
                    
            }
            else if (IsEnabled(LogLevel.Debug))
            {
                _systemWrapper.LogLine(
                    $"Skipping Event Log because event parameter not found.");
            }
        }

        public void OnSuccess(AspectEventArgs eventArgs, object result)
        {
            
        }

        public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            throw exception;
        }

        public void OnExit(AspectEventArgs eventArgs)
        {
            if (!_isContextInitialized) return;
            
            Logger.RemoveAllKeys();
            _initializeContext = true;
        }
    }
}