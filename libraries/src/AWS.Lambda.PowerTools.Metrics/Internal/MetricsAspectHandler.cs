using System;
using AWS.Lambda.PowerTools.Aspects;

namespace AWS.Lambda.PowerTools.Metrics.Internal
{
    internal class MetricsAspectHandler : IMethodAspectHandler
    {
        private readonly IMetrics _metrics;
        private readonly bool _captureColdStartEnabled;
        private static bool _isColdStart = true;

        internal MetricsAspectHandler
        (
            IMetrics metricsInstance,
            bool captureColdStartEnabled
        )
        {
            _metrics = metricsInstance;
            _captureColdStartEnabled = captureColdStartEnabled;
        }

        public void OnExit(AspectEventArgs eventArgs)
        {            
            _metrics.Flush();
        }

        public void OnEntry(AspectEventArgs eventArgs)
        {
            if (!_isColdStart || !_captureColdStartEnabled) return;
            _metrics.AddMetric("ColdStart", 1.0, MetricUnit.COUNT);
            _isColdStart = false;
        }

        public void OnSuccess(AspectEventArgs eventArgs, object result)
        {
            
        }

        public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            throw exception;
        }
    }
}