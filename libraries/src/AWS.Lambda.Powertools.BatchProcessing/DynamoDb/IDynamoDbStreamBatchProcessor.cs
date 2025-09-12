

using Amazon.Lambda.DynamoDBEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <summary>
/// The default batch processor for DynamoDB Stream events.
/// </summary>
public interface IDynamoDbStreamBatchProcessor : IBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>
{
 
}