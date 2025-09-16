

using System.Collections.Generic;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Contains the result of the most recent batch processing run.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class ProcessingResult<TRecord>
{
    /// <summary>
    /// The batch item failures response, containing a list of partial batch item failures.
    /// This is the response to be returned from the Lambda function handler.
    /// </summary>
    public BatchItemFailuresResponse BatchItemFailuresResponse { get; } = new();

    /// <summary>
    /// The set of batch records from the batch event.
    /// </summary>
    public List<TRecord> BatchRecords { get; } = new();

    /// <summary>
    /// The set of batch records that were successfully processed.
    /// </summary>
    public List<RecordSuccess<TRecord>> SuccessRecords { get; } = new();

    /// <summary>
    /// The set of batch records that failed processing.
    /// </summary>
    public List<RecordFailure<TRecord>> FailureRecords { get; } = new();

    /// <summary>
    /// Clears the result object.
    /// </summary>
    public void Clear()
    {
        BatchRecords.Clear();
        SuccessRecords.Clear();
        FailureRecords.Clear();
    }
}