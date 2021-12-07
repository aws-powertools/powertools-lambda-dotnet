using System;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Metrics.Internal;
using AWS.Lambda.PowerTools.Core;

namespace AWS.Lambda.PowerTools.Metrics
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MetricsAttribute : MethodAspectAttribute
    {
        private string MetricsNamespace { get; set; }
        private string ServiceName { get; set; }
        private bool CaptureColdStart { get; set; }
        private bool CaptureEmptyMetrics { get; set; }


        private IMetrics _metricsInstance;
        private IMetrics MetricsInstance =>
            _metricsInstance ??= new Metrics(
                metricsNamespace: MetricsNamespace,
                serviceName: ServiceName,
                captureMetricsEvenIfEmpty: CaptureEmptyMetrics
            );

        protected override IMethodAspectHandler CreateHandler()
        {
            return new MetricsAspectHandler
            (
                MetricsInstance,
                CaptureColdStart,
                PowerToolsConfigurations.Instance
            );
        }

        public MetricsAttribute(string metricsNamespace = null,
                string serviceName = null,
                bool captureColdStart = false,
                bool captureMetricsEvenIfEmpty = false)
        {
            MetricsNamespace = metricsNamespace;
            ServiceName = serviceName;
            CaptureColdStart = captureColdStart;
            CaptureEmptyMetrics = captureMetricsEvenIfEmpty;
        }
    }
}



