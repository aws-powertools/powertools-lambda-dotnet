using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// The default batch processor for SQS events.
/// </summary>
public interface ISqsBatchProcessor : IBatchProcessor<SQSEvent, SQSEvent.SQSMessage>
{
 
}