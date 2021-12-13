using System;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Metrics.Internal;

namespace AWS.Lambda.PowerTools.Metrics
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MetricsAttribute : MethodAspectAttribute
    {
        public string Namespace { get; set; }
        public string Service { get; set; }
        public bool CaptureColdStart { get; set; }
        public bool RaiseOnEmptyMetrics { get; set; }


        private IMetrics _metricsInstance;
        private IMetrics MetricsInstance =>
            _metricsInstance ??= new Metrics(
                PowerToolsConfigurations.Instance,
                Namespace,
                Service,
                RaiseOnEmptyMetrics
            );
        
        protected override IMethodAspectHandler CreateHandler()
        {
            return new MetricsAspectHandler
            (
                MetricsInstance,
                CaptureColdStart
            );
        }
    }
}



