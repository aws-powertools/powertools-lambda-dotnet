

using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="IBatchProcessor{TEvent,TRecord}"/> interface.
/// </summary>
/// <typeparam name="TEvent">Type of batch event.</typeparam>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public interface IBatchProcessor<in TEvent, TRecord>
{
    /// <summary>
    /// The <see cref="ProcessingResult{TRecord}"/> of the latest batch processing run. This includes a <see cref="BatchItemFailuresResponse"/> object with the identifiers of the batch items that failed processing.
    /// </summary>
    ProcessingResult<TRecord> ProcessingResult { get; }

    /// <inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler);

    /// <inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a batch event.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="recordHandler">The record handler containing the per-record processing logic.</param>
    /// <param name="processingOptions">Processing options to control settings such as cancellation, error handling policy and parallelism.</param>
    /// <returns>A <see cref="ProcessingResult{TRecord}"/> of the latest batch processing run. This includes a <see cref="BatchItemFailuresResponse"/> object with the identifiers of the batch items that failed processing.</returns>
    Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, ProcessingOptions processingOptions);
}
