

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Enum to specify the error handling policy applied during batch processing.
/// </summary>
public enum BatchProcessorErrorHandlingPolicy
{
    /// <summary>
    /// Auto-derive the policy based on the event.
    /// </summary>
    DeriveFromEvent,

    /// <summary>
    /// Continue processing regardless of whether other batch items fails during processing.
    /// </summary>
    ContinueOnBatchItemFailure,

    /// <summary>
    /// Stop processing other batch items after the first batch item has failed processing.
    /// This is useful to preserve ordered processing of events.
    /// <br/>
    /// When parallel processing is enabled, all batch items already scheduled to be processed,
    /// will be allowed to complete before the batch processing stops.
    /// <br/>
    /// Therefore, if order is important, it is recommended to use sequential (non-parallel) processing
    /// together with this value.
    /// </summary>
    StopOnFirstBatchItemFailure
}
