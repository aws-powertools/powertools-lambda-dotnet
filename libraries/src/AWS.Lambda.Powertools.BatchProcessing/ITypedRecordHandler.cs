

using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="ITypedRecordHandler{T}"/> interface for strongly-typed record handling.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
public interface ITypedRecordHandler<in T>
{
    /// <summary>
    /// Handles processing of a given batch record with strongly-typed data.
    /// </summary>
    /// <param name="data">The deserialized data from the record to process.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
    Task<RecordHandlerResult> HandleAsync(T data, CancellationToken cancellationToken);
}