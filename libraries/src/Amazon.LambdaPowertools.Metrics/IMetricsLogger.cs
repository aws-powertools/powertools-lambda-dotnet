using System;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{
    public interface IMetricsLogger : IDisposable
    {
        public MetricsLogger AddMetric(string key, double value, Unit unit);
        public MetricsLogger SetNamespace(string metricNamespace);
        public MetricsLogger AddDimension(string key, string value);
        public MetricsLogger AddMetadata(string key, dynamic value);
        public void PushSingleMetric(string metricName, double value, Unit unit, string serviceNamespace = null, string serviceName = null);

        public string Serialize();
        public void Flush();
    }
}
    