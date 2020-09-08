using System;

namespace Amazon.LambdaPowertools.Tracing
{
    public interface ITracerOptions
    {
        /// <summary>
        /// Service: default "service_undefined", env:POWERTOOLS_SERVICE_NAME
        /// </summary>
        public string Service { get; set; }
        
        /// <summary>
        /// Explicitly disable tracing via env var POWERTOOLS_TRACE_DISABLED="true"
        /// </summary>
        public bool Disabled { get; set; }
        
        /// <summary>
        ///  If true, it'll use X-Ray to patch all supported libraries at initialization
        /// </summary>
        public bool AutoPatch { get; set; }
        
        /// <summary>
        /// Tuple of specific supported modules by X-Ray that should be patched
        /// </summary>
        public Tuple<string> PatchModules { get; set; }
    }
}