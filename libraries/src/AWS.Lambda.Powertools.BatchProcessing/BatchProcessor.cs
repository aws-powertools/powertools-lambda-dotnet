using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

public abstract class BatchProcessor<TEvent, TRecord> : IBatchProcessor<TEvent, TRecord>
{
    public BatchResponse BatchResponse { get; } = new();

    public virtual async Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler)
    {
        // Clear item failures from any previous run
        BatchResponse.BatchItemFailures.Clear();

        // Prepare records. We assume all records fail by default to avoid loss of data.
        var allRecords = GetRecordsFromEvent(@event).ToDictionary(x => x, GetRecordId);
        var failedRecordIds = new HashSet<string>(allRecords.Values);

        // Get error handling policy
        var errorHandlingPolicy = GetErrorHandlingPolicyForEvent(@event);

        // Invoke hook
        await BeforeBatchProcessingAsync(@event);

        foreach (var (record, recordId) in allRecords)
        {
            try
            {
                // Process the record
                await HandleRecordAsync(record, recordHandler);

                // Register success
                failedRecordIds.Remove(recordId);

                // Invoke hook
                await HandleRecordSuccesssAsync(record);
            }
            catch (Exception ex)
            {
                // Invoke hook
                await HandleRecordFailureAsync(record, ex);

                // For some event sources, we should stop record processing on first error
                if (errorHandlingPolicy == BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)
                {
                    break;
                }
            }
        }

        // Invoke hook
        await AfterBatchProcessingAsync(@event, failedRecordIds);

        // Collect results
        BatchResponse.BatchItemFailures.AddRange(failedRecordIds.Select(x => new BatchResponse.BatchItemFailure
        {
            ItemIdentifier = x
        }));

        return BatchResponse;
    }

    protected virtual async Task BeforeBatchProcessingAsync(TEvent @event) => await Task.CompletedTask;

    protected virtual async Task HandleRecordAsync(TRecord record, IRecordHandler<TRecord> recordHandler)
    {
        await recordHandler.HandleAsync(record);
    }

    protected virtual async Task HandleRecordSuccesssAsync(TRecord record) => await Task.CompletedTask;

    protected virtual async Task HandleRecordFailureAsync(TRecord record, Exception exception) => await Task.CompletedTask;

    protected virtual async Task AfterBatchProcessingAsync(TEvent @event, HashSet<string> failedRecordIds) => await Task.CompletedTask;

    protected abstract BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(TEvent @event);

    protected abstract ICollection<TRecord> GetRecordsFromEvent(TEvent @event);

    protected abstract string GetRecordId(TRecord record);
}