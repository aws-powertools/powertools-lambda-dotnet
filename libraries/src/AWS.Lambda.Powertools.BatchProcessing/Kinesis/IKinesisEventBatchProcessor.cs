using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

/// <summary>
/// The default batch processor for Kinesis Data Stream events.
/// </summary>
public interface IKinesisEventBatchProcessor : IBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>
{

}