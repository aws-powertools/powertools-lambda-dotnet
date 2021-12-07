using System;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;

namespace AWS.Lambda.PowerTools.Metrics.Internal
{
    internal class MetricsAspectHandler : IMethodAspectHandler
    {
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly IMetrics _metrics;
        private readonly bool _captureColdStartEnabled;
        private static bool _isColdStart = true;

        internal MetricsAspectHandler
        (
            IMetrics metricsInstance,
            bool captureColdStartEnabled,
            IPowerToolsConfigurations powerToolsConfigurations
        )
        {
            _metrics = metricsInstance;
            _captureColdStartEnabled = captureColdStartEnabled;
            _powerToolsConfigurations = powerToolsConfigurations;
        }

        public void OnExit(AspectEventArgs eventArgs)
        {            
            _metrics.Flush();
        }

        public void OnEntry(AspectEventArgs eventArgs)
        {
            if (!_isColdStart || !_captureColdStartEnabled) return;
            _metrics.AddMetric("ColdStart", 1, MetricUnit.COUNT);
            _isColdStart = false;
        }

        public void OnSuccess(AspectEventArgs eventArgs, object result)
        {
            // NOT IMPLEMENTED
        }

        public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            throw exception;
        }
    }
}