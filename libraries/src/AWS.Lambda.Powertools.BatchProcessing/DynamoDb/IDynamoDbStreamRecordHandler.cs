using Amazon.Lambda.DynamoDBEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <inheritdoc />
public interface IDynamoDbStreamRecordHandler : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>
{
 
}