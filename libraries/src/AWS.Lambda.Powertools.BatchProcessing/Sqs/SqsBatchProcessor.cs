using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// The default batch processor for SQS events.
/// </summary>
public class SqsBatchProcessor : BatchProcessor<SQSEvent, SQSEvent.SQSMessage>, ISqsBatchProcessor
{
    /// <summary>
    /// The singleton instance of the batch processor.
    /// </summary>
    private static ISqsBatchProcessor _instance;

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static ISqsBatchProcessor Instance => _instance ??= new SqsBatchProcessor();
    
    /// <summary>
    /// Need default constructor for when consumers create a custom batch processor
    /// </summary>
    public SqsBatchProcessor()
    {
        _instance = this;
    }
    
    /// <summary>
    /// Return the instance ProcessingResult
    /// </summary>
    public static ProcessingResult<SQSEvent.SQSMessage> Result => _instance.ProcessingResult;

    /// <inheritdoc />
    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(SQSEvent @event)
    {
        var isSqsFifoSource = @event.Records.FirstOrDefault()?.EventSourceArn
            ?.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
        return isSqsFifoSource == true
            ? BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure
            : BatchProcessorErrorHandlingPolicy.ContinueOnBatchItemFailure;
    }

    /// <inheritdoc />
    protected override ICollection<SQSEvent.SQSMessage> GetRecordsFromEvent(SQSEvent @event) => @event.Records;

    /// <inheritdoc />
    protected override string GetRecordId(SQSEvent.SQSMessage record) => record.MessageId;
}