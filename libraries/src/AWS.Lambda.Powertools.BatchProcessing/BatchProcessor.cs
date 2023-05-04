using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;

namespace AWS.Lambda.Powertools.BatchProcessing;

public abstract class BatchProcessor<TEvent, TRecord> : IBatchProcessor<TEvent, TRecord>
{
    public BatchResponse BatchResponse { get; } = new();

    public virtual async Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler)
    {
        // Clear item failures from any previous run
        BatchResponse.BatchItemFailures.Clear();

        // Prepare records. We assume all records fail by default to avoid loss of data.
        var batchRecords = GetRecordsFromEvent(@event).ToDictionary(GetRecordId, x => x);
        var failedRecords = batchRecords.ToDictionary(x => x.Key, _ => (Exception) null);

        // Get error handling policy
        var errorHandlingPolicy = GetErrorHandlingPolicyForEvent(@event);

        // Invoke hook
        await BeforeBatchProcessingAsync(@event, batchRecords);

        foreach (var (recordId, record) in batchRecords)
        {
            try
            {
                // Process the record
                await HandleRecordAsync(record, recordHandler);

                // Register success
                failedRecords.Remove(recordId);

                try
                {
                    // Invoke hook
                    await HandleRecordSuccesssAsync(record);
                }
                catch
                {
                    // NOOP
                }
            }
            catch (Exception ex)
            {
                // Capture exception
                failedRecords[recordId] = new HandleRecordException($"Failed processing record: '{recordId}'. See inner exception for details.", ex);

                try
                {
                    // Invoke hook
                    await HandleRecordFailureAsync(record, ex);
                }
                catch
                {
                    // NOOP
                }

                // Check if we should stop record processing on first error
                if (errorHandlingPolicy == BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)
                {
                    var cbException = new CircuitBreakerException($"A previous record: '{recordId}' failed processing.");
                    foreach (var failedRecordId in failedRecords.Keys)
                    {
                        failedRecords[failedRecordId] ??= cbException;
                    }
                    break;
                }
            }
        }

        // Invoke hook
        await AfterBatchProcessingAsync(@event, batchRecords, failedRecords);

        // Collect results
        BatchResponse.BatchItemFailures.AddRange(failedRecords.Select(x => new BatchResponse.BatchItemFailure
        {
            ItemIdentifier = x.Key
        }));

        return BatchResponse;
    }

    protected virtual async Task BeforeBatchProcessingAsync(TEvent @event, IReadOnlyDictionary<string, TRecord> batchRecords) => await Task.CompletedTask;

    protected virtual async Task HandleRecordAsync(TRecord record, IRecordHandler<TRecord> recordHandler)
    {
        await recordHandler.HandleAsync(record);
    }

    protected virtual async Task HandleRecordSuccesssAsync(TRecord record) => await Task.CompletedTask;

    /*
     * TODO: Should we log this by default? In that case, can we take a dependency on the Powertools logger?
     * Example: https://github.com/awslabs/aws-lambda-powertools-python/blob/develop/aws_lambda_powertools/utilities/batch/base.py#L198
     */
    protected virtual async Task HandleRecordFailureAsync(TRecord record, Exception exception) => await Task.CompletedTask;

    protected virtual async Task AfterBatchProcessingAsync(TEvent @event, IReadOnlyDictionary<string, TRecord> batchRecords, IReadOnlyDictionary<string, Exception> failedRecords)
    {
        if (batchRecords.Count == failedRecords.Count)
        {
            throw new BatchProcessingException($"Entire batch of '{batchRecords.Count}' record(s) failed processing. See inner exceptions for details.", failedRecords.Values);
        }
        await Task.CompletedTask;
    }

    protected abstract BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(TEvent @event);

    protected abstract ICollection<TRecord> GetRecordsFromEvent(TEvent @event);

    protected abstract string GetRecordId(TRecord record);
}