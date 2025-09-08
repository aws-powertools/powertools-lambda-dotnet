

using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Delegate for handling strongly-typed record data without Lambda context.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
/// <param name="data">The deserialized data from the record to process.</param>
/// <param name="cancellationToken">The cancellation token to monitor.</param>
/// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
public delegate Task<RecordHandlerResult> TypedRecordHandler<in T>(T data, CancellationToken cancellationToken);

/// <summary>
/// Delegate for handling strongly-typed record data with Lambda context.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
/// <param name="data">The deserialized data from the record to process.</param>
/// <param name="context">The Lambda context for the current invocation.</param>
/// <param name="cancellationToken">The cancellation token to monitor.</param>
/// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
public delegate Task<RecordHandlerResult> TypedRecordHandlerWithContext<in T>(T data, ILambdaContext context, CancellationToken cancellationToken);

/// <summary>
/// Simplified delegate for handling strongly-typed record data without cancellation token.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
/// <param name="data">The deserialized data from the record to process.</param>
/// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
public delegate Task<RecordHandlerResult> SimpleTypedRecordHandler<in T>(T data);

/// <summary>
/// Simplified delegate for handling strongly-typed record data with Lambda context but without cancellation token.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
/// <param name="data">The deserialized data from the record to process.</param>
/// <param name="context">The Lambda context for the current invocation.</param>
/// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
public delegate Task<RecordHandlerResult> SimpleTypedRecordHandlerWithContext<in T>(T data, ILambdaContext context);