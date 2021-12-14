using System;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Metrics.Internal;

namespace AWS.Lambda.PowerTools.Metrics
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MetricsAttribute : MethodAspectAttribute
    {
        /// <summary>
        /// Set namespace to current subsegment.
        /// The default is the environment variable <c>POWERTOOLS_METRICS_NAMESPACE</c>.
        /// </summary>
        public string Namespace { get; set; }
        
        /// <summary>
        /// Service name is used for metric dimension across all metrics.
        /// This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
        /// </summary>
        public string Service { get; set; }
        
        /// <summary>
        /// Captures cold start during Lambda execution
        /// </summary>
        public bool CaptureColdStart { get; set; }
        
        /// <summary>
        /// Instructs metrics validation to throw exception if no metrics are provided.
        /// </summary>
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



