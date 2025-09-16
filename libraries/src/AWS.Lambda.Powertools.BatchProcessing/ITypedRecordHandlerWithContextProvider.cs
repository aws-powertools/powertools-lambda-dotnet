

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="ITypedRecordHandlerWithContextProvider{T}"/> interface for creating strongly-typed record handlers with Lambda context.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
public interface ITypedRecordHandlerWithContextProvider<in T>
{
    /// <summary>
    /// Creates a typed record handler with context.
    /// </summary>
    /// <returns>The created typed record handler with context.</returns>
    ITypedRecordHandlerWithContext<T> Create();
}