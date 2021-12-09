using System;
using AWS.Lambda.PowerTools.Aspects;

namespace AWS.Lambda.PowerTools.Metrics.Internal
{
    internal class MetricsAspectHandler : IMethodAspectHandler
    {
        private readonly IMetrics _metrics;
        private readonly bool _captureColdStartEnabled;
        private static bool _isColdStart = true;

        /// <summary>
        /// Creates MetricsAspectHandler to supports running code before and after marked methods.
        /// </summary>
        /// <param name="metricsInstance">AWS.Lambda.PowerTools.Metrics Instance</param>
        /// <param name="captureColdStartEnabled">If 'true', captures cold start during Lambda execution</param>
        internal MetricsAspectHandler
        (
            IMetrics metricsInstance,
            bool captureColdStartEnabled
        )
        {
            _metrics = metricsInstance;
            _captureColdStartEnabled = captureColdStartEnabled;
        }

        /// <summary>
        /// OnExit runs after the execution of the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        public void OnExit(AspectEventArgs eventArgs)
        {            
            _metrics.Flush();
        }

        /// <summary>
        /// OnEntry runs before the execution of the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        public void OnEntry(AspectEventArgs eventArgs)
        {
            if (!_isColdStart || !_captureColdStartEnabled) return;
            _metrics.AddMetric("ColdStart", 1.0, MetricUnit.COUNT);
            _isColdStart = false;
        }

        /// <summary>
        /// OnSuccess run after successful execution of the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        /// <param name="result">Object returned by the method marked with Metrics Attribute</param>
        public void OnSuccess(AspectEventArgs eventArgs, object result) { }

        /// <summary>
        /// OnException runs when an unhandled exception occurs inside the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        /// <param name="exception">Exception thrown by the method marked with Metrics Attribute</param>
        /// <typeparam name="T">Type of Exception expected</typeparam>
        /// <returns>Type of the Exception expected</returns>
        /// <exception cref="Exception">Generic unhandled exception</exception>
        public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            throw exception;
        }
    }
}