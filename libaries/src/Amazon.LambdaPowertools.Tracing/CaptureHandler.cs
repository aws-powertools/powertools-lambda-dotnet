using System;

namespace Amazon.LambdaPowertools.Tracing
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CaptureHandler : Attribute
    {
        public CaptureHandler(ITracerOptions options)
        {
            // not implemented
        }
        
        public CaptureHandler(string service = "undefined", bool disabled = false, bool autoPatch = true,
            string patchmodules = null)
        {
            // not implemented
        }
    }
}