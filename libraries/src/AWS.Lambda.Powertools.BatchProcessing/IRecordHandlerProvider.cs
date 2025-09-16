

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="IRecordHandlerProvider{TRecord}"/> interface.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public interface IRecordHandlerProvider<in TRecord>
{
    /// <summary>
    /// Creates a record handler.
    /// </summary>
    /// <returns>The created record handler.</returns>
    IRecordHandler<TRecord> Create();
}
