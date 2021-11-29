using System;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    internal class TracingAspectHandler : IMethodAspectAttribute
    {
        private readonly string _segmentName;
        private readonly string _namespace;
        private readonly TracingCaptureMode _tracingCaptureMode;
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly IXRayRecorder _xRayRecorder;
        
        private static bool _isColdStart = true;
        private static bool _captureColdStart = true;
        private bool _isColdStartCaptured;

        internal TracingAspectHandler
        (
            string segmentName,
            string @namespace,
            TracingCaptureMode tracingCaptureMode,
            IPowerToolsConfigurations powerToolsConfigurations,
            IXRayRecorder xRayRecorder
        )
        {
            _segmentName = segmentName;
            _namespace = @namespace;
            _tracingCaptureMode = tracingCaptureMode;
            _powerToolsConfigurations = powerToolsConfigurations;
            _xRayRecorder = xRayRecorder;
        }
        
        private string GetNamespace()
        {
            return !string.IsNullOrWhiteSpace(_namespace) ? _namespace : _powerToolsConfigurations.ServiceName;
        }

        private bool CaptureResponse()
        {
            switch (_tracingCaptureMode)
            {
                case TracingCaptureMode.EnvironmentVariable:
                    return _powerToolsConfigurations.TracerCaptureResponse;
                case TracingCaptureMode.Response:
                case TracingCaptureMode.ResponseAndError:
                    return true;
                case TracingCaptureMode.Error:
                case TracingCaptureMode.Disabled:
                default:
                    return false;
            }
        }

        private bool CaptureError()
        {
            switch (_tracingCaptureMode)
            {
                case TracingCaptureMode.EnvironmentVariable:
                    return _powerToolsConfigurations.TracerCaptureError;
                case TracingCaptureMode.Error:
                case TracingCaptureMode.ResponseAndError:
                    return true;
                case TracingCaptureMode.Response:
                case TracingCaptureMode.Disabled:
                default:
                    return false;
            }
        }

        public void OnEntry(AspectEventArgs eventArgs)
        {
            Console.WriteLine($"OnEntry method {eventArgs.Name}");

            var segmentName = !string.IsNullOrWhiteSpace(_segmentName) ? _segmentName : $"## {eventArgs.Name}";
            var nameSpace = GetNamespace();
            
            Console.WriteLine($"BeginSubsegment method {eventArgs.Name}, SegmentName: {segmentName}, namespace: {nameSpace}");
            
            _xRayRecorder.BeginSubsegment(segmentName);
            _xRayRecorder.SetNamespace(nameSpace);

            if (_captureColdStart)
            {
                Console.WriteLine($"Capturing ColdStart for method: {eventArgs.Name}, ColdStart: {_isColdStart}");
                _xRayRecorder.AddAnnotation("ColdStart", _isColdStart);
                _isColdStart = false;
                _captureColdStart = false;
                _isColdStartCaptured = true;
            }
        }

        public void OnSuccess(AspectEventArgs eventArgs, object result)
        {
            Console.WriteLine($"OnSuccess method {eventArgs.Name}");

            if (CaptureResponse())
            {
                var nameSpace = GetNamespace();
                Console.WriteLine($"Capturing Response for method: {eventArgs.Name}, namespace: {nameSpace}");
                _xRayRecorder.AddMetadata
                (
                    nameSpace: nameSpace,
                    key: $"{eventArgs.Name} response",
                    value: result
                );
            }
        }

        public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            Console.WriteLine($"OnException method {eventArgs.Name} --> {exception}");

            if (CaptureError())
            {
                var nameSpace = GetNamespace();
                Console.WriteLine($"Capturing Error for method: {eventArgs.Name}, namespace: {nameSpace}");
                
                _xRayRecorder.AddMetadata
                (
                    nameSpace: nameSpace,
                    key: $"{eventArgs.Name} error",
                    value: exception
                );
            }

            throw exception;
        }

        public void OnExit(AspectEventArgs eventArgs)
        {
            Console.WriteLine($"OnExit method {eventArgs.Name}");

            if (_isColdStartCaptured)
                _captureColdStart = true;

            if (!_powerToolsConfigurations.IsSamLocal)
            {
                Console.WriteLine($"EndSubsegment method {eventArgs.Name}");
                _xRayRecorder.EndSubsegment();
            }
        }
    }
}