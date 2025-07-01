using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <inheritdoc />
public interface ISqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
{
 
}