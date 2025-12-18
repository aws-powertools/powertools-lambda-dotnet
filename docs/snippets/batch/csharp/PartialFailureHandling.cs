// This file is referenced by docs/utilities/batch-processing.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:throw_on_full_batch_failure_decorator]
	[BatchProcessor(
        RecordHandler = typeof(CustomSqsRecordHandler),
        ThrowOnFullBatchFailure = false)]
	public BatchItemFailuresResponse HandlerUsingAttribute(SQSEvent _)
	{
		return SqsBatchProcessor.Result.BatchItemFailuresResponse;
	}
// --8<-- [end:throw_on_full_batch_failure_decorator]

// --8<-- [start:throw_on_full_batch_failure_outside_decorator]
public async Task<BatchItemFailuresResponse> HandlerUsingUtility(SQSEvent sqsEvent)
{
    var result = await SqsBatchProcessor.Instance.ProcessAsync(sqsEvent, RecordHandler<SQSEvent.SQSMessage>.From(x =>
    {
        // Inline handling of SQS message...
    }), new ProcessingOptions
    {
        ThrowOnFullBatchFailure = false
    });
    return result.BatchItemFailuresResponse;
}
// --8<-- [end:throw_on_full_batch_failure_outside_decorator]

// --8<-- [start:extending_batch_processor]

public class CustomDynamoDbStreamBatchProcessor : DynamoDbStreamBatchProcessor
{
	public override async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync(DynamoDBEvent @event,
	IRecordHandler<DynamoDBEvent.DynamodbStreamRecord> recordHandler, ProcessingOptions processingOptions)
	{
		ProcessingResult = new ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>();

		// Prepare batch records (order is preserved)
		var batchRecords = GetRecordsFromEvent(@event).Select(x => new KeyValuePair<string, DynamoDBEvent.DynamodbStreamRecord>(GetRecordId(x), x))
			.ToArray();

		// We assume all records fail by default to avoid loss of data
		var failureBatchRecords = batchRecords.Select(x => new KeyValuePair<string, RecordFailure<DynamoDBEvent.DynamodbStreamRecord>>(x.Key,
			new RecordFailure<DynamoDBEvent.DynamodbStreamRecord>
			{
				Exception = new UnprocessedRecordException($"Record: '{x.Key}' has not been processed."),
				Record = x.Value
			}));

		// Override to fail on first failure
		var errorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure;

		var successRecords = new Dictionary<string, RecordSuccess<DynamoDBEvent.DynamodbStreamRecord>>();
		var failureRecords = new Dictionary<string, RecordFailure<DynamoDBEvent.DynamodbStreamRecord>>(failureBatchRecords);

		try
		{
			foreach (var pair in batchRecords)
			{
				var (recordId, record) = pair;

				try
				{
					var result = await HandleRecordAsync(record, recordHandler, CancellationToken.None);
					failureRecords.Remove(recordId, out _);
					successRecords.TryAdd(recordId, new RecordSuccess<DynamoDBEvent.DynamodbStreamRecord>
					{
						Record = record,
						RecordId = recordId,
						HandlerResult = result
					});
				}
				catch (Exception ex)
				{
					// Capture exception
					failureRecords[recordId] = new RecordFailure<DynamoDBEvent.DynamodbStreamRecord>
					{
						Exception = new RecordProcessingException(
							$"Failed processing record: '{recordId}'. See inner exception for details.", ex),
						Record = record,
						RecordId = recordId
					};

					Metrics.AddMetric("BatchRecordFailures", 1, MetricUnit.Count);

					try
					{
						// Invoke hook
						await HandleRecordFailureAsync(record, ex);
					}
					catch
					{
						// NOOP
					}

					// Check if we should stop record processing on first error
					// ReSharper disable once ConditionIsAlwaysTrueOrFalse
					if (errorHandlingPolicy == BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)
					{
						// This causes the loop's (inner) cancellation token to be cancelled for all operations already scheduled internally
						throw new CircuitBreakerException(
							"Error handling policy is configured to stop processing on first batch item failure. See inner exception for details.",
							ex);
					}
				}
			}
		}
		catch (Exception ex) when (ex is CircuitBreakerException or OperationCanceledException)
		{
			// NOOP
		}

		ProcessingResult.BatchRecords.AddRange(batchRecords.Select(x => x.Value));
		ProcessingResult.BatchItemFailuresResponse.BatchItemFailures.AddRange(failureRecords.Select(x =>
			new BatchItemFailuresResponse.BatchItemFailure
			{
				ItemIdentifier = x.Key
			}));
		ProcessingResult.FailureRecords.AddRange(failureRecords.Values);

		ProcessingResult.SuccessRecords.AddRange(successRecords.Values);

		return ProcessingResult;
	}

