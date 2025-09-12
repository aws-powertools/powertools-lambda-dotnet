

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="IBatchProcessorProvider{TEvent,TRecord}"/> interface.
/// </summary>
/// <typeparam name="TEvent">Type of batch event.</typeparam>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public interface IBatchProcessorProvider<in TEvent, TRecord>
{
    /// <summary>
    /// Creates a batch processor.
    /// </summary>
    /// <returns>The created batch processor.</returns>
    IBatchProcessor<TEvent, TRecord> Create();
}
