using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

public class SqsBatchProcessor : BatchProcessor<SQSEvent, SQSEvent.SQSMessage>
{
    public static readonly SqsBatchProcessor Instance = new();

    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(SQSEvent @event)
    {
        var isSqsFifoSource = @event.Records.FirstOrDefault()?.EventSourceArn?.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
        return isSqsFifoSource == true
            ? BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure
            : BatchProcessorErrorHandlingPolicy.ContinueOnBatchItemFailure;
    }

    protected override ICollection<SQSEvent.SQSMessage> GetRecordsFromEvent(SQSEvent @event) => @event.Records;

    protected override string GetRecordId(SQSEvent.SQSMessage record) => record.MessageId;
}