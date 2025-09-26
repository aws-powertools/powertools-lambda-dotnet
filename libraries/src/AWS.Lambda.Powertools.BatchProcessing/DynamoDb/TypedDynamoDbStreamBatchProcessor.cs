

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Internal;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <summary>
/// Typed batch processor for DynamoDB stream events that supports automatic deserialization of record data.
/// </summary>
public class TypedDynamoDbStreamBatchProcessor : DynamoDbStreamBatchProcessor, ITypedBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>
{
    private readonly IDeserializationService _deserializationService;
    private readonly IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> _recordDataExtractor;


    /// <summary>
    /// Initializes a new instance of the TypedDynamoDbStreamBatchProcessor class.
    /// </summary>
    /// <param name="deserializationService">The deserialization service. If null, uses JsonDeserializationService.Instance.</param>
    /// <param name="recordDataExtractor">The record data extractor. If null, uses DynamoDbRecordDataExtractor.Instance.</param>
    public TypedDynamoDbStreamBatchProcessor(IDeserializationService deserializationService = null,
        IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> recordDataExtractor = null) 
    {
        _deserializationService = deserializationService ?? JsonDeserializationService.Instance;
        _recordDataExtractor = recordDataExtractor ?? DynamoDbRecordDataExtractor.Instance;
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandler<T> recordHandler)
    {
        return await ProcessAsync(@event, recordHandler, null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandler<T> recordHandler, 
        DeserializationOptions deserializationOptions)
    {
        return await ProcessAsync(@event, recordHandler, deserializationOptions, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandler<T> recordHandler, 
        CancellationToken cancellationToken)
    {
        return await ProcessAsync(@event, recordHandler, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandler<T> recordHandler, 
        DeserializationOptions deserializationOptions, 
        CancellationToken cancellationToken)
    {
        var processingOptions = new ProcessingOptions
        {
            CancellationToken = cancellationToken
        };
        return await ProcessAsync(@event, recordHandler, deserializationOptions, processingOptions);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandler<T> recordHandler, 
        DeserializationOptions deserializationOptions, 
        ProcessingOptions processingOptions)
    {
        // Validate AOT compatibility before processing
        AotCompatibilityHelper.ValidateAotCompatibility<T>(deserializationOptions);
        
        var wrappedHandler = new TypedRecordHandlerWrapper<T>(recordHandler, _deserializationService, _recordDataExtractor, deserializationOptions);
        return await ProcessAsync(@event, wrappedHandler, processingOptions);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context)
    {
        return await ProcessAsync(@event, recordHandler, context, null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context, 
        DeserializationOptions deserializationOptions)
    {
        return await ProcessAsync(@event, recordHandler, context, deserializationOptions, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context, 
        CancellationToken cancellationToken)
    {
        return await ProcessAsync(@event, recordHandler, context, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context, 
        DeserializationOptions deserializationOptions, 
        CancellationToken cancellationToken)
    {
        var processingOptions = new ProcessingOptions
        {
            CancellationToken = cancellationToken
        };
        return await ProcessAsync(@event, recordHandler, context, deserializationOptions, processingOptions);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync<T>(
        DynamoDBEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context, 
        DeserializationOptions deserializationOptions, 
        ProcessingOptions processingOptions)
    {
        // Validate AOT compatibility before processing
        AotCompatibilityHelper.ValidateAotCompatibility<T>(deserializationOptions);
        
        var wrappedHandler = new TypedRecordHandlerWithContextWrapper<T>(recordHandler, context, _deserializationService, _recordDataExtractor, deserializationOptions);
        return await ProcessAsync(@event, wrappedHandler, processingOptions);
    }

    /// <summary>
    /// Wrapper class that adapts ITypedRecordHandler to IRecordHandler.
    /// </summary>
    private sealed class TypedRecordHandlerWrapper<T> : TypedRecordHandlerWrapperBase<DynamoDBEvent.DynamodbStreamRecord, T>
    {
        private readonly ITypedRecordHandler<T> _typedHandler;

        public TypedRecordHandlerWrapper(
            ITypedRecordHandler<T> typedHandler,
            IDeserializationService deserializationService,
            IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> recordDataExtractor,
            DeserializationOptions deserializationOptions)
            : base(deserializationService, recordDataExtractor, deserializationOptions)
        {
            _typedHandler = typedHandler ?? throw new ArgumentNullException(nameof(typedHandler));
        }

        protected override async Task<RecordHandlerResult> HandleTypedRecordAsync(T deserializedData, CancellationToken cancellationToken)
        {
            return await _typedHandler.HandleAsync(deserializedData, cancellationToken);
        }

        protected override string GetDeserializationErrorMessage(DynamoDBEvent.DynamodbStreamRecord record, DeserializationException ex)
        {
            return $"Failed to deserialize DynamoDB stream record '{record.Dynamodb.SequenceNumber}' to type '{typeof(T).Name}'. See inner exception for details.";
        }
    }

    /// <summary>
    /// Wrapper class that adapts ITypedRecordHandlerWithContext to IRecordHandler.
    /// </summary>
    private sealed class TypedRecordHandlerWithContextWrapper<T> : TypedRecordHandlerWithContextWrapperBase<DynamoDBEvent.DynamodbStreamRecord, T>
    {
        private readonly ITypedRecordHandlerWithContext<T> _typedHandler;

        public TypedRecordHandlerWithContextWrapper(
            ITypedRecordHandlerWithContext<T> typedHandler,
            ILambdaContext context,
            IDeserializationService deserializationService,
            IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> recordDataExtractor,
            DeserializationOptions deserializationOptions)
            : base(context, deserializationService, recordDataExtractor, deserializationOptions)
        {
            _typedHandler = typedHandler ?? throw new ArgumentNullException(nameof(typedHandler));
        }

        protected override async Task<RecordHandlerResult> HandleTypedRecordWithContextAsync(T deserializedData, ILambdaContext context, CancellationToken cancellationToken)
        {
            return await _typedHandler.HandleAsync(deserializedData, context, cancellationToken);
        }

        protected override string GetDeserializationErrorMessage(DynamoDBEvent.DynamodbStreamRecord record, DeserializationException ex)
        {
            return $"Failed to deserialize DynamoDB stream record '{record.Dynamodb.SequenceNumber}' to type '{typeof(T).Name}'. See inner exception for details.";
        }
    }
}