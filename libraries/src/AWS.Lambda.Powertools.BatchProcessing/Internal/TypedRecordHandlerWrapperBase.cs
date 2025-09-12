using System;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

/// <summary>
/// Base wrapper class that adapts ITypedRecordHandler to IRecordHandler with common deserialization logic.
/// </summary>
/// <typeparam name="TRecord">The type of the record being processed.</typeparam>
/// <typeparam name="T">The type to deserialize the record data to.</typeparam>
internal abstract class TypedRecordHandlerWrapperBase<TRecord, T> : IRecordHandler<TRecord>
{
    protected readonly IDeserializationService DeserializationService;
    protected readonly IRecordDataExtractor<TRecord> RecordDataExtractor;
    protected readonly DeserializationOptions DeserializationOptions;

    protected TypedRecordHandlerWrapperBase(
        IDeserializationService deserializationService,
        IRecordDataExtractor<TRecord> recordDataExtractor,
        DeserializationOptions deserializationOptions)
    {
        DeserializationService = deserializationService ?? throw new ArgumentNullException(nameof(deserializationService));
        RecordDataExtractor = recordDataExtractor ?? throw new ArgumentNullException(nameof(recordDataExtractor));
        DeserializationOptions = deserializationOptions;
    }

    public async Task<RecordHandlerResult> HandleAsync(TRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var recordData = RecordDataExtractor.ExtractData(record);
            
            // Use TryDeserialize to check if deserialization was successful
            if (DeserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord || 
                DeserializationOptions?.IgnoreDeserializationErrors == true)
            {
                if (!DeserializationService.TryDeserialize<T>(recordData, out var deserializedData, out _, DeserializationOptions))
                {
                    // Deserialization failed and we're ignoring errors, don't call the handler
                    return RecordHandlerResult.None;
                }
                return await HandleTypedRecordAsync(deserializedData, cancellationToken);
            }
            else
            {
                // Use regular deserialize which will throw on errors
                var deserializedData = DeserializationService.Deserialize<T>(recordData, DeserializationOptions);
                return await HandleTypedRecordAsync(deserializedData, cancellationToken);
            }
        }
        catch (DeserializationException ex)
        {
            // Handle deserialization errors based on policy
            if (DeserializationOptions?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord)
            {
                return RecordHandlerResult.None;
            }
            
            // For FailRecord policy or default, re-throw the exception
            throw new RecordProcessingException(GetDeserializationErrorMessage(record, ex), ex);
        }
    }

    /// <summary>
    /// Handles the typed record after successful deserialization.
    /// </summary>
    /// <param name="deserializedData">The deserialized data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of handling the record.</returns>
    protected abstract Task<RecordHandlerResult> HandleTypedRecordAsync(T deserializedData, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the error message for deserialization failures.
    /// </summary>
    /// <param name="record">The record that failed to deserialize.</param>
    /// <param name="ex">The deserialization exception.</param>
    /// <returns>The error message.</returns>
    protected abstract string GetDeserializationErrorMessage(TRecord record, DeserializationException ex);
}