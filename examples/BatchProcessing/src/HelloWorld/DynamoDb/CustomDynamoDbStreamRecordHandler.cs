using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.DynamoDb;
internal class CustomDynamoDbStreamRecordHandler : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>
{
    public async Task HandleAsync(DynamoDBEvent.DynamodbStreamRecord record)
    {
        Logger.LogInformation($"Handling DynamoDB event with sequence number: {record.Dynamodb.SequenceNumber}");
        await Task.CompletedTask;
    }
}
