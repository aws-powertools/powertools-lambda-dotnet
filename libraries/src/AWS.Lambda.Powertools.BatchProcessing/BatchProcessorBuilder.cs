

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.BatchProcessing.Internal;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Wrapper class to convert ITypedRecordHandler to ITypedRecordHandlerWithContext.
/// </summary>
/// <typeparam name="T">The type to deserialize record data to.</typeparam>
internal class TypedRecordHandlerWrapper<T> : ITypedRecordHandlerWithContext<T>
{
    private readonly ITypedRecordHandler<T> _handler;

    public TypedRecordHandlerWrapper(ITypedRecordHandler<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public Task<RecordHandlerResult> HandleAsync(T data, ILambdaContext context, CancellationToken cancellationToken)
    {
        // Ignore the context and call the handler without it
        return _handler.HandleAsync(data, cancellationToken);
    }
}

/// <summary>
/// Fluent API builder for configuring and executing batch processing with strongly-typed record handlers.
/// </summary>
/// <typeparam name="TEvent">Type of batch event.</typeparam>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class BatchProcessorBuilder<TEvent, TRecord>
    where TEvent : class
{
    private readonly ITypedBatchProcessor<TEvent, TRecord> _batchProcessor;
    private readonly DeserializationOptions _deserializationOptions;
    private ProcessingOptions _processingOptions;
    private readonly Dictionary<Type, Func<TRecord, ILambdaContext, CancellationToken, Task<RecordHandlerResult>>> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessorBuilder{TEvent, TRecord}"/> class.
    /// </summary>
    /// <param name="batchProcessor">The underlying batch processor to use for processing.</param>
    public BatchProcessorBuilder(ITypedBatchProcessor<TEvent, TRecord> batchProcessor)
    {
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        _deserializationOptions = new DeserializationOptions();
        _processingOptions = new ProcessingOptions();
        _handlers = new Dictionary<Type, Func<TRecord, ILambdaContext, CancellationToken, Task<RecordHandlerResult>>>();
    }

    /// <summary>
    /// Configures the JsonSerializerContext for AOT-compatible deserialization.
    /// </summary>
    /// <param name="context">The JsonSerializerContext to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> WithJsonSerializerContext(JsonSerializerContext context)
    {
        _deserializationOptions.JsonSerializerContext = context;
        return this;
    }

    /// <summary>
    /// Configures the JsonSerializerOptions for deserialization.
    /// </summary>
    /// <param name="options">The JsonSerializerOptions to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> WithJsonSerializerOptions(JsonSerializerOptions options)
    {
        _deserializationOptions.JsonSerializerOptions = options;
        return this;
    }

    /// <summary>
    /// Configures the deserialization error policy.
    /// </summary>
    /// <param name="errorPolicy">The error policy to use when deserialization fails.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> WithDeserializationErrorPolicy(DeserializationErrorPolicy errorPolicy)
    {
        _deserializationOptions.ErrorPolicy = errorPolicy;
        return this;
    }

    /// <summary>
    /// Configures processing options for batch processing.
    /// </summary>
    /// <param name="processingOptions">The processing options to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> WithProcessingOptions(ProcessingOptions processingOptions)
    {
        _processingOptions = processingOptions ?? throw new ArgumentNullException(nameof(processingOptions));
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using the TypedRecordHandler delegate.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler delegate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(TypedRecordHandler<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using the TypedRecordHandlerWithContext delegate.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler delegate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(TypedRecordHandlerWithContext<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using the SimpleTypedRecordHandler delegate.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler delegate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(SimpleTypedRecordHandler<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using the SimpleTypedRecordHandlerWithContext delegate.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler delegate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(SimpleTypedRecordHandlerWithContext<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using any delegate with automatic context injection.
    /// The method signature will be analyzed to determine if Lambda context is required.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler delegate with any supported signature.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(Delegate handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using a Func delegate without cancellation token.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler function.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(Func<T, Task<RecordHandlerResult>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using a Func delegate with cancellation token.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler function.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(Func<T, CancellationToken, Task<RecordHandlerResult>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using a Func delegate with Lambda context.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler function.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(Func<T, ILambdaContext, Task<RecordHandlerResult>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Registers a typed record handler using a Func delegate with Lambda context and cancellation token.
    /// Context injection will be handled automatically based on method signature.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="handler">The handler function.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public BatchProcessorBuilder<TEvent, TRecord> Handler<T>(Func<T, ILambdaContext, CancellationToken, Task<RecordHandlerResult>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        // Validate handler signature and store for later use
        ContextInjectionHelper.ValidateHandlerSignature<T>(handler);
        _handlers[typeof(T)] = async (record, context, cancellationToken) =>
        {
            return await ContextInjectionHelper.InvokeWithContextInjection(handler, record, context, cancellationToken);
        };
        
        return this;
    }

    /// <summary>
    /// Processes the batch event using the configured handlers and options.
    /// Note: This is a simplified implementation for the builder pattern. 
    /// The actual implementation would need to handle multiple handler types and routing.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="event">The batch event to process.</param>
    /// <param name="handler">The specific handler to use for this processing run.</param>
    /// <param name="context">The Lambda context (optional).</param>
    /// <returns>The processing result.</returns>
    public Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandler<T> handler, ILambdaContext context = null)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // Validate AOT compatibility before processing
        AotCompatibilityHelper.ValidateAotCompatibility<T>(_deserializationOptions);

        if (context != null)
        {
            // Create a wrapper that implements ITypedRecordHandlerWithContext
            var contextHandler = new TypedRecordHandlerWrapper<T>(handler);
            return _batchProcessor.ProcessAsync(@event, contextHandler, context, _deserializationOptions, _processingOptions);
        }
        
        return _batchProcessor.ProcessAsync(@event, handler, _deserializationOptions, _processingOptions);
    }

    /// <summary>
    /// Processes the batch event using the configured handlers and options with context.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="event">The batch event to process.</param>
    /// <param name="handler">The specific handler to use for this processing run.</param>
    /// <param name="context">The Lambda context.</param>
    /// <returns>The processing result.</returns>
    public Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandlerWithContext<T> handler, ILambdaContext context)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (context == null) throw new ArgumentNullException(nameof(context));

        // Validate AOT compatibility before processing
        AotCompatibilityHelper.ValidateAotCompatibility<T>(_deserializationOptions);

        return _batchProcessor.ProcessAsync(@event, handler, context, _deserializationOptions, _processingOptions);
    }
}