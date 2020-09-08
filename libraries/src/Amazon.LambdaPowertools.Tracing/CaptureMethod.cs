using System;
using System.Collections.Generic;

namespace Amazon.LambdaPowertools.Tracing
{
    /// <summary>
    /// Decorator to trace a function execution
    /// Creates subsegment named after the method
    /// Optionally supports both sync and async method
    /// Adds function response as trace metadata using service attribute as metadata namespace
    /// </summary>
    
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CaptureMethod : Attribute
    {
        public CaptureMethod(ITracerOptions options)
        {
            // not implemented
        }
        
        public CaptureMethod(string service = "undefined", bool disabled = false, bool autoPatch = true,
            string patchmodules = null)
        {
            // not implemented
        }
    }
}