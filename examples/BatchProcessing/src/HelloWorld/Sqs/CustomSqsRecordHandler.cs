using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.Sqs;
public class CustomSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
{
    public async Task HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Handling SQS record with message id: '{record.MessageId}'.");
        Logger.LogInformation("Doing async operation...");
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        Logger.LogInformation("Async operation complete.");
        await Task.CompletedTask;
    }
}
