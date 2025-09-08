

using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="IRecordHandler{TRecord}"/> interface.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public interface IRecordHandler<in TRecord>
{
    /// <summary>
    /// Handles processing of a given batch record.
    /// </summary>
    /// <param name="record">The record to process.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
    Task<RecordHandlerResult> HandleAsync(TRecord record, CancellationToken cancellationToken);
}
