// This file is referenced by docs/utilities/batch-processing.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:sqs_record_handler_error_handling]
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
// --8<-- [end:sqs_record_handler_error_handling]

// --8<-- [start:error_handling_policy_attribute]
[BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler),
	ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
public BatchItemFailuresResponse HandlerUsingAttribute(DynamoDBEvent _)
{
	return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:error_handling_policy_attribute]

// --8<-- [start:typed_custom_error_handling]
[BatchProcessor(
    TypedRecordHandler = typeof(TypedSqsHandler),
    ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
public BatchItemFailuresResponse ProcessWithErrorPolicy(SQSEvent sqsEvent)
{
    return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:typed_custom_error_handling]
