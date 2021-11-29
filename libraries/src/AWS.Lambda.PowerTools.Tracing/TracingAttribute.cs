using System;
using Amazon.Lambda.PowerTools.Tracing.Internal;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing
{
    public class TracingAttribute : MethodAspectAttribute
    {
        public string SegmentName { get; set; } = "";
        public string Namespace { get; set; } = "";
        public TracingCaptureMode TracingCaptureMode { get; set; } = TracingCaptureMode.EnvironmentVariable;
        
        private IMethodAspectAttribute _tracingHandler;
        private IMethodAspectAttribute TracingHandler =>
            _tracingHandler ??= new TracingAspectHandler
            (
                SegmentName,
                Namespace,
                TracingCaptureMode,
                PowerToolsConfigurations.Instance,
                XRayRecorder.Instance
            );

        public override void OnEntry(AspectEventArgs eventArgs)
        {
            TracingHandler.OnEntry(eventArgs);
        }

        public override void OnSuccess(AspectEventArgs eventArgs, object result)
        {
            TracingHandler.OnSuccess(eventArgs, result);
        }

        public override T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            return TracingHandler.OnException<T>(eventArgs, exception);
        }

        public override void OnExit(AspectEventArgs eventArgs)
        {
            TracingHandler.OnExit(eventArgs);
        }
    }
}