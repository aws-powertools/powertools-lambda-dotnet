using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;

namespace Docs.BatchProcessingSnippets;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class Order { }
public class Customer { }

public class CustomDynamoDbStreamRecordHandler : IDynamoDbStreamRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(Amazon.Lambda.DynamoDBEvents.DynamoDBEvent.DynamodbStreamRecord record,
        CancellationToken cancellationToken) => Task.FromResult(RecordHandlerResult.None);
}

public class TypedSqsHandler : ITypedRecordHandler<Product>
{
    public Task<RecordHandlerResult> HandleAsync(Product product, CancellationToken cancellationToken) =>
        Task.FromResult(RecordHandlerResult.None);
}

public class CustomSqsRecordHandler : ISqsRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken) =>
        Task.FromResult(RecordHandlerResult.None);
}

public class TypedSqsRecordHandler : ITypedRecordHandler<Product>
{
    public Task<RecordHandlerResult> HandleAsync(Product product, CancellationToken cancellationToken) =>
        Task.FromResult(RecordHandlerResult.None);
}

public class TypedFunction
{
    public BatchItemFailuresResponse HandlerUsingTypedAttribute(SQSEvent sqsEvent) => TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
}

public class HandlerFunction
{
    public BatchItemFailuresResponse HandlerUsingAttribute(SQSEvent sqsEvent) => SqsBatchProcessor.Result.BatchItemFailuresResponse;
}

public static class TestHelper
{
    public static List<SQSEvent.SQSMessage> SqsMessages => new();
}
