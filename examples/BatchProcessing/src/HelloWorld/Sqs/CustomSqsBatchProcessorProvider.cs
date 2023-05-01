using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace HelloWorld.Sqs;
internal class CustomSqsBatchProcessorProvider : IBatchProcessorProvider<SQSEvent, SQSEvent.SQSMessage>
{
    public IBatchProcessor<SQSEvent, SQSEvent.SQSMessage> Create()
    {
        Logger.LogInformation($"Creating SQS batch processor using: '{GetType().Name}'.");
        return Services.Provider.GetRequiredService<CustomSqsBatchProcessor>();
    }
}
