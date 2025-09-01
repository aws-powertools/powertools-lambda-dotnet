/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// Typed batch processor for SQS events that supports automatic deserialization of message bodies.
/// </summary>
public class TypedSqsBatchProcessor : SqsBatchProcessor, ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage>
{
    private readonly IDeserializationService _deserializationService;
    private readonly IRecordDataExtractor<SQSEvent.SQSMessage> _recordDataExtractor;

    /// <summary>
    /// The singleton instance of the typed SQS batch processor.
    /// </summary>
    private static ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage> _typedInstance;

    /// <summary>
    /// Gets the typed instance.
    /// </summary>
    /// <value>The typed instance.</value>
    public static ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage> TypedInstance => 
        _typedInstance ??= new TypedSqsBatchProcessor(PowertoolsConfigurations.Instance);

    /// <summary>
    /// Initializes a new instance of the TypedSqsBatchProcessor class.
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools configurations.</param>
    /// <param name="deserializationService">The deserialization service. If null, uses JsonDeserializationService.Instance.</param>
    /// <param name="recordDataExtractor">The record data extractor. If null, uses SqsRecordDataExtractor.Instance.</param>
    public TypedSqsBatchProcessor(
        IPowertoolsConfigurations powertoolsConfigurations,
        IDeserializationService deserializationService = null,
        IRecordDataExtractor<SQSEvent.SQSMessage> recordDataExtractor = null) 
        : base(powertoolsConfigurations)
    {
        _deserializationService = deserializationService ?? JsonDeserializationService.Instance;
        _recordDataExtractor = recordDataExtractor ?? SqsRecordDataExtractor.Instance;
        _typedInstance = this;
    }

