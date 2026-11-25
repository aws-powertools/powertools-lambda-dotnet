using System.Collections.Generic;
using System.Text.Json.Serialization;
using Amazon.Lambda.DynamoDBEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

/// <summary>
/// Source-generated serialization metadata for the types BatchProcessing serializes itself,
/// so they serialize the same way with and without Native AOT.
/// </summary>
[JsonSerializable(typeof(DynamoDbRecordData))]
internal partial class BatchProcessingSerializationContext : JsonSerializerContext;

/// <summary>
/// The part of a DynamoDB stream record that typed record handlers deserialize.
/// The property names and their order define the JSON shape, so keep them unchanged.
/// </summary>
internal sealed class DynamoDbRecordData
{
    public string EventName { get; init; }
    public Dictionary<string, DynamoDBEvent.AttributeValue> Keys { get; init; }
    public Dictionary<string, DynamoDBEvent.AttributeValue> NewImage { get; init; }
    public Dictionary<string, DynamoDBEvent.AttributeValue> OldImage { get; init; }
    public string SequenceNumber { get; init; }
    public long SizeBytes { get; init; }
    public string StreamViewType { get; init; }
}
