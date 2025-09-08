

using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="ITypedBatchProcessor{TEvent,TRecord}"/> interface for strongly-typed batch processing.
/// </summary>
/// <typeparam name="TEvent">Type of batch event.</typeparam>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public interface ITypedBatchProcessor<in TEvent, TRecord>
{
    /// <summary>
    /// The <see cref="ProcessingResult{TRecord}"/> of the latest batch processing run. This includes a <see cref="BatchItemFailuresResponse"/> object with the identifiers of the batch items that failed processing.
    /// </summary>
    ProcessingResult<TRecord> ProcessingResult { get; }

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandler<T> recordHandler);

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="deserializationOptions">Options for controlling deserialization behavior.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions);

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandler<T> recordHandler, CancellationToken cancellationToken);

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandler{T},DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="deserializationOptions">Options for controlling deserialization behavior.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a batch event with strongly-typed record handling.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="event">The event to process.</param>
    /// <param name="recordHandler">The typed record handler containing the per-record processing logic.</param>
    /// <param name="deserializationOptions">Options for controlling deserialization behavior.</param>
    /// <param name="processingOptions">Processing options to control settings such as cancellation, error handling policy and parallelism.</param>
    /// <returns>A <see cref="ProcessingResult{TRecord}"/> of the latest batch processing run. This includes a <see cref="BatchItemFailuresResponse"/> object with the identifiers of the batch items that failed processing.</returns>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions, ProcessingOptions processingOptions);

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="context"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context);

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="context"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="deserializationOptions">Options for controlling deserialization behavior.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions);

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="context"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, CancellationToken cancellationToken);

    /// <inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="context"><inheritdoc cref="ProcessAsync{T}(TEvent,ITypedRecordHandlerWithContext{T},ILambdaContext,DeserializationOptions,ProcessingOptions)"/></param>
    /// <param name="deserializationOptions">Options for controlling deserialization behavior.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a batch event with strongly-typed record handling and Lambda context.
    /// </summary>
    /// <typeparam name="T">The type to deserialize record data to.</typeparam>
    /// <param name="event">The event to process.</param>
    /// <param name="recordHandler">The typed record handler with context containing the per-record processing logic.</param>
    /// <param name="context">The Lambda context for the current invocation.</param>
    /// <param name="deserializationOptions">Options for controlling deserialization behavior.</param>
    /// <param name="processingOptions">Processing options to control settings such as cancellation, error handling policy and parallelism.</param>
    /// <returns>A <see cref="ProcessingResult{TRecord}"/> of the latest batch processing run. This includes a <see cref="BatchItemFailuresResponse"/> object with the identifiers of the batch items that failed processing.</returns>
    Task<ProcessingResult<TRecord>> ProcessAsync<T>(TEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions, ProcessingOptions processingOptions);
}