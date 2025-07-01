using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Custom;

public class CustomDynamoDbStreamRecordHandler : IDynamoDbStreamRecordHandler
{
    public async Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record,
        CancellationToken cancellationToken)
    {
        var product = JsonSerializer.Deserialize<JsonElement>(record.Dynamodb.NewImage["Product"].S);
        
        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on id 4");
        }

        // Return some data (not needed but useful for test coverage)
        return await Task.FromResult(RecordHandlerResult.FromData(product));
    }
}

internal class CustomFailDynamoDbStreamRecordHandler : IDynamoDbStreamRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record,
        CancellationToken cancellationToken)
    {
        throw new ArgumentException("Raise exception on all!");
    }
}

public class BadCustomDynamoDbStreamRecordHandler
{
}