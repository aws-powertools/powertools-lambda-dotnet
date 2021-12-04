using Amazon.Lambda.PowerTools.Tracing.Internal;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing
{
    public class TracingAttribute : MethodAspectAttribute
    {
        public string SegmentName { get; set; } = "";
        public string Namespace { get; set; } = "";
        public TracingCaptureMode CaptureMode { get; set; } = TracingCaptureMode.EnvironmentVariable;

        protected override IMethodAspectHandler CreateHandler()
        {
            return new TracingAspectHandler
            (
                SegmentName,
                Namespace,
                CaptureMode,
                PowerToolsConfigurations.Instance,
                XRayRecorder.Instance
            );
        }
    }
}