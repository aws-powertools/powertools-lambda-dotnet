

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Defines how deserialization errors should be handled during batch processing.
/// </summary>
public enum DeserializationErrorPolicy
{
    /// <summary>
    /// Mark the record as failed when deserialization fails (default behavior).
    /// The record will be included in the batch failure response.
    /// </summary>
    FailRecord,

    /// <summary>
    /// Skip records that fail deserialization and continue processing other records.
    /// Failed records will not be included in the batch failure response.
    /// </summary>
    IgnoreRecord,

    /// <summary>
    /// Use a custom error handler to process deserialization failures.
    /// The custom handler determines how to handle the failed record.
    /// </summary>
    CustomHandler
}