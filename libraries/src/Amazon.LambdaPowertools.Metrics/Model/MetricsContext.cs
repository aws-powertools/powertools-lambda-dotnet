using System;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.LambdaPowertools.Metrics.Model
{
    public class MetricsContext : IDisposable
    {
        private RootNode _rootNode;

        public MetricsContext()
        {
            _rootNode = new RootNode();
        }

        public MetricsContext(List<DimensionSet> dimensions) : this()
        {
            foreach (DimensionSet dimension in dimensions)
            {
                AddDimension(dimension);
            }
        }

        public List<MetricDefinition> GetMetrics()
        {
            return _rootNode.AWS.GetMetrics();
        }

        public void ClearMetrics()
        {
            _rootNode.AWS.ClearMetrics();
        }

        public void AddMetric(string key, double value, Unit unit)
        {
            _rootNode.AWS.AddMetric(key, value, unit);
        }

        public void SetNamespace(string metricNamespace)
        {
            _rootNode.AWS.SetNamespace(metricNamespace);
        }

        public void AddDimension(DimensionSet dimension)
        {

            _rootNode.AWS.AddDimensionSet(dimension);
        }

        public void AddDimension(string key, string value)
        {
            _rootNode.AWS.AddDimensionSet(new DimensionSet(key, value));
        }

        public void SetDimensions(params DimensionSet[] dimensions)
        {
            _rootNode.AWS.SetDimensions(dimensions.ToList());
        }

        public void AddMetadata(string key, dynamic value)
        {
            _rootNode.AWS.AddMetadata(key, value);
        }

        public string Serialize()
        {
            return _rootNode.Serialize();
        }

        public void Dispose()
        {
            _rootNode = null;
        }
    }
}