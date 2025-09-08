

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The result of the record handler processing the batch record.
/// </summary>
public class RecordHandlerResult
{
    /// <summary>
    /// Returns an empty <see cref="RecordHandlerResult"/> value.
    /// </summary>
    public static RecordHandlerResult None { get; } = null!;

    /// <summary>
    /// Convenience method for the creation of a <see cref="RecordHandlerResult"/>.
    /// </summary>
    /// <param name="data">The result of the record handler.</param>
    /// <returns>A <see cref="RecordHandlerResult"/> with the provided data.</returns>
    public static RecordHandlerResult FromData(object data)
    {
        return new RecordHandlerResult
        {
            Data = data
        };
    }

    /// <summary>
    /// The data returned from processing the batch record.
    /// </summary>
    public object Data { get; init; }
}
