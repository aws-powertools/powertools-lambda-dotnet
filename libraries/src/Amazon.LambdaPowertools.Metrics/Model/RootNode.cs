using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amazon.LambdaPowertools.Metrics.Model
{
    public class RootNode
    {
        [JsonPropertyName("_aws")]
        public Metadata AWS { get; } = new Metadata();

        [JsonExtensionData]
        public Dictionary<string, dynamic> MetricData
        {
            get
            {
                var targetMembers = new Dictionary<string, dynamic>();

                foreach(var dimension in AWS.ExpandAllDimensionSets())
                {
                    targetMembers.Add(dimension.Key, dimension.Value);
                }

                foreach (var metadata in AWS.CustomMetadata)
                {
                    targetMembers.Add(metadata.Key, metadata.Value);
                }

                foreach (var metricDefinition in AWS.GetMetrics())
                {
                    var values = metricDefinition.Values;
                    targetMembers.Add(metricDefinition.Name, values.Count == 1 ? (dynamic)values[0] : values);
                }

                return targetMembers;
            }
        }

        

        public string Serialize()
        {
            if (string.IsNullOrEmpty(AWS.GetNamespace()))
            {
                throw new ArgumentNullException("namespace", "Namespace property is mandatory");
            }

            return JsonSerializer.Serialize(this, typeof(RootNode));
        }


       


    }
}