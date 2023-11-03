# AWS.Lambda.Powertools.BatchProcessing

The batch processing utility handles partial failures when processing batches from Amazon SQS, Amazon Kinesis Data Streams, and Amazon DynamoDB Streams.

## Key features

* Reports batch item failures to reduce number of retries for a record upon errors
* Simple interface to process each batch record
* Bring your own batch processor
* Parallel processing

## Background

When using SQS, Kinesis Data Streams, or DynamoDB Streams as a Lambda event source, your Lambda functions are triggered with a batch of messages.

If your function fails to process any message from the batch, the entire batch returns to your queue or stream. This same batch is then retried until either condition happens first: a) your Lambda function returns a successful response, b) record reaches maximum retry attempts, or c) when records expire.

This behavior changes when you enable Report Batch Item Failures feature in your Lambda function event source configuration:

* [SQS queues](https://docs.powertools.aws.dev/lambda/dotnet/utilities/batch-processing/#sqs-standard). Only messages reported as failure will return to the queue for a retry, while successful ones will be deleted.
* [Kinesis data streams](https://docs.powertools.aws.dev/lambda/dotnet/utilities/batch-processing/#kinesis-and-dynamodb-streams) and [DynamoDB streams](https://docs.powertools.aws.dev/lambda/dotnet/utilities/batch-processing/#kinesis-and-dynamodb-streams). Single reported failure will use its sequence number as the stream checkpoint. Multiple reported failures will use the lowest sequence number as checkpoint.

## Read the docs

For a full list of features go to [docs.powertools.aws.dev/lambda/dotnet/utilities/batch-processing/](https://docs.powertools.aws.dev/lambda/dotnet/utilities/batch-processing/)

GitHub: https://github.com/aws-powertools/powertools-lambda-dotnet/

## Sample Function

View the full example here: [github.com/aws-powertools/powertools-lambda-dotnet/tree/develop/examples/BatchProcessing](https://github.com/aws-powertools/powertools-lambda-dotnet/tree/develop/examples/BatchProcessing)

```csharp
[BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler))]
public BatchItemFailuresResponse HandlerUsingAttribute(SQSEvent _)
{
    return SqsBatchProcessor.Result.BatchItemFailuresResponse;
}

public class CustomSqsRecordHandler : ISqsRecordHandler
{
    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        /*
            Your business logic. 
            If an exception is thrown, the item will be marked as a partial batch item failure.
        */
        
        var product = JsonSerializer.Deserialize<JsonElement>(record.Body);

        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on 4");
        }

        return await Task.FromResult(RecordHandlerResult.None);
    }
}
```