using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace HelloWorld.Sqs;
internal class CustomSqsRecordHandlerProvider : IRecordHandlerProvider<SQSEvent.SQSMessage>
{
    public IRecordHandler<SQSEvent.SQSMessage> Create()
    {
        return Services.Provider.GetRequiredService<CustomSqsRecordHandler>();
    }
}