    /// <summary>
    /// Default constructor for when consumers create a custom typed batch processor.
    /// </summary>
    protected TypedSqsBatchProcessor() : this(PowertoolsConfigurations.Instance)
    {
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
        ITypedRecordHandler<T> recordHandler)
    {
        return await ProcessAsync(@event, recordHandler, null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
        ITypedRecordHandler<T> recordHandler, 
        DeserializationOptions deserializationOptions)
    {
        return await ProcessAsync(@event, recordHandler, deserializationOptions, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
        ITypedRecordHandler<T> recordHandler, 
        CancellationToken cancellationToken)
    {
        return await ProcessAsync(@event, recordHandler, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
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
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
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
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context)
    {
        return await ProcessAsync(@event, recordHandler, context, null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context, 
        DeserializationOptions deserializationOptions)
    {
        return await ProcessAsync(@event, recordHandler, context, deserializationOptions, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
        ITypedRecordHandlerWithContext<T> recordHandler, 
        ILambdaContext context, 
        CancellationToken cancellationToken)
    {
        return await ProcessAsync(@event, recordHandler, context, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
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
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event, 
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
    /// Processes a batch event using a delegate with automatic context injection.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="event">The SQS event to process.</param>
    /// <param name="handler">The handler delegate (any supported signature).</param>
    /// <param name="context">The Lambda context (optional).</param>
    /// <param name="deserializationOptions">Options for controlling deserialization behavior.</param>
    /// <param name="processingOptions">Processing options to control settings such as cancellation, error handling policy and parallelism.</param>
    /// <returns>The processing result.</returns>
    public async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(
        SQSEvent @event,
        Delegate handler,
        ILambdaContext context = null,
        DeserializationOptions deserializationOptions = null,
        ProcessingOptions processingOptions = null)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate AOT compatibility before processing
        AotCompatibilityHelper.ValidateAotCompatibility<T>(deserializationOptions);
        
        var wrappedHandler = new DelegateRecordHandlerWrapper<T>(handler, context, _deserializationService, _recordDataExtractor, deserializationOptions);
        return await ProcessAsync(@event, wrappedHandler, processingOptions ?? new ProcessingOptions());
    }

    /// <summary>
    /// Wrapper class that adapts ITypedRecordHandler to IRecordHandler.
    /// </summary>
    private class TypedRecordHandlerWrapper<T> : IRecordHandler<SQSEvent.SQSMessage>
    {
        private readonly ITypedRecordHandler<T> _typedHandler;
        private readonly IDeserializationService _deserializationService;
        private readonly IRecordDataExtractor<SQSEvent.SQSMessage> _recordDataExtractor;
        private readonly DeserializationOptions _deserializationOptions;

        public TypedRecordHandlerWrapper(
            ITypedRecordHandler<T> typedHandler,
            IDeserializationService deserializationService,
            IRecordDataExtractor<SQSEvent.SQSMessage> recordDataExtractor,
            DeserializationOptions deserializationOptions)
        {
            _typedHandler = typedHandler ?? throw new ArgumentNullException(nameof(typedHandler));
            _deserializationService = deserializationService ?? throw new ArgumentNullException(nameof(deserializationService));
            _recordDataExtractor = recordDataExtractor ?? throw new ArgumentNullException(nameof(recordDataExtractor));
            _deserializationOptions = deserializationOptions;
        }

        public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            try
            {
                var recordData = _recordDataExtractor.ExtractData(record);
                
                // Use TryDeserialize to check if deserialization was successful
                if (_deserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord || 
                    _deserializationOptions?.IgnoreDeserializationErrors == true)
                {
                    if (!_deserializationService.TryDeserialize<T>(recordData, out var deserializedData, out var exception, _deserializationOptions))
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
                throw new RecordProcessingException($"Failed to deserialize SQS message '{record.MessageId}' to type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }
    }

    /// <summary>
    /// Enhanced wrapper class that can adapt any delegate to IRecordHandler with automatic context injection.
    /// </summary>
    private class DelegateRecordHandlerWrapper<T> : IRecordHandler<SQSEvent.SQSMessage>
    {
        private readonly Delegate _handler;
        private readonly ILambdaContext _context;
        private readonly IDeserializationService _deserializationService;
        private readonly IRecordDataExtractor<SQSEvent.SQSMessage> _recordDataExtractor;
        private readonly DeserializationOptions _deserializationOptions;

        public DelegateRecordHandlerWrapper(
            Delegate handler,
            ILambdaContext context,
            IDeserializationService deserializationService,
            IRecordDataExtractor<SQSEvent.SQSMessage> recordDataExtractor,
            DeserializationOptions deserializationOptions)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _context = context; // Context can be null
            _deserializationService = deserializationService ?? throw new ArgumentNullException(nameof(deserializationService));
            _recordDataExtractor = recordDataExtractor ?? throw new ArgumentNullException(nameof(recordDataExtractor));
            _deserializationOptions = deserializationOptions;
            
            // Validate handler signature at construction time
            ContextInjectionHelper.ValidateHandlerSignature<T>(_handler);
        }

        public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            try
            {
                var recordData = _recordDataExtractor.ExtractData(record);
                var deserializedData = _deserializationService.Deserialize<T>(recordData, _deserializationOptions);
                
                // Use context injection helper to invoke the handler with appropriate parameters
                return await ContextInjectionHelper.InvokeWithContextInjection(_handler, deserializedData, _context, cancellationToken);
            }
            catch (DeserializationException ex)
            {
                // Handle deserialization errors based on policy
                if (_deserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord)
                {
                    return RecordHandlerResult.None;
                }
                
                // For FailRecord policy or default, re-throw the exception
                throw new RecordProcessingException($"Failed to deserialize SQS message '{record.MessageId}' to type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
            catch (Exception ex) when (ex is not DeserializationException)
            {
                // Handle other exceptions that might occur during handler execution
                throw new RecordProcessingException($"Failed to process SQS message '{record.MessageId}' with type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }
    }

    /// <summary>
    /// Wrapper class that adapts ITypedRecordHandlerWithContext to IRecordHandler.
    /// </summary>
    private class TypedRecordHandlerWithContextWrapper<T> : IRecordHandler<SQSEvent.SQSMessage>
    {
        private readonly ITypedRecordHandlerWithContext<T> _typedHandler;
        private readonly ILambdaContext _context;
        private readonly IDeserializationService _deserializationService;
        private readonly IRecordDataExtractor<SQSEvent.SQSMessage> _recordDataExtractor;
        private readonly DeserializationOptions _deserializationOptions;

        public TypedRecordHandlerWithContextWrapper(
            ITypedRecordHandlerWithContext<T> typedHandler,
            ILambdaContext context,
            IDeserializationService deserializationService,
            IRecordDataExtractor<SQSEvent.SQSMessage> recordDataExtractor,
            DeserializationOptions deserializationOptions)
        {
            _typedHandler = typedHandler ?? throw new ArgumentNullException(nameof(typedHandler));
            _context = context; // Context can be null
            _deserializationService = deserializationService ?? throw new ArgumentNullException(nameof(deserializationService));
            _recordDataExtractor = recordDataExtractor ?? throw new ArgumentNullException(nameof(recordDataExtractor));
            _deserializationOptions = deserializationOptions;
        }

        public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            try
            {
                var recordData = _recordDataExtractor.ExtractData(record);
                
                // Use TryDeserialize to check if deserialization was successful
                if (_deserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord || 
                    _deserializationOptions?.IgnoreDeserializationErrors == true)
                {
                    if (!_deserializationService.TryDeserialize<T>(recordData, out var deserializedData, out var exception, _deserializationOptions))
                    {
                        // Deserialization failed and we're ignoring errors, don't call the handler
                        return RecordHandlerResult.None;
                    }
                    // Handle null context gracefully - pass null if context is not available
                    return await _typedHandler.HandleAsync(deserializedData, _context, cancellationToken);
                }
                else
                {
                    // Use regular deserialize which will throw on errors
                    var deserializedData = _deserializationService.Deserialize<T>(recordData, _deserializationOptions);
                    // Handle null context gracefully - pass null if context is not available
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
                throw new RecordProcessingException($"Failed to deserialize SQS message '{record.MessageId}' to type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
            catch (Exception ex) when (ex is not DeserializationException)
            {
                // Handle other exceptions that might occur during handler execution
                throw new RecordProcessingException($"Failed to process SQS message '{record.MessageId}' with type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }
    }
}