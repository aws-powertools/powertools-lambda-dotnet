using System.Collections.Generic;
using System.Text.Json.Serialization;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{

    public class MetricsWrapper
    {
        [JsonPropertyName("_aws")]
        public MetricsDefinition Root { get; set; }
    }

    public class MetricsDefinition
    {
        public double Timestamp { get; set; }
        public List<CloudWatchMetrics> CloudWatchMetrics { get; set; }
    }

    public class CloudWatchMetrics
    {
        public string Namespace { get; set; }
        public List<List<string>> Dimensions { get; set; }
        public List<MetricsDefinitionSet> Metrics { get; set; }
    }

    public class MetricsDefinitionSet
    {
        public string Name { get; set; }
        public string Unit { get; set; }
    }
}