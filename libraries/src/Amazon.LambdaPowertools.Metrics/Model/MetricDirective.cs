using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace Amazon.LambdaPowertools.Metrics.Model
{
    public class MetricDirective
    {
        
        [JsonPropertyName("Namespace")]
        public string Namespace { get; private set; }

        [MaxLength(100)]
        [JsonPropertyName("Metrics")]
        public List<MetricDefinition> Metrics
        {
            get; private set;
        }

        [JsonIgnore]
        public List<DimensionSet> Dimensions { get; private set; }

        public MetricDirective() : this(null, new List<MetricDefinition>(), new List<DimensionSet>()) { }

        public MetricDirective(string metricsNamespace) : this(metricsNamespace, new List<MetricDefinition>(), new List<DimensionSet>()) { }

        public MetricDirective(string metricsNamespace, List<DimensionSet> defaultDimensions) : this(metricsNamespace, new List<MetricDefinition>(), defaultDimensions) { }
        
        private MetricDirective(string metricsNamespace, List<MetricDefinition> metrics, List<DimensionSet> defaultDimensions)
        {
            Namespace = metricsNamespace;
            Metrics = metrics;
            Dimensions = defaultDimensions;
        }

        [JsonPropertyName("Dimensions")]
        public List<List<string>> AllDimensionKeys
        {
            get
            {
                var keys = Dimensions
                    .Where(d => d.DimensionKeys.Any())
                    .Select(s => s.DimensionKeys)
                    .ToList();

                if(keys.Count == 0)
                {
                    keys.Add(new List<string>());
                }

                return keys;
            }
        }

        public void AddMetric(string name, double value, MetricUnit unit)
        {
            if (Metrics.Count < PowertoolsConfig.MaxMetrics)
            {
                var metric = Metrics.FirstOrDefault(metric => metric.Name == name);
                if (metric != null)
                {
                    metric.AddValue(value);
                }
                else
                {
                    Metrics.Add(new MetricDefinition(name, unit, value));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(Metrics), "Cannot add more than 100 metrics at the same time.");
            }
        }

        internal void SetNamespace(string metricsNamespace)
        {
            Namespace = metricsNamespace;
        }

        internal void AddDimension(DimensionSet dimension)
        {
            if (Dimensions.Count < PowertoolsConfig.MaxDimensions)
            {
                Dimensions.Add(dimension);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(Dimensions), "Cannot add more than 9 dimensions at the same time.");
            }
        }

        internal void SetDimensions(List<DimensionSet> dimensions)
        {
            Dimensions = dimensions;
        }

        
        internal Dictionary<string, string> ExpandAllDimensionSets()
        {
            var dimensions = new Dictionary<string, string>();
            foreach (DimensionSet dimensionSet in Dimensions)
            {
                foreach (var dimension in dimensionSet.Dimensions)
                {
                    dimensions.TryAdd(dimension.Key, dimension.Value);
                }
            }

            return dimensions;
        }
    }
}