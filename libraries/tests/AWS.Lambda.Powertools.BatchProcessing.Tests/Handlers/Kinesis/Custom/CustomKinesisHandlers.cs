using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Custom;

internal class CustomKinesisEventRecordHandler : IKinesisEventRecordHandler
{
    public async Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record,
        CancellationToken cancellationToken)
    {
        var product = JsonSerializer.Deserialize<JsonElement>(record.Kinesis.Data);

        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on id 4");
        }

        return await Task.FromResult(RecordHandlerResult.None);
    }
}

internal class CustomFailKinesisEventRecordHandler : IKinesisEventRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record,
        CancellationToken cancellationToken)
    {
        throw new ArgumentException("Raise exception on all!");
    }
}

public class BadCustomKinesisEventRecordHandler
{
}