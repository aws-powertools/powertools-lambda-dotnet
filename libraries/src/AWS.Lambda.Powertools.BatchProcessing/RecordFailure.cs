

using System;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Represents a batch record that failed processing.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class RecordFailure<TRecord>
{
    /// <summary>
    /// The exception causing the failure.
    /// </summary>
    public Exception Exception { get; init; }

    /// <summary>
    /// The batch record.
    /// </summary>
    public TRecord Record { get; init; }

    /// <summary>
    /// The batch record identifier.
    /// </summary>
    public string RecordId { get; init; }
}
