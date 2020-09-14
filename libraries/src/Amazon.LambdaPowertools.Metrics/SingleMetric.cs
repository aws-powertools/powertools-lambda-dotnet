using System;
using System.Collections.Generic;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{
    public class SingleMetric : MetricsManager
    {
        public SingleMetric(
                        string metricsNamespace = null,
                        string serviceName = null,
                        Metric metric = null) 
                        : base(metricsNamespace: metricsNamespace, 
                               serviceName: serviceName)
                               {
                                 AddMetric(metric);   
                                }

        public override void AddMetric(Metric metric)
        {
            Metrics.Clear();
            Metrics.Add(metric.Name, metric);
        }
    }
}