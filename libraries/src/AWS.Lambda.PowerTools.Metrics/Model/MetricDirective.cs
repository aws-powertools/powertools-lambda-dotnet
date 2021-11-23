using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class MetricDirective
    {
        
        [JsonProperty("Namespace")]
        public string Namespace { get; private set; }
        
        [JsonProperty("Metrics")]
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

        [JsonProperty("Dimensions")]
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

                var matchingKeys = AllDimensionKeys.Where(x => x.Contains(dimension.DimensionKeys[0]));
                if(!matchingKeys.Any())
                {
                    Dimensions.Add(dimension);
                }
                else
                {
                    Console.WriteLine($"WARN: Failed to Add dimension '{dimension.DimensionKeys[0]}'. Dimension already exists.");
                }
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