using System.Collections.Generic;
using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

/// <summary>
/// The default batch processor for Kinesis Data Stream events.
/// </summary>
public class KinesisEventBatchProcessor : BatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>, IKinesisEventBatchProcessor
{
    /// <summary>
    /// The singleton instance of the batch processor.
    /// </summary>
    private static IKinesisEventBatchProcessor _instance;

    /// <summary>
    /// The singleton instance of the batch processor.
    /// </summary>
    public static IKinesisEventBatchProcessor Instance =>
        _instance ??= new KinesisEventBatchProcessor();

    /// <summary>
    /// Need default constructor for when consumers create a custom batch processor
    /// </summary>
    protected KinesisEventBatchProcessor()
    {
        _instance = this;
    }
    
    /// <summary>
    /// Return the instance ProcessingResult
    /// </summary>
    public static ProcessingResult<KinesisEvent.KinesisEventRecord> Result => _instance.ProcessingResult;

    /// <inheritdoc />
    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(KinesisEvent _) =>
        BatchProcessorErrorHandlingPolicy.ContinueOnBatchItemFailure;

    /// <inheritdoc />
    protected override ICollection<KinesisEvent.KinesisEventRecord> GetRecordsFromEvent(KinesisEvent @event) =>
        @event.Records;

    /// <inheritdoc />
    protected override string GetRecordId(KinesisEvent.KinesisEventRecord record) => record.Kinesis.SequenceNumber;
}