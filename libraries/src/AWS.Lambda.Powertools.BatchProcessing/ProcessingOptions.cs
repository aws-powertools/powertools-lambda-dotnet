

using System;
using System.Threading;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Batch processing options to control settings such as cancellation, error handling policy and parallelism.
/// </summary>
public class ProcessingOptions
{
    /// <summary>
    /// The cancellation token to monitor.
    /// </summary>
    public CancellationToken? CancellationToken { get; init; }

    /// <summary>
    /// The maximum degree of parallelism to apply during batch processing if <see cref="BatchParallelProcessingEnabled"/> is enabled (default is -1, which means <see cref="Environment.ProcessorCount"/>).
    /// </summary>
    public int? MaxDegreeOfParallelism { get; init; }

    /// <summary>
    /// The error handling policy to apply during batch processing (default is <see cref="BatchProcessorErrorHandlingPolicy.DeriveFromEvent"/>).
    /// </summary>
    public BatchProcessorErrorHandlingPolicy? ErrorHandlingPolicy { get; init; }

    /// <summary>
    /// Controls whether parallel batch processing is enabled (default false).
    /// </summary>
    public bool BatchParallelProcessingEnabled { get; init; } = false;

    /// <summary>
    /// Controls whether the Batch processor throws a <see cref="BatchProcessingException"/> on full batch failure (default true).
    /// </summary>
    public bool ThrowOnFullBatchFailure { get; init; } = true;
}
