

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <summary>
/// Typed batch processor for DynamoDB stream events that supports automatic deserialization of record data.
/// </summary>
public class TypedDynamoDbStreamBatchProcessor : DynamoDbStreamBatchProcessor, ITypedBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>
{
    private readonly IDeserializationService _deserializationService;
    private readonly IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> _recordDataExtractor;

    /// <summary>
    /// The singleton instance of the typed DynamoDB stream batch processor.
    /// </summary>
    private static ITypedBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord> _typedInstance;

    /// <summary>
    /// Gets the typed instance.
    /// </summary>
    /// <value>The typed instance.</value>
    public static ITypedBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord> TypedInstance => 
        _typedInstance ??= new TypedDynamoDbStreamBatchProcessor(PowertoolsConfigurations.Instance);

    /// <summary>
    /// Initializes a new instance of the TypedDynamoDbStreamBatchProcessor class.
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools configurations.</param>
    /// <param name="deserializationService">The deserialization service. If null, uses JsonDeserializationService.Instance.</param>
    /// <param name="recordDataExtractor">The record data extractor. If null, uses DynamoDbRecordDataExtractor.Instance.</param>
    public TypedDynamoDbStreamBatchProcessor(
        IPowertoolsConfigurations powertoolsConfigurations,
        IDeserializationService deserializationService = null,
        IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> recordDataExtractor = null) 
        : base(powertoolsConfigurations)
    {
        _deserializationService = deserializationService ?? JsonDeserializationService.Instance;
        _recordDataExtractor = recordDataExtractor ?? DynamoDbRecordDataExtractor.Instance;
    }

    /// <summary>
    /// Default constructor for when consumers create a custom typed batch processor.
    /// </summary>
    protected TypedDynamoDbStreamBatchProcessor() : this(PowertoolsConfigurations.Instance)
    {
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
    private sealed class TypedRecordHandlerWrapper<T> : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>
    {
        private readonly ITypedRecordHandler<T> _typedHandler;
        private readonly IDeserializationService _deserializationService;
        private readonly IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> _recordDataExtractor;
        private readonly DeserializationOptions _deserializationOptions;

        public TypedRecordHandlerWrapper(
            ITypedRecordHandler<T> typedHandler,
            IDeserializationService deserializationService,
            IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> recordDataExtractor,
            DeserializationOptions deserializationOptions)
        {
            _typedHandler = typedHandler ?? throw new ArgumentNullException(nameof(typedHandler));
            _deserializationService = deserializationService ?? throw new ArgumentNullException(nameof(deserializationService));
            _recordDataExtractor = recordDataExtractor ?? throw new ArgumentNullException(nameof(recordDataExtractor));
            _deserializationOptions = deserializationOptions;
        }

        public async Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record, CancellationToken cancellationToken)
        {
            try
            {
                var recordData = _recordDataExtractor.ExtractData(record);
                
                // Use TryDeserialize to check if deserialization was successful
                if (_deserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord || 
                    _deserializationOptions?.IgnoreDeserializationErrors == true)
                {
                    if (!_deserializationService.TryDeserialize<T>(recordData, out var deserializedData, out _, _deserializationOptions))
                    {
                        // Deserialization failed and we're ignoring errors, don't call the handler
                        return RecordHandlerResult.None;
                    }
                    return await _typedHandler.HandleAsync(deserializedData, cancellationToken);
                }
                else
                {
                    // Use regular deserialize which will throw on errors
                    var deserializedData = _deserializationService.Deserialize<T>(recordData, _deserializationOptions);
                    return await _typedHandler.HandleAsync(deserializedData, cancellationToken);
                }
            }
            catch (DeserializationException ex)
            {
                // Handle deserialization errors based on policy
                if (_deserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord)
                {
                    return RecordHandlerResult.None;
                }
                
                // For FailRecord policy or default, re-throw the exception
                throw new RecordProcessingException($"Failed to deserialize DynamoDB stream record '{record.Dynamodb.SequenceNumber}' to type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }
    }

    /// <summary>
    /// Wrapper class that adapts ITypedRecordHandlerWithContext to IRecordHandler.
    /// </summary>
    private sealed class TypedRecordHandlerWithContextWrapper<T> : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>
    {
        private readonly ITypedRecordHandlerWithContext<T> _typedHandler;
        private readonly ILambdaContext _context;
        private readonly IDeserializationService _deserializationService;
        private readonly IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> _recordDataExtractor;
        private readonly DeserializationOptions _deserializationOptions;

        public TypedRecordHandlerWithContextWrapper(
            ITypedRecordHandlerWithContext<T> typedHandler,
            ILambdaContext context,
            IDeserializationService deserializationService,
            IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord> recordDataExtractor,
            DeserializationOptions deserializationOptions)
        {
            _typedHandler = typedHandler ?? throw new ArgumentNullException(nameof(typedHandler));
            _context = context; // Context can be null
            _deserializationService = deserializationService ?? throw new ArgumentNullException(nameof(deserializationService));
            _recordDataExtractor = recordDataExtractor ?? throw new ArgumentNullException(nameof(recordDataExtractor));
            _deserializationOptions = deserializationOptions;
        }

        public async Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record, CancellationToken cancellationToken)
        {
            try
            {
                var recordData = _recordDataExtractor.ExtractData(record);
                
                // Use TryDeserialize to check if deserialization was successful
                if (_deserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord || 
                    _deserializationOptions?.IgnoreDeserializationErrors == true)
                {
                    if (!_deserializationService.TryDeserialize<T>(recordData, out var deserializedData, out _, _deserializationOptions))
                    {
                        // Deserialization failed and we're ignoring errors, don't call the handler
                        return RecordHandlerResult.None;
                    }
                    return await _typedHandler.HandleAsync(deserializedData, _context, cancellationToken);
                }
                else
                {
                    // Use regular deserialize which will throw on errors
                    var deserializedData = _deserializationService.Deserialize<T>(recordData, _deserializationOptions);
                    return await _typedHandler.HandleAsync(deserializedData, _context, cancellationToken);
                }
            }
            catch (DeserializationException ex)
            {
                // Handle deserialization errors based on policy
                if (_deserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord)
                {
                    return RecordHandlerResult.None;
                }
                
                // For FailRecord policy or default, re-throw the exception
                throw new RecordProcessingException($"Failed to deserialize DynamoDB stream record '{record.Dynamodb.SequenceNumber}' to type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }
    }
}