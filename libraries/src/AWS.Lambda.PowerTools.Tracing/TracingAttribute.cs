using Amazon.Lambda.PowerTools.Tracing.Internal;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing
{
    public class TracingAttribute : MethodAspectAttribute
    {
        /// <summary>
        /// Set custom segment name for the operation.
        /// The default is '## {MethodName}'.
        /// </summary>
        public string SegmentName { get; set; } = "";
        
        /// <summary>
        /// Set namespace to current subsegment.
        /// The default is the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
        /// </summary>
        public string Namespace { get; set; } = "";
        
        /// <summary>
        /// Set capture mode to record method responses and exceptions.
        /// The defaults are the environment variables <c>POWERTOOLS_TRACER_CAPTURE_RESPONSE</c> and <c>POWERTOOLS_TRACER_CAPTURE_ERROR</c>.
        /// </summary>
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