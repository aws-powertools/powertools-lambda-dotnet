using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;

namespace AWS.Lambda.Powertools.BatchProcessing;

public abstract class BatchProcessor<TEvent, TRecord> : IBatchProcessor<TEvent, TRecord>
{
    public BatchResponse BatchResponse { get; } = new();

    public async Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler)
    {
        return await ProcessAsync(@event, recordHandler, CancellationToken.None);
    }

    public async Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, CancellationToken cancellationToken)
    {
        return await ProcessAsync(@event, recordHandler, new ProcessingOptions
        {
            CancellationToken = cancellationToken
        });
    }

    public virtual async Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, ProcessingOptions processingOptions)
    {
        // Clear item failures from any previous run
        BatchResponse.BatchItemFailures.Clear();

        // Prepare records. We assume all records fail by default to avoid loss of data.
        var batchRecords = GetRecordsFromEvent(@event).ToDictionary(GetRecordId, x => x);
        var failedRecords = new ConcurrentDictionary<string, Exception>(batchRecords.ToDictionary(x => x.Key, x => (Exception) new UnprocessedRecordException($"Record: '{x.Key}' has not been processed.")));

        // Get error handling policy
        var errorHandlingPolicy = processingOptions?.ErrorHandlingPolicy is null or BatchProcessorErrorHandlingPolicy.DeriveFromEvent
            ? GetErrorHandlingPolicyForEvent(@event)
            : processingOptions.ErrorHandlingPolicy;

        // Invoke hook
        await BeforeBatchProcessingAsync(@event, batchRecords);

        try
        {
            // Invoke processing
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = processingOptions?.CancellationToken ?? CancellationToken.None,
                MaxDegreeOfParallelism = processingOptions?.MaxDegreeOfParallelism ?? 1
            };
            await Parallel.ForEachAsync(batchRecords, parallelOptions, async (pair, cancellationToken) =>
            {
                var (recordId, record) = pair;

                try
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    // Process the record
                    await HandleRecordAsync(record, recordHandler, cancellationToken);

                    // Register success
                    failedRecords.TryRemove(recordId, out _);

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
                    failedRecords[recordId] = new RecordProcessingException($"Failed processing record: '{recordId}'. See inner exception for details.", ex);

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
                        // This causes the loop's (inner) cancellation token to be cancelled for all operations already scheduled internally
                        throw new CircuitBreakerException("Error handling policy is configured to stop processing on first batch item failure. See inner exception for details.", ex);
                    }
                }
            });
        }
        catch (Exception ex) when (ex is CircuitBreakerException or OperationCanceledException)
        {
            // NOOP
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

    protected virtual async Task BeforeBatchProcessingAsync(
        TEvent @event,
        IReadOnlyDictionary<string, TRecord> batchRecords)
    {
        await Task.CompletedTask;
    }

    protected virtual async Task HandleRecordAsync(
        TRecord record,
        IRecordHandler<TRecord> recordHandler,
        CancellationToken cancellationToken)
    {
        await recordHandler.HandleAsync(record, cancellationToken);
    }

    protected virtual async Task HandleRecordSuccesssAsync(
        TRecord record)
    {
        await Task.CompletedTask;
    }

    protected virtual async Task HandleRecordFailureAsync(
        TRecord record,
        Exception exception)
    {
        await Task.CompletedTask;
    }

    protected virtual async Task AfterBatchProcessingAsync(
        TEvent @event,
        IReadOnlyDictionary<string, TRecord> batchRecords,
        IReadOnlyDictionary<string, Exception> failedRecords)
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