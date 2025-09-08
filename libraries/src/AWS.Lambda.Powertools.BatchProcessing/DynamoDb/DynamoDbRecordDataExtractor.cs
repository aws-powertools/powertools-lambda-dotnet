

using System.Text.Json;
using Amazon.Lambda.DynamoDBEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <summary>
/// Extracts data from DynamoDB stream records for deserialization.
/// </summary>
public class DynamoDbRecordDataExtractor : IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord>
{
    /// <summary>
    /// The singleton instance of the DynamoDB record data extractor.
    /// </summary>
    public static readonly DynamoDbRecordDataExtractor Instance = new();

    /// <summary>
    /// Extracts the data from a DynamoDB stream record by serializing the entire DynamoDB record.
    /// For INSERT and MODIFY events, this includes the NewImage. For REMOVE events, this includes the OldImage.
    /// </summary>
    /// <param name="record">The DynamoDB stream record.</param>
    /// <returns>The serialized DynamoDB record data.</returns>
    public string ExtractData(DynamoDBEvent.DynamodbStreamRecord record)
    {
        if (record?.Dynamodb == null)
            return string.Empty;

        // Create a simplified representation of the DynamoDB record for deserialization
        var recordData = new
        {
            record.EventName,
            record.Dynamodb.Keys,
            record.Dynamodb.NewImage,
            record.Dynamodb.OldImage,
            record.Dynamodb.SequenceNumber,
            record.Dynamodb.SizeBytes,
            record.Dynamodb.StreamViewType
        };

        return JsonSerializer.Serialize(recordData);
    }
}