using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.PowerTools.Metrics
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

        /// <summary>
        /// Serializes metrics object to a valid string in JSON format
        /// </summary>
        /// <returns>JSON EMF payload in string format</returns>
        /// <exception cref="SchemaValidationException">Throws 'SchemaValidationException' when namespace is not defined</exception>
        public string Serialize()
        {
            if (string.IsNullOrWhiteSpace(AWS.GetNamespace()))
            {
                throw new SchemaValidationException("namespace");
            }

            return JsonSerializer.Serialize(this);
        }

    }
}