	// ReSharper disable once RedundantOverriddenMember
	protected override async Task HandleRecordFailureAsync(DynamoDBEvent.DynamodbStreamRecord record, Exception exception)
	{
		await base.HandleRecordFailureAsync(record, exception);
	}
}
// --8<-- [end:extending_batch_processor]

// --8<-- [start:typed_handler_test]
[Fact]
public async Task TypedHandler_ValidProduct_ProcessesSuccessfully()
{
	// Arrange
	var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };
	var handler = new TypedSqsRecordHandler();
	var cancellationToken = CancellationToken.None;

	// Act
	var result = await handler.HandleAsync(product, cancellationToken);

	// Assert
	Assert.Equal(RecordHandlerResult.None, result);
}

[Fact]
public async Task TypedHandler_InvalidProduct_ThrowsException()
{
	// Arrange
	var product = new Product { Id = 4, Name = "Invalid", Price = -10 };
	var handler = new TypedSqsRecordHandler();

	// Act & Assert
	await Assert.ThrowsAsync<ArgumentException>(() =>
		handler.HandleAsync(product, CancellationToken.None));
}
// --8<-- [end:typed_handler_test]

// --8<-- [start:integration_test]
[Fact]
public async Task ProcessSqsEvent_WithTypedHandler_ProcessesAllRecords()
{
	// Arrange
	var sqsEvent = new SQSEvent
	{
		Records = new List<SQSEvent.SQSMessage>
		{
			new() {
				MessageId = "1",
				Body = JsonSerializer.Serialize(new Product { Id = 1, Name = "Product 1", Price = 10 }),
				EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:my-queue"
			},
			new() {
				MessageId = "2",
				Body = JsonSerializer.Serialize(new Product { Id = 2, Name = "Product 2", Price = 20 }),
				EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:my-queue"
			}
		}
	};

	var function = new TypedFunction();

	// Act
	var result = function.HandlerUsingTypedAttribute(sqsEvent);

	// Assert
	Assert.Empty(result.BatchItemFailures);
}
// --8<-- [end:integration_test]

// --8<-- [start:traditional_handler_test]
[Fact]
public Task Sqs_Handler_Using_Attribute()
{
	var request = new SQSEvent
	{
		Records = TestHelper.SqsMessages
	};

	var function = new HandlerFunction();

	var response = function.HandlerUsingAttribute(request);

	Assert.Equal(2, response.BatchItemFailures.Count);
	Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
	Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);

	return Task.CompletedTask;
}
// --8<-- [end:traditional_handler_test]

// --8<-- [start:function_handler_using_attribute]
[BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler))]
public BatchItemFailuresResponse HandlerUsingAttribute(SQSEvent _)
{
    return SqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:function_handler_using_attribute]

// --8<-- [start:custom_sqs_record_handler]
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
// --8<-- [end:custom_sqs_record_handler]

// --8<-- [start:sqs_event_test_helper]
internal static List<SQSEvent.SQSMessage> SqsMessages => new()
{
	new SQSEvent.SQSMessage
	{
		MessageId = "1",
		Body = "{\"Id\":1,\"Name\":\"product-4\",\"Price\":14}",
		EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
	},
	new SQSEvent.SQSMessage
	{
		MessageId = "2",
		Body = "fail",
		EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
	},
	new SQSEvent.SQSMessage
	{
		MessageId = "3",
		Body = "{\"Id\":3,\"Name\":\"product-4\",\"Price\":14}",
		EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
	},
	new SQSEvent.SQSMessage
	{
		MessageId = "4",
		Body = "{\"Id\":4,\"Name\":\"product-4\",\"Price\":14}",
		EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
	},
	new SQSEvent.SQSMessage
	{
		MessageId = "5",
		Body = "{\"Id\":5,\"Name\":\"product-4\",\"Price\":14}",
		EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
	},
};
// --8<-- [end:sqs_event_test_helper]
