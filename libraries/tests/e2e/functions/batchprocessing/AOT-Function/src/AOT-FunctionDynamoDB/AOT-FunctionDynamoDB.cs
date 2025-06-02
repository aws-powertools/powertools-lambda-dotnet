using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

namespace AOT_FunctionDynamoDB;

public class Program
{
    private static async Task Main()
    {
        Func<DynamoDBEvent, ILambdaContext, BatchItemFailuresResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<CustomJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [BatchProcessor(RecordHandler = typeof(DynamoDbRecordHandler))]
    public static BatchItemFailuresResponse FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing {dynamoEvent.Records.Count} records");
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
}

public class DynamoDbRecordHandler : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>
{
    public Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            // Process the DynamoDB record
            if (record.Dynamodb.NewImage != null && record.Dynamodb.NewImage.TryGetValue("id", out var idValue))
            {
                Console.WriteLine($"Processing record with id: {idValue.S}");
                
                if (idValue.S?.Contains("fail") == true)
                {
                    throw new Exception($"Failed to process record with id: {idValue.S}");
                }
            }
            
            return Task.FromResult(RecordHandlerResult.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing record: {ex.Message}");
            return Task.FromResult(RecordHandlerResult.None);
        }
    }
}

[JsonSerializable(typeof(DynamoDBEvent))]
[JsonSerializable(typeof(BatchItemFailuresResponse))]
public partial class CustomJsonSerializerContext : JsonSerializerContext
{
}