using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{
    public interface IMetricsLogger
    {
        public MetricsLogger AddMetric(string key, double value, Unit unit);
        public MetricsLogger SetNamespace(string metricNamespace);
        public MetricsLogger AddDimension(DimensionSet dimension);
        public MetricsLogger SetDimensions(DimensionSet[] dimensions);
        public MetricsLogger AddMetadata(string key, dynamic value);

        public void Flush();
    }
}
    