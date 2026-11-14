// This file is referenced by docs/utilities/batch-processing.md
// via pymdownx.snippets (mkdocs).


using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Docs.Snippets.BatchProcessing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Lambda.Powertools.Docs.Snippets.Batch.AdvancedErrorHandling.SqsRecordHandlerErrorHandling
{
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
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Batch.AdvancedErrorHandling.ErrorHandlingPolicyAttribute
{
public class Function
{
// --8<-- [start:error_handling_policy_attribute]
[BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler),
	ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
public BatchItemFailuresResponse HandlerUsingAttribute(DynamoDBEvent _)
{
	return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:error_handling_policy_attribute]
}
}


namespace AWS.Lambda.Powertools.Docs.Snippets.Batch.AdvancedErrorHandling.TypedCustomErrorHandling
{
public class Function
{
// --8<-- [start:typed_custom_error_handling]
[BatchProcessor(
    TypedRecordHandler = typeof(TypedSqsHandler),
    ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
public BatchItemFailuresResponse ProcessWithErrorPolicy(SQSEvent sqsEvent)
{
    return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:typed_custom_error_handling]
}
}
