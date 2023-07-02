using System;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.Sqs;
internal class CustomSqsBatchProcessor : SqsBatchProcessor
{
    public override Task<BatchResponse> ProcessAsync(SQSEvent @event, IRecordHandler<SQSEvent.SQSMessage> recordHandler, ProcessingOptions processingOptions)
    {
        Logger.LogInformation($"Processing {@event.Records.Count} record(s) using: '{GetType().Name}'.");
        return base.ProcessAsync(@event, recordHandler, processingOptions);
    }

    protected override async Task HandleRecordFailureAsync(SQSEvent.SQSMessage record, Exception exception)
    {
        Logger.LogWarning(exception, $"Failed to process record: '{record.MessageId}'.");
        await base.HandleRecordFailureAsync(record, exception);
    }
}
