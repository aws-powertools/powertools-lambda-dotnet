using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using AWS.Lambda.PowerTools.Core;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class MetricDirective
    {
        
        [JsonPropertyName("Namespace")]
        public string Namespace { get; private set; }
        
        [JsonIgnore]
        public string ServiceName { get; private set; }
        
        [JsonPropertyName("Metrics")]
        public List<MetricDefinition> Metrics
        {
            get; private set;
        }

        [JsonIgnore]
        public List<DimensionSet> Dimensions { get; private set; }

        [JsonIgnore]
        public List<DimensionSet> DefaultDimensions {get; private set; }

        /// <summary>
        /// Creates empty MetricDirective object 
        /// </summary>
        public MetricDirective() : this(null, new List<MetricDefinition>(), new List<DimensionSet>()) { }

        /// <summary>
        /// Creates MetricDirective object with specific namespace identifier
        /// </summary>
        /// <param name="metricsNamespace">Metrics namespace identifier</param>
        public MetricDirective(string metricsNamespace) : this(metricsNamespace, new List<MetricDefinition>(), new List<DimensionSet>()) { }

        /// <summary>
        /// Creates MetricDirective object with specific namespace identifier and default dimensions list
        /// </summary>
        /// <param name="metricsNamespace">Metrics namespace identifier</param>
        /// <param name="defaultDimensions">Default dimensions list</param>
        public MetricDirective(string metricsNamespace, List<DimensionSet> defaultDimensions) : this(metricsNamespace, new List<MetricDefinition>(), defaultDimensions) { }
        
        /// <summary>
        /// Creates MetricDirective object with specific namespace identifier, list of metrics and default dimensions list
        /// </summary>
        /// <param name="metricsNamespace">Metrics namespace identifier</param>
        /// <param name="metrics">List of metrics</param>
        /// <param name="defaultDimensions">Default dimensions list</param>
        private MetricDirective(string metricsNamespace, List<MetricDefinition> metrics, List<DimensionSet> defaultDimensions)
        {
            Namespace = metricsNamespace;
            Metrics = metrics;
            Dimensions = new List<DimensionSet>();
            DefaultDimensions = defaultDimensions;
        }

        /// <summary>
        /// Creates list with all dimensions. Needed for correct EMF payload creation
        /// </summary>
        [JsonPropertyName("Dimensions")]
        public List<List<string>> AllDimensionKeys
        {
            get
            {
                var defaultKeys = DefaultDimensions
                    .Where(d => d.DimensionKeys.Any())
                    .Select(s => s.DimensionKeys)
                    .ToList();

                var keys = Dimensions
                    .Where(d => d.DimensionKeys.Any())
                    .Select(s => s.DimensionKeys)
                    .ToList();

                defaultKeys.AddRange(keys);

                if(defaultKeys.Count == 0)
                {
                    defaultKeys.Add(new List<string>());
                }

                return defaultKeys;
            }
        }
        
        /// <summary>
        /// Adds metric to memory
        /// </summary>
        /// <param name="name">Metric name. Cannot be null, empty or whitespace</param>
        /// <param name="value">Metric value</param>
        /// <param name="unit">Metric unit</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws 'ArgumentOutOfRangeException' if more than 100 items are added to list. Should only happen if gate keeping fails on Flush method</exception>
        public void AddMetric(string name, double value, MetricUnit unit)
        {
            if (Metrics.Count < PowerToolsConfigurations.MaxMetrics)
            {
                var metric = Metrics.FirstOrDefault(m => m.Name == name);
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

        /// <summary>
        /// Sets metrics namespace identifier
        /// </summary>
        /// <param name="metricsNamespace">Metrics namespace identifier</param>
        internal void SetNamespace(string metricsNamespace)
        {
            Namespace = metricsNamespace;
        }
        
        /// <summary>
        /// Sets service name
        /// </summary>
        /// <param name="serviceName">Service name</param>
        internal void SetServiceName(string serviceName)
        {
            ServiceName = serviceName;
        }

        /// <summary>
        /// Adds new dimension to memory
        /// </summary>
        /// <param name="dimension">Metrics Dimension</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws 'ArgumentOutOfRangeException' if more than 9 dimension are added to the list</exception>
        internal void AddDimension(DimensionSet dimension)
        {
            if (Dimensions.Count < PowerToolsConfigurations.MaxDimensions)
            {
                var matchingKeys = AllDimensionKeys.Where(x => x.Contains(dimension.DimensionKeys[0]));
                if(!matchingKeys.Any())
                {
                    Dimensions.Add(dimension);
                }
                else
                {
                    Console.WriteLine($"##WARNING##: Failed to Add dimension '{dimension.DimensionKeys[0]}'. Dimension already exists.");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(Dimensions), "Cannot add more than 9 dimensions at the same time.");
            }
        }

        /// <summary>
        /// Sets default dimensions
        /// </summary>
        /// <param name="defaultDimensions">Default dimensions list</param>
        internal void SetDefaultDimensions(List<DimensionSet> defaultDimensions)
        {
            if(!DefaultDimensions.Any()){
                DefaultDimensions = defaultDimensions;
            }
            else {
                foreach (var item in defaultDimensions)
                {                    
                    if(!DefaultDimensions.Any(d => d.DimensionKeys.Contains(item.DimensionKeys[0]))){
                        DefaultDimensions.Add(item);
                    }
                }
            }
            
        }
        
        /// <summary>
        /// Appends dimension and default dimension lists
        /// </summary>
        /// <returns>Dictionary with dimension and default dimension list appended</returns>
        internal Dictionary<string, string> ExpandAllDimensionSets()
        {
            var dimensions = new Dictionary<string, string>();

            foreach (var dimensionSet in DefaultDimensions)
            {
                foreach (var (key, value) in dimensionSet.Dimensions)
                {
                    dimensions.TryAdd(key, value);
                }
            }

            foreach (var dimensionSet in Dimensions)
            {
                foreach (var (key, value) in dimensionSet.Dimensions)
                {
                    dimensions.TryAdd(key, value);
                }
            }

            return dimensions;
        }
    }
}