using System;
using System.Collections.Generic;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{
    public class Metrics : MetricsManager
    {
        public Metrics(
                        string metricsNamespace = null,
                        string serviceName = null,
                        Dictionary<string, string> dimensions = null,
                        Dictionary<string, Metric> metrics = null,
                        Dictionary<string, dynamic> metadata = null)
                        : base(metricsNamespace,
                                serviceName,
                                dimensions,
                                metrics,
                                metadata)
        {

        }
    }
}