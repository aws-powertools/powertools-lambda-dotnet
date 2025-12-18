// This file is referenced by docs/utilities/batch-processing.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:kinesis_typed_handler_decorator]
public class Order
{
    public string? OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public List<Product> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

internal class TypedKinesisRecordHandler : ITypedRecordHandler<Order> // (1)!
{
	public async Task<RecordHandlerResult> HandleAsync(Order order, CancellationToken cancellationToken)
	{
		Logger.LogInformation($"Processing order {order.OrderId} with {order.Items.Count} items");

		if (order.TotalAmount <= 0) // (2)!
		{
			throw new ArgumentException("Invalid order total");
		}

		return await Task.FromResult(RecordHandlerResult.None); // (3)!
	}
}

[BatchProcessor(TypedRecordHandler = typeof(TypedKinesisRecordHandler))]
public BatchItemFailuresResponse HandlerUsingTypedAttribute(KinesisEvent _)
{
	return TypedKinesisEventBatchProcessor.Result.BatchItemFailuresResponse; // (4)!
}
// --8<-- [end:kinesis_typed_handler_decorator]

// --8<-- [start:kinesis_handler_decorator_traditional]
internal class CustomKinesisEventRecordHandler : IKinesisEventRecordHandler // (1)!
{
	public async Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record, CancellationToken cancellationToken)
	{
		var product = JsonSerializer.Deserialize<Product>(record.Kinesis.Data);

		if (product.Id == 4) // (2)!
		{
			throw new ArgumentException("Error on id 4");
		}

		return await Task.FromResult(RecordHandlerResult.None); // (3)!
	}
}


[BatchProcessor(RecordHandler = typeof(CustomKinesisEventRecordHandler))]
public BatchItemFailuresResponse HandlerUsingAttribute(KinesisEvent _)
{
	return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse; // (4)!
}
// --8<-- [end:kinesis_handler_decorator_traditional]

// --8<-- [start:dynamodb_typed_handler_decorator]
public class Customer
{
    public string? CustomerId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal class TypedDynamoDbRecordHandler : ITypedRecordHandler<Customer> // (1)!
{
	public async Task<RecordHandlerResult> HandleAsync(Customer customer, CancellationToken cancellationToken)
	{
		Logger.LogInformation($"Processing customer {customer.CustomerId} - {customer.Name}");

		if (string.IsNullOrEmpty(customer.Email)) // (2)!
		{
			throw new ArgumentException("Customer email is required");
		}

		return await Task.FromResult(RecordHandlerResult.None); // (3)!
	}
}

[BatchProcessor(TypedRecordHandler = typeof(TypedDynamoDbRecordHandler))]
public BatchItemFailuresResponse HandlerUsingTypedAttribute(DynamoDBEvent _)
{
	return TypedDynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse; // (4)!
}
// --8<-- [end:dynamodb_typed_handler_decorator]

// --8<-- [start:dynamodb_handler_decorator_traditional]
internal class CustomDynamoDbStreamRecordHandler : IDynamoDbStreamRecordHandler // (1)!
{
	public async Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record, CancellationToken cancellationToken)
	{
		var product = JsonSerializer.Deserialize<Product>(record.Dynamodb.NewImage["Product"].S);

		if (product.Id == 4) // (2)!
		{
			throw new ArgumentException("Error on id 4");
		}

		return await Task.FromResult(RecordHandlerResult.None); // (3)!
	}
}


[BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler))]
public BatchItemFailuresResponse HandlerUsingAttribute(DynamoDBEvent _)
{
	return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse; // (4)!
}
// --8<-- [end:dynamodb_handler_decorator_traditional]

// --8<-- [start:using_utility_outside_decorator]
public async Task<BatchItemFailuresResponse> HandlerUsingUtility(DynamoDBEvent dynamoDbEvent)
{
	var result = await DynamoDbStreamBatchProcessor.Instance.ProcessAsync(dynamoDbEvent, RecordHandler<DynamoDBEvent.DynamodbStreamRecord>.From(record =>
    {
        var product = JsonSerializer.Deserialize<JsonElement>(record.Dynamodb.NewImage["Product"].S);

        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on 4");
        }
    }));
    return result.BatchItemFailuresResponse;
}
// --8<-- [end:using_utility_outside_decorator]

// --8<-- [start:using_utility_from_ioc_getrequiredservice]
public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIoc(DynamoDBEvent dynamoDbEvent)
{
    var batchProcessor = Services.Provider.GetRequiredService<IDynamoDbStreamBatchProcessor>();
    var recordHandler = Services.Provider.GetRequiredService<IDynamoDbStreamRecordHandler>();
    var result = await batchProcessor.ProcessAsync(dynamoDbEvent, recordHandler);
    return result.BatchItemFailuresResponse;
}
// --8<-- [end:using_utility_from_ioc_getrequiredservice]

// --8<-- [start:using_utility_from_ioc_injected_parameters]
public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIoc(DynamoDBEvent dynamoDbEvent,
	IDynamoDbStreamBatchProcessor batchProcessor, IDynamoDbStreamRecordHandler recordHandler)
{
    var result = await batchProcessor.ProcessAsync(dynamoDbEvent, recordHandler);
    return result.BatchItemFailuresResponse;
}
// --8<-- [end:using_utility_from_ioc_injected_parameters]

// --8<-- [start:example_implementation_of_iserviceprovider]
internal class Services
{
	private static readonly Lazy<IServiceProvider> LazyInstance = new(Build);

	private static ServiceCollection _services;
	public static IServiceProvider Provider => LazyInstance.Value;

	public static IServiceProvider Init()
	{
		return LazyInstance.Value;
	}

	private static IServiceProvider Build()
	{
		_services = new ServiceCollection();
		_services.AddScoped<IDynamoDbStreamBatchProcessor, CustomDynamoDbStreamBatchProcessor>();
		_services.AddScoped<IDynamoDbStreamRecordHandler, CustomDynamoDbStreamRecordHandler>();
		return _services.BuildServiceProvider();
	}
}
// --8<-- [end:example_implementation_of_iserviceprovider]

// --8<-- [start:processing_messages_in_parallel]
[BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler), BatchParallelProcessingEnabled = true )]
public BatchItemFailuresResponse HandlerUsingAttribute(DynamoDBEvent _)
{
	return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:processing_messages_in_parallel]
