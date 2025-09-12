

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="ITypedRecordHandlerProvider{T}"/> interface for creating strongly-typed record handlers.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
public interface ITypedRecordHandlerProvider<in T>
{
    /// <summary>
    /// Creates a typed record handler.
    /// </summary>
    /// <returns>The created typed record handler.</returns>
    ITypedRecordHandler<T> Create();
}