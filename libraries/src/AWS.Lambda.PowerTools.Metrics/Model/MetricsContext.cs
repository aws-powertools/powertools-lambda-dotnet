using System;
using System.Collections.Generic;
using System.Linq;

namespace AWS.Lambda.PowerTools.Metrics.Model
{
    public class MetricsContext : IDisposable
    {
        private RootNode _rootNode;
        internal bool IsSerializable {
            get {
                return !(GetMetrics().Count == 0 && _rootNode.AWS.CustomMetadata.Count == 0);
            }
        }

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

        public void AddMetric(string key, double value, MetricUnit unit)
        {
            _rootNode.AWS.AddMetric(key, value, unit);
        }

        public void SetNamespace(string metricNamespace)
        {
            _rootNode.AWS.SetNamespace(metricNamespace);
        }

        internal string GetNamespace()
        {
            return _rootNode.AWS.GetNamespace();
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
            try
            {
                return _rootNode.Serialize();
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            
        }

        public void Dispose()
        {
            _rootNode = null;
        }
    }
}