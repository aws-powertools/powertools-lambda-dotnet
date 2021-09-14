using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using AWS.Lambda.PowerTools.Metrics.Serializer;
using Newtonsoft.Json;

namespace AWS.Lambda.PowerTools.Metrics.Model
{
    public class Metadata
    {
        [JsonConverter(typeof(UnixMillisecondDateTimeConverter))]
        public DateTime Timestamp { get; }

        [JsonProperty("CloudWatchMetrics")]
        public List<MetricDirective> CloudWatchMetrics { get; private set; }

        private MetricDirective _metricDirective { get { return CloudWatchMetrics[0]; } }

        [JsonIgnore]
        public Dictionary<string, dynamic> CustomMetadata { get; } = new Dictionary<string, dynamic>();

        public Metadata()
        {
            CloudWatchMetrics = new List<MetricDirective>() { new MetricDirective() };
            Timestamp = DateTime.Now;
            CustomMetadata = new Dictionary<string, dynamic>();
        }

        internal void ClearMetrics()
        {
            _metricDirective.Metrics.Clear();
        }

        internal void AddMetric(string key, double value, MetricUnit unit)
        {
            _metricDirective.AddMetric(key, value, unit);
        }   

        internal void SetNamespace(string metricNamespace)
        {         
            _metricDirective.SetNamespace(metricNamespace);
        }

        internal void AddDimensionSet(DimensionSet dimension)
        {
            _metricDirective.AddDimension(dimension);
        }

        internal void SetDimensions(List<DimensionSet> dimensionSets)
        {
            _metricDirective.SetDimensions(dimensionSets);
        }

        internal List<MetricDefinition> GetMetrics()
        {
            return _metricDirective.Metrics;
        }

        internal string GetNamespace()
        {
            return _metricDirective.Namespace;
        }

        internal void AddMetadata(string key, dynamic value)
        {
            CustomMetadata.Add(key, value);
        }

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