using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

/// <inheritdoc />
public interface IKinesisEventRecordHandler : IRecordHandler<KinesisEvent.KinesisEventRecord>
{
 
}