

using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="ITypedRecordHandlerWithContext{T}"/> interface for strongly-typed record handling with Lambda context.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
public interface ITypedRecordHandlerWithContext<in T>
{
    /// <summary>
    /// Handles processing of a given batch record with strongly-typed data and Lambda context.
    /// </summary>
    /// <param name="data">The deserialized data from the record to process.</param>
    /// <param name="context">The Lambda context for the current invocation.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
    Task<RecordHandlerResult> HandleAsync(T data, ILambdaContext context, CancellationToken cancellationToken);
}