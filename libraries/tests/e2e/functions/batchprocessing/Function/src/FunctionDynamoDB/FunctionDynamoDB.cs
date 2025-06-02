// FunctionDynamoDB/Function.cs
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FunctionDynamoDB;

public class Function
{
    /// <summary>
    /// Process DynamoDB Stream events
    /// </summary>
    [BatchProcessor(RecordHandler = typeof(DynamoDbRecordHandler))]
    public BatchItemFailuresResponse FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
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
            // Example: Check if NewImage contains certain fields and process based on that
            if (record.Dynamodb.NewImage != null && record.Dynamodb.NewImage.TryGetValue("id", out var idValue))
            {
                // Process the record based on id
                Console.WriteLine($"Processing record with id: {idValue.S}");
                
                // Simulate failed processing for specific IDs
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