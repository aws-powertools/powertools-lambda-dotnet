namespace Amazon.Lambda.PowerTools.Tracing
{
 public enum TracingCaptureMode
 {
   /// <summary>
   /// Enables attribute to capture only response. If this mode is explicitly overridden
   /// on {<see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override value of environment variable POWERTOOLS_TRACER_CAPTURE_RESPONSE
   /// </summary>
   Response,

   /// <summary>
   /// Enabled attribute to capture only error from the method. If this mode is explicitly overridden
   /// on <see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override value of environment variable POWERTOOLS_TRACER_CAPTURE_ERROR
   /// </summary>
   Error,

   /// <summary>
   /// Enabled attribute to capture both response error from the method. If this mode is explicitly overridden
   /// on <see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override value of environment variables POWERTOOLS_TRACER_CAPTURE_RESPONSE
   /// and POWERTOOLS_TRACER_CAPTURE_ERROR
   /// </summary>
   ResponseAndError,

   /// <summary>
   /// Disables attribute to capture both response and error from the method. If this mode is explicitly overridden
   /// on <see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override values of environment variable POWERTOOLS_TRACER_CAPTURE_RESPONSE
   /// and POWERTOOLS_TRACER_CAPTURE_ERROR
   /// </summary>
   Disabled,

   /// <summary>
   /// Enables/Disables attribute to capture response and error from the method based on the value of
   /// environment variable POWERTOOLS_TRACER_CAPTURE_RESPONSE and POWERTOOLS_TRACER_CAPTURE_ERROR
   /// </summary>
   EnvironmentVariable
 }
}