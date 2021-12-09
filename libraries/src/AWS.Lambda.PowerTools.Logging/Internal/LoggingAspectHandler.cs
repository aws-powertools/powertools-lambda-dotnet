using System;
using System.IO;
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
        private readonly bool? _logEvent;
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly ISystemWrapper _systemWrapper;

        private static bool _isColdStart = true;
        private static bool _initializeContext = true;
        private bool _isContextInitialized;
        
        internal LoggingAspectHandler
        (
            string serviceName,
            LogLevel? logLevel,
            double? samplingRate,
            bool? logEvent,
            IPowerToolsConfigurations powerToolsConfigurations,
            ISystemWrapper systemWrapper
        )
        {
            _serviceName = serviceName;
            _logLevel = logLevel;
            _samplingRate = samplingRate;
            _logEvent = logEvent;
            _powerToolsConfigurations = powerToolsConfigurations;
            _systemWrapper = systemWrapper;
        }

        private bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None && logLevel >= _powerToolsConfigurations.GetLogLevel(_logLevel);

        public void OnEntry(AspectEventArgs eventArgs)
        {
            Logger.LoggerProvider ??= new LoggerProvider(
                new LoggerConfiguration
                {
                    ServiceName = _serviceName,
                    MinimumLevel = _logLevel,
                    SamplingRate = _samplingRate
                });

            if (!_initializeContext)
                return;

            Logger.AppendKey("ColdStart", _isColdStart);

            _isColdStart = false;
            _initializeContext = false;
            _isContextInitialized = true;

            var eventObject = eventArgs.Args.FirstOrDefault();
            var context = eventArgs.Args.FirstOrDefault(x => x is ILambdaContext) as ILambdaContext;
            
            InjectLambdaContext(context);
            InjectXrayTraceId(eventObject);
            if (_logEvent ?? _powerToolsConfigurations.LoggerLogEvent)
                LogEvent(eventObject);
        }

        private void InjectLambdaContext(ILambdaContext context)
        {
            if (context is null)
            {
                if(IsEnabled(LogLevel.Debug))
                    _systemWrapper.LogLine(
                        $"Skipping Lambda Context injection because ILambdaContext context parameter not found.");
                return;
            }
            
            Logger.AppendKey("FunctionName", context.FunctionName);
            Logger.AppendKey("FunctionVersion", context.FunctionVersion);
            Logger.AppendKey("FunctionMemorySize", context.MemoryLimitInMB);
            Logger.AppendKey("FunctionArn", context.InvokedFunctionArn);
            Logger.AppendKey("FunctionRequestId", context.AwsRequestId);
        }

        private void InjectXrayTraceId(object eventArg)
        {
            if (eventArg is null)
            {
                if(IsEnabled(LogLevel.Debug))
                    _systemWrapper.LogLine(
                        $"Skipping Xray Trace Id extraction because event parameter not found.");
                return;
            }
            
            //ToDo: Implement XrayTraceId extraction logic
            Logger.AppendKey("XrayTraceId", "");
        }

        private void LogEvent(object eventArg)
        {
            switch (eventArg)
            {
                case null:
                {
                    if (IsEnabled(LogLevel.Debug))
                        _systemWrapper.LogLine(
                            $"Skipping Event Log because event parameter not found.");
                    break;
                }
                case Stream:
                    try
                    {
                        Logger.LogInformation("{event}", eventArg);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Failed to log event from supplied input stream.");
                    }
                    break;
                default:
                    try
                    {
                        Logger.LogInformation("{event}", eventArg);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Failed to log event from supplied input object.");
                    }
                    break;
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

        internal void ResetForTest()
        { 
            _isColdStart = true; 
            _initializeContext = true; 
            _isContextInitialized = false;
            Logger.LoggerProvider = null;
            Logger.RemoveAllKeys();
        }
    }
}