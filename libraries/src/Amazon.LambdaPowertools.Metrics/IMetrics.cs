using System;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{
    public interface IMetrics : IDisposable
    {
        public Metrics AddMetric(string key, double value, MetricUnit unit);
        public Metrics SetNamespace(string metricNamespace);
        public Metrics AddDimension(string key, string value);
        public Metrics AddMetadata(string key, dynamic value);
        public void PushSingleMetric(string metricName, double value, MetricUnit unit, string metricsNamespace = null, string serviceName = null);

        public string GetNamespace();

        public string Serialize();
        public void Flush();
    }
}
    