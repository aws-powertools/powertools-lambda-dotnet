

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Represents a batch record that was successfully processed.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class RecordSuccess<TRecord>
{
    /// <summary>
    /// The result returned by the record handler processing the batch record.
    /// </summary>
    public RecordHandlerResult HandlerResult { get; init; }

    /// <summary>
    /// The batch record.
    /// </summary>
    public TRecord Record { get; init; }

    /// <summary>
    /// The batch record identifier.
    /// </summary>
    public string RecordId { get; init; }
}
