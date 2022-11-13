using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.BatchProcessing;

[DataContract]
public class BatchResponse
{
    public BatchResponse() : this(new List<BatchItemFailure>())
    {
    }

    public BatchResponse(List<BatchItemFailure> batchItemFailures)
    {
        BatchItemFailures = batchItemFailures;
    }

    [DataMember(Name = "batchItemFailures")]
    [JsonPropertyName("batchItemFailures")]
    public List<BatchItemFailure> BatchItemFailures { get; set; }

    [DataContract]
    public class BatchItemFailure
    {
        [DataMember(Name = "itemIdentifier")]
        [JsonPropertyName("itemIdentifier")]
        public string ItemIdentifier { get; set; }
    }
}