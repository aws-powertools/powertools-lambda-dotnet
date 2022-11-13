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
        Logger.LogInformation($"Handling SQS message with id={record.MessageId}, body={record.Body}...");

        // Business logic
        var msgNo = int.Parse(record.Body.Split("_")[1]);
        if (msgNo % 5 == 0)
        {
            throw new InvalidOperationException($"{record.Body} could not be processed...");
        }

        await Task.CompletedTask;
    }
}
