using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FunctionKinesis;

public class Function
{
    /// <summary>
    /// Process Kinesis events
    /// </summary>
    [BatchProcessor(RecordHandler = typeof(KinesisRecordHandler))]
    public BatchItemFailuresResponse FunctionHandler(KinesisEvent kinesisEvent, ILambdaContext context)
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
            
            // Example: Check if the data contains specific fields
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