using System.Text;
using System.Text.Json;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using Xunit;

namespace PackageSmoke.Tests;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class SqsRecordHandler : ISqsRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        var product = JsonSerializer.Deserialize<Product>(record.Body);
        if (product.Id == 2) throw new ArgumentException("Error on id 2");
        return Task.FromResult(RecordHandlerResult.None);
    }
}

public class TypedProductHandler : ITypedRecordHandler<Product>
{
    public Task<RecordHandlerResult> HandleAsync(Product product, CancellationToken cancellationToken)
    {
        if (product.Id == 2) throw new ArgumentException("Error on id 2");
        return Task.FromResult(RecordHandlerResult.None);
    }
}

public class KinesisRecordHandler : IKinesisEventRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record, CancellationToken cancellationToken)
    {
        var data = Encoding.UTF8.GetString(record.Kinesis.Data.ToArray());
        if (data == "fail") throw new ArgumentException("Error on record");
        return Task.FromResult(RecordHandlerResult.None);
    }
}

public class DynamoDbRecordHandler : IDynamoDbStreamRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record, CancellationToken cancellationToken)
    {
        if (record.Dynamodb.NewImage["Name"].S == "fail") throw new ArgumentException("Error on record");
        return Task.FromResult(RecordHandlerResult.None);
    }
}

public class BatchProcessingFunction
{
    [BatchProcessor(RecordHandler = typeof(SqsRecordHandler))]
    public BatchItemFailuresResponse SqsHandler(SQSEvent _) => SqsBatchProcessor.Result.BatchItemFailuresResponse;

    [BatchProcessor(TypedRecordHandler = typeof(TypedProductHandler))]
    public BatchItemFailuresResponse TypedSqsHandler(SQSEvent _) => TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;

    [BatchProcessor(RecordHandler = typeof(KinesisRecordHandler))]
    public BatchItemFailuresResponse KinesisHandler(KinesisEvent _) => KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;

    [BatchProcessor(RecordHandler = typeof(DynamoDbRecordHandler))]
    public BatchItemFailuresResponse DynamoDbHandler(DynamoDBEvent _) => DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
}

public class BatchProcessingSmokeTests
{
    private static SQSEvent SqsEvent(string queueArn) => new()
    {
        Records = Enumerable.Range(1, 3).Select(id => new SQSEvent.SQSMessage
        {
            MessageId = id.ToString(),
            Body = $"{{\"Id\":{id},\"Name\":\"product-{id}\"}}",
            EventSourceArn = queueArn
        }).ToList()
    };

    [Fact]
    public void Sqs_Handler_Reports_Failed_Record()
    {
        var response = new BatchProcessingFunction().SqsHandler(SqsEvent("arn:aws:sqs:us-east-1:123456789012:queue"));

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("2", failure.ItemIdentifier);
    }

    [Fact]
    public void Sqs_Fifo_Handler_Fails_Every_Record_After_The_First_Failure()
    {
        var response = new BatchProcessingFunction().SqsHandler(SqsEvent("arn:aws:sqs:us-east-1:123456789012:queue.fifo"));

        Assert.Equal(["2", "3"], response.BatchItemFailures.Select(f => f.ItemIdentifier));
    }

    [Fact]
    public void Typed_Sqs_Handler_Deserializes_Records_And_Reports_Failed_Record()
    {
        var response = new BatchProcessingFunction().TypedSqsHandler(SqsEvent("arn:aws:sqs:us-east-1:123456789012:queue"));

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("2", failure.ItemIdentifier);
    }

    [Fact]
    public void Kinesis_Handler_Reports_Failed_Sequence_Number()
    {
        var kinesisEvent = new KinesisEvent
        {
            Records = new[] { "ok", "fail" }.Select((data, i) => new KinesisEvent.KinesisEventRecord
            {
                EventSourceARN = "arn:aws:kinesis:us-east-1:123456789012:stream/smoke",
                Kinesis = new KinesisEvent.Record
                {
                    SequenceNumber = $"seq-{i}",
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(data))
                }
            }).ToList()
        };

        var response = new BatchProcessingFunction().KinesisHandler(kinesisEvent);

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("seq-1", failure.ItemIdentifier);
    }

    [Fact]
    public void DynamoDb_Handler_Reports_Failed_Sequence_Number()
    {
        var dynamoDbEvent = new DynamoDBEvent
        {
            Records = new[] { "ok", "fail" }.Select((name, i) => new DynamoDBEvent.DynamodbStreamRecord
            {
                EventSourceArn = "arn:aws:dynamodb:us-east-1:123456789012:table/smoke/stream/2024",
                Dynamodb = new DynamoDBEvent.StreamRecord
                {
                    SequenceNumber = $"seq-{i}",
                    NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                    {
                        ["Name"] = new() { S = name }
                    }
                }
            }).ToList()
        };

        var response = new BatchProcessingFunction().DynamoDbHandler(dynamoDbEvent);

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("seq-1", failure.ItemIdentifier);
    }
}
