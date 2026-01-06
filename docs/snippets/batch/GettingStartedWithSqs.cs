// This file is referenced by docs/utilities/batch-processing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.BatchProcessing;

// --8<-- [start:sqs_typed_handler_decorator]
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
    }

    public class TypedSqsRecordHandler : ITypedRecordHandler<Product> // (1)!
    {
    	public async Task<RecordHandlerResult> HandleAsync(Product product, CancellationToken cancellationToken)
    	{
    		 /*
    		 * Your business logic with automatic deserialization.
    		 * If an exception is thrown, the item will be marked as a partial batch item failure.
             */

             Logger.LogInformation($"Processing product {product.Id} - {product.Name} (${product.Price})");

             if (product.Id == 4) // (2)!
             {
                 throw new ArgumentException("Error on id 4");
             }

             return await Task.FromResult(RecordHandlerResult.None); // (3)!
         }

	}

    [BatchProcessor(TypedRecordHandler = typeof(TypedSqsRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingTypedAttribute(SQSEvent _)
    {
    	return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse; // (4)!
    }
// --8<-- [end:sqs_typed_handler_decorator]

// --8<-- [start:sqs_handler_decorator_traditional]
    public class CustomSqsRecordHandler : ISqsRecordHandler // (1)!
    {
    	public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    	{
    		 /*
    		 * Your business logic.
    		 * If an exception is thrown, the item will be marked as a partial batch item failure.
             */

             var product = JsonSerializer.Deserialize<Product>(record.Body);

             if (product.Id == 4) // (2)!
             {
                 throw new ArgumentException("Error on id 4");
             }

             return await Task.FromResult(RecordHandlerResult.None); // (3)!
         }

	}

    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttribute(SQSEvent _)
    {
    	return SqsBatchProcessor.Result.BatchItemFailuresResponse; // (4)!
    }
// --8<-- [end:sqs_handler_decorator_traditional]

// --8<-- [start:typed_handler_with_context]
public class ProductHandlerWithContext : ITypedRecordHandlerWithContext<Product>
{
    public async Task<RecordHandlerResult> HandleAsync(Product product, ILambdaContext context, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing product {product.Id} in request {context.AwsRequestId}");
        Logger.LogInformation($"Remaining time: {context.RemainingTime.TotalSeconds}s");

        // Use context for timeout handling
        if (context.RemainingTime.TotalSeconds < 5)
        {
            Logger.LogWarning("Low remaining time, processing quickly");
        }

        return RecordHandlerResult.None;
    }
}
// --8<-- [end:typed_handler_with_context]

// --8<-- [start:function_usage_with_context]
[BatchProcessor(TypedRecordHandlerWithContext = typeof(ProductHandlerWithContext))]
public BatchItemFailuresResponse ProcessWithContext(SQSEvent sqsEvent, ILambdaContext context)
{
    return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:function_usage_with_context]

// --8<-- [start:migration_before_traditional]
public class TraditionalSqsHandler : ISqsRecordHandler
{
    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        // Manual deserialization
        var product = JsonSerializer.Deserialize<Product>(record.Body);

        Logger.LogInformation($"Processing product {product.Id}");

        if (product.Price < 0)
            throw new ArgumentException("Invalid price");

        return RecordHandlerResult.None;
    }
}

[BatchProcessor(RecordHandler = typeof(TraditionalSqsHandler))]
public BatchItemFailuresResponse ProcessSqs(SQSEvent sqsEvent)
{
    return SqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:migration_before_traditional]

// --8<-- [start:migration_after_typed]
public class TypedSqsHandler : ITypedRecordHandler<Product>
{
    public async Task<RecordHandlerResult> HandleAsync(Product product, CancellationToken cancellationToken)
    {
        // Automatic deserialization - product is already deserialized!
        Logger.LogInformation($"Processing product {product.Id}");

        // Same business logic
        if (product.Price < 0)
            throw new ArgumentException("Invalid price");

        return RecordHandlerResult.None;
    }
}

[BatchProcessor(TypedRecordHandler = typeof(TypedSqsHandler))]
public BatchItemFailuresResponse ProcessSqs(SQSEvent sqsEvent)
{
    return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:migration_after_typed]
