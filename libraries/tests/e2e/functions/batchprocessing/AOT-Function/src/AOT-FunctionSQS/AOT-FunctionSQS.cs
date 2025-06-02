// AOT-FunctionSQS/Program.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;

namespace AOT_FunctionSQS;

public class Program
{
    private static async Task Main()
    {
        Func<SQSEvent, ILambdaContext, BatchItemFailuresResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<CustomJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [BatchProcessor(RecordHandler = typeof(SqsRecordHandler))]
    public static BatchItemFailuresResponse FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
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

[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(BatchItemFailuresResponse))]
public partial class CustomJsonSerializerContext : JsonSerializerContext
{
}