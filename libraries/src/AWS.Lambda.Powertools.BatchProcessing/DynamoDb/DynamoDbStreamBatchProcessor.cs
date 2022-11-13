using System.Collections.Generic;
using Amazon.Lambda.DynamoDBEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

public class DynamoDbStreamBatchProcessor : BatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>
{
    public static readonly DynamoDbStreamBatchProcessor Instance = new();

    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(DynamoDBEvent _) => BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure;

    protected override ICollection<DynamoDBEvent.DynamodbStreamRecord> GetRecordsFromEvent(DynamoDBEvent @event) => @event.Records;

    protected override string GetRecordId(DynamoDBEvent.DynamodbStreamRecord record) => record.Dynamodb.SequenceNumber;
}
