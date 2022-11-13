using System;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.Sqs;
public class CustomSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
{
    public async Task HandleAsync(SQSEvent.SQSMessage record)
    {
        Logger.LogInformation($"Handling SQS record with message id: '{record.MessageId}'.");

        // Business logic
        var msgNo = int.Parse(record.Body.Split("_")[1]);
        if (msgNo % 5 == 0)
        {
            throw new InvalidOperationException("Business logic error.");
        }

        await Task.CompletedTask;
    }
}
