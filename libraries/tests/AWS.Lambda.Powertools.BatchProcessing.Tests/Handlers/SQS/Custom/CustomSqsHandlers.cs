using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Custom;

public class CustomSqsRecordHandler : ISqsRecordHandler
{
    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        var product = JsonSerializer.Deserialize<JsonElement>(record.Body);
        
        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on 4");
        }
        
        return await Task.FromResult(RecordHandlerResult.None);
    }
}

public class CustomFailSqsRecordHandler : ISqsRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        throw new ArgumentException("Raise exception on all!");
    }
}

public class BadCustomSqsRecordHandler
{
}