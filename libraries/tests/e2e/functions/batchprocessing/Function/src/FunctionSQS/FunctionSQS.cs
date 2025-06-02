// FunctionSQS/Function.cs
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FunctionSQS;

public class Function
{
    /// <summary>
    /// Process SQS events
    /// </summary>
    [BatchProcessor(RecordHandler = typeof(SqsRecordHandler))]
    public BatchItemFailuresResponse FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing {sqsEvent.Records.Count} records");
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
}

public class SqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
{
    public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Process the SQS message
            Console.WriteLine($"Processing message: {message.MessageId}");
            
            // Attempt to parse the message body as JSON
            var messageBody = message.Body;
            var messageJson = JsonSerializer.Deserialize<JsonElement>(messageBody);
            
            // Example: Inspect messageJson for specific content and process accordingly
            if (messageJson.TryGetProperty("action", out var action) && action.GetString() == "fail")
            {
                throw new Exception($"Failed to process message with ID: {message.MessageId}");
            }
            
            return Task.FromResult(RecordHandlerResult.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            return Task.FromResult(RecordHandlerResult.None);
        }
    }
}