using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AWS.Lambda.PowerTools.Metrics.Serializer;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class Metadata
    {
        [JsonConverter(typeof(UnixMillisecondDateTimeConverter))]
        public DateTime Timestamp { get; }

        [JsonPropertyName("CloudWatchMetrics")]
        public List<MetricDirective> CloudWatchMetrics { get; private set; }

        private MetricDirective _metricDirective { get { return CloudWatchMetrics[0]; } }

        [JsonIgnore]
        public Dictionary<string, dynamic> CustomMetadata { get; }

        /// <summary>
        /// Create Metadata object
        /// </summary>
        public Metadata()
        {
            CloudWatchMetrics = new List<MetricDirective>() { new MetricDirective() };
            Timestamp = DateTime.Now;
            CustomMetadata = new Dictionary<string, dynamic>();
        }

        /// <summary>
        /// Deletes all metrics from memory
        /// </summary>
        internal void ClearMetrics()
        {
            _metricDirective.Metrics.Clear();
        }

        /// <summary>
        /// Deletes non-default dimensions from memory
        /// </summary>
        internal void ClearNonDefaultDimensions(){
            _metricDirective.Dimensions.Clear();
        }

        /// <summary>
        /// Adds metric to memory
        /// </summary>
        /// <param name="key">Metric key. Cannot be null, empty or whitespace</param>
        /// <param name="value">Metric value</param>
        /// <param name="unit">Metric Unit</param>
        internal void AddMetric(string key, double value, MetricUnit unit)
        {
            _metricDirective.AddMetric(key, value, unit);
        }   

        /// <summary>
        /// Sets global metrics namespace
        /// </summary>
        /// <param name="metricNamespace">Global metrics namespace</param>
        internal void SetNamespace(string metricNamespace)
        {         
            _metricDirective.SetNamespace(metricNamespace);
        }
        
        /// <summary>
        /// Retrieves global namespace identifier
        /// </summary>
        /// <returns>Global namespace identifier</returns>
        internal string GetNamespace()
        {
            return _metricDirective.Namespace;
        }
        
        /// <summary>
        /// Sets service name
        /// </summary>
        /// <param name="serviceName">Service name</param>
        internal void SetServiceName(string serviceName)
        {         
            _metricDirective.SetServiceName(serviceName);
        }
        
        /// <summary>
        /// Retrieves service name
        /// </summary>
        /// <returns>Service name</returns>
        internal string GetServiceName()
        {
            return _metricDirective.ServiceName;
        }

        /// <summary>
        /// Adds new Dimension
        /// </summary>
        /// <param name="dimension">Dimension to add</param>
        internal void AddDimensionSet(DimensionSet dimension)
        {
            _metricDirective.AddDimension(dimension);
        }

        /// <summary>
        /// Sets default dimensions list
        /// </summary>
        /// <param name="defaultDimensionSets">Default dimensions list</param>
        internal void SetDefaultDimensions(List<DimensionSet> defaultDimensionSets){
            _metricDirective.SetDefaultDimensions(defaultDimensionSets);
        }

        /// <summary>
        /// Retrieves metrics stored in memory
        /// </summary>
        /// <returns>List of metrics stored in memory</returns>
        internal List<MetricDefinition> GetMetrics()
        {
            return _metricDirective.Metrics;
        }

        /// <summary>
        /// Adds metadata to memory
        /// </summary>
        /// <param name="key">Metadata key. Cannot be null, empty or whitespace</param>
        /// <param name="value">Metadata value</param>
        internal void AddMetadata(string key, dynamic value)
        {
            CustomMetadata.Add(key, value);
        }

        /// <summary>
        /// Creates Dictionary with all Dimensions. Needed for correct EMF payload serialization
        /// </summary>
        /// <returns>Dictionary with all dimensions</returns>
        internal Dictionary<string, string> ExpandAllDimensionSets()
        {
            Dictionary<string, string> dimensionSets;
            if (CloudWatchMetrics.Count > 0)
                dimensionSets = _metricDirective.ExpandAllDimensionSets();
            else
                dimensionSets = new Dictionary<string, string>();

            return dimensionSets;
        }
    }
}