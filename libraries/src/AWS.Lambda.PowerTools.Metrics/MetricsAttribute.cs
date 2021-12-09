using System;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Metrics.Internal;

namespace AWS.Lambda.PowerTools.Metrics
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MetricsAttribute : MethodAspectAttribute
    {
        public string MetricsNamespace { get; set; }
        public string ServiceName { get; set; }
        public bool CaptureColdStart { get; set; }
        public bool CaptureEmptyMetrics { get; set; }


        private IMetrics _metricsInstance;
        private IMetrics MetricsInstance =>
            _metricsInstance ??= new Metrics(
                PowerToolsConfigurations.Instance,
                metricsNamespace: MetricsNamespace,
                serviceName: ServiceName,
                captureMetricsEvenIfEmpty: CaptureEmptyMetrics
            );

        protected override IMethodAspectHandler CreateHandler()
        {
            return new MetricsAspectHandler
            (
                MetricsInstance,
                CaptureColdStart
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



