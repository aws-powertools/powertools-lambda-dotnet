using System;
using System.Collections.Generic;
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
        private readonly string _service;
        private readonly LogLevel? _logLevel;
        private readonly double? _samplingRate;
        private readonly bool? _logEvent;
        private readonly bool _clearState;
        private readonly string _correlationIdPath;
        private static readonly Dictionary<string, object> _lambdaContextKeys = new();
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly ISystemWrapper _systemWrapper;

        private static bool _isColdStart = true;
        private static bool _initializeContext = true;
        private bool _isContextInitialized;

        internal LoggingAspectHandler
        (
            string service,
            LogLevel? logLevel,
            double? samplingRate,
            bool? logEvent,
            string correlationIdPath,
            bool clearState,
            IPowerToolsConfigurations powerToolsConfigurations,
            ISystemWrapper systemWrapper
        )
        {
            _service = service;
            _logLevel = logLevel;
            _samplingRate = samplingRate;
            _logEvent = logEvent;
            _clearState = clearState;
            _correlationIdPath = correlationIdPath;
            _powerToolsConfigurations = powerToolsConfigurations;
            _systemWrapper = systemWrapper;
        }

        public void OnEntry(AspectEventArgs eventArgs)
        {
            Logger.LoggerProvider ??= new LoggerProvider(
                new LoggerConfiguration
                {
                    Service = _service,
                    MinimumLevel = _logLevel,
                    SamplingRate = _samplingRate
                });

            if (!_initializeContext)
                return;
            
            Logger.AppendKey(LoggingConstants.KeyColdStart, _isColdStart);

            _isColdStart = false;
            _initializeContext = false;
            _isContextInitialized = true;

            var eventObject = eventArgs.Args.FirstOrDefault();
            var context = eventArgs.Args.FirstOrDefault(x => x is ILambdaContext) as ILambdaContext;

            CaptureXrayTraceId();
            CaptureLambdaContext(context);
            CaptureCorrelationId(eventObject);
            if (_logEvent ?? _powerToolsConfigurations.LoggerLogEvent)
                LogEvent(eventObject);
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
            _lambdaContextKeys.Clear();
            if (_clearState)
                Logger.RemoveAllKeys();
            _initializeContext = true;
        }

        private bool IsDebug() => 
            LogLevel.Debug >= _powerToolsConfigurations.GetLogLevel(_logLevel);

        private void CaptureXrayTraceId()
        {
            var xRayTraceId = _powerToolsConfigurations.XRayTraceId;
            if (string.IsNullOrWhiteSpace(xRayTraceId))
                return;

            xRayTraceId = xRayTraceId
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .First()
                .Replace("Root=", "");

            Logger.AppendKey(LoggingConstants.KeyXRayTraceId, xRayTraceId);
        }

        private void CaptureLambdaContext(ILambdaContext context)
        {
            _lambdaContextKeys.Clear();
            if (context is null)
            {
                if (IsDebug())
                    _systemWrapper.LogLine(
                        $"Skipping Lambda Context injection because ILambdaContext context parameter not found.");
                return;
            }
            
            _lambdaContextKeys.Add(LoggingConstants.KeyFunctionName, context.FunctionName);
            _lambdaContextKeys.Add(LoggingConstants.KeyFunctionVersion, context.FunctionVersion);
            _lambdaContextKeys.Add(LoggingConstants.KeyFunctionMemorySize, context.MemoryLimitInMB);
            _lambdaContextKeys.Add(LoggingConstants.KeyFunctionArn, context.InvokedFunctionArn);
            _lambdaContextKeys.Add(LoggingConstants.KeyFunctionRequestId, context.AwsRequestId);
        }

        private void CaptureCorrelationId(object eventArg)
        {
            if (string.IsNullOrWhiteSpace(_correlationIdPath))
                return;

            var correlationIdPaths = _correlationIdPath
                .Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (!correlationIdPaths.Any())
                return;

            if (eventArg is null)
            {
                if (IsDebug())
                    _systemWrapper.LogLine(
                        $"Skipping CorrelationId capture because event parameter not found.");
                return;
            }
            
            try
            {
                var rootObject = correlationIdPaths.Aggregate(eventArg, GetCorrelationId);
                if (rootObject is not null)
                    Logger.AppendKey(LoggingConstants.KeyCorrelationId, rootObject);
            }
            catch (Exception e)
            {
                if (IsDebug())
                    _systemWrapper.LogLine(
                        $"Skipping CorrelationId capture because of error caused while parsing the event object {e.Message}.");
            }
        }
        
        private static object GetCorrelationId(object rootObject, string propertyName)
        {
            return rootObject switch
            {
                null => null,
                IDictionary<string, string> headers => headers.ContainsKey(propertyName) ? headers[propertyName] : null,
                IDictionary<string, IList<string>> multiValueHeaders => multiValueHeaders.ContainsKey(propertyName)
                    ? multiValueHeaders[propertyName]?.FirstOrDefault()
                    : null,
                _ => rootObject.GetType().GetProperty(propertyName)?.GetValue(rootObject)
            };
        }

        private void LogEvent(object eventArg)
        {
            switch (eventArg)
            {
                case null:
                {
                    if (IsDebug())
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

        internal void ResetForTest()
        {
            _isColdStart = true;
            _initializeContext = true;
            _isContextInitialized = false;
            _lambdaContextKeys.Clear();
            Logger.LoggerProvider = null;
            Logger.RemoveAllKeys();
        }

        internal static IEnumerable<KeyValuePair<string, object>> GetLambdaContextKeys()
        {
            return _lambdaContextKeys.AsEnumerable();
        }
    }
}