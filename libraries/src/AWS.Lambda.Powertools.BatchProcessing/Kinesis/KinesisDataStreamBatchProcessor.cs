using System.Collections.Generic;
using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

public class KinesisDataStreamBatchProcessor : BatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>
{
    public static readonly KinesisDataStreamBatchProcessor Instance = new();

    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(KinesisEvent _) => BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure;

    protected override ICollection<KinesisEvent.KinesisEventRecord> GetRecordsFromEvent(KinesisEvent @event) => @event.Records;

    protected override string GetRecordId(KinesisEvent.KinesisEventRecord record) => record.Kinesis.SequenceNumber;
}
