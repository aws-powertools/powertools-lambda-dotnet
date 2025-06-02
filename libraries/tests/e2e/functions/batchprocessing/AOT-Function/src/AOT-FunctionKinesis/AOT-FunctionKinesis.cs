// AOT-FunctionKinesis/Program.cs
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;

namespace AOT_FunctionKinesis;

public class Program
{
    private static async Task Main()
    {
        Func<KinesisEvent, ILambdaContext, BatchItemFailuresResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<CustomJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [BatchProcessor(RecordHandler = typeof(KinesisRecordHandler))]
    public static BatchItemFailuresResponse FunctionHandler(KinesisEvent kinesisEvent, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing {kinesisEvent.Records.Count} records");
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
}

public class KinesisRecordHandler : IRecordHandler<KinesisEvent.KinesisEventRecord>
{
    public Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            // Process the Kinesis record
            Console.WriteLine($"Processing record: {record.EventId}");
            
            // Decode and parse the data
            string data = Encoding.UTF8.GetString(record.Kinesis.Data.ToArray());
            var dataJson = JsonSerializer.Deserialize<JsonElement>(data);
            
            if (dataJson.TryGetProperty("status", out var status) && status.GetString() == "error")
            {
                throw new Exception($"Failed to process record with status error: {record.EventId}");
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

[JsonSerializable(typeof(KinesisEvent))]
[JsonSerializable(typeof(BatchItemFailuresResponse))]
public partial class CustomJsonSerializerContext : JsonSerializerContext
{
}