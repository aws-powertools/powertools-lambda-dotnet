/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// An abstract class that implements common batch processing logic.
/// </summary>
/// <typeparam name="TEvent">Type of batch event.</typeparam>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public abstract class BatchProcessor<TEvent, TRecord> : IBatchProcessor<TEvent, TRecord>
{
    /// <inheritdoc />
    public ProcessingResult<TRecord> ProcessingResult { get; } = new();

    /// <inheritdoc />
    public async Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler)
    {
        return await ProcessAsync(@event, recordHandler, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, CancellationToken cancellationToken)
    {
        return await ProcessAsync(@event, recordHandler, new ProcessingOptions
        {
            CancellationToken = cancellationToken
        });
    }

    /// <inheritdoc />
    public virtual async Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, ProcessingOptions processingOptions)
    {
        // Clear result from any previous run
        ProcessingResult.Clear();

        // Prepare batch records (order is preserved)
        var batchRecords = GetRecordsFromEvent(@event).Select(x => new KeyValuePair<string, TRecord>(GetRecordId(x), x)).ToArray();

        // We assume all records fail by default to avoid loss of data
        var successRecords = new ConcurrentDictionary<string, RecordSuccess<TRecord>>();
        var failureRecords = new ConcurrentDictionary<string, RecordFailure<TRecord>>(
            batchRecords.Select(x => new KeyValuePair<string, RecordFailure<TRecord>>(x.Key, new RecordFailure<TRecord>
            {
                Exception = new UnprocessedRecordException($"Record: '{x.Key}' has not been processed."),
                Record = x.Value
            })));

        // Get error handling policy
        var errorHandlingPolicy = processingOptions?.ErrorHandlingPolicy is null or BatchProcessorErrorHandlingPolicy.DeriveFromEvent
            ? GetErrorHandlingPolicyForEvent(@event)
            : processingOptions.ErrorHandlingPolicy;

        // Invoke hook
        await BeforeBatchProcessingAsync(@event);

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
                    var result = await HandleRecordAsync(record, recordHandler, cancellationToken);

                    // Register success
                    failureRecords.TryRemove(recordId, out _);
                    successRecords.TryAdd(recordId, new RecordSuccess<TRecord>
                    {
                        Record = record,
                        RecordId = recordId,
                        HandlerResult = result
                    });

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
                    failureRecords[recordId] = new RecordFailure<TRecord>
                    {
                        Exception = new RecordProcessingException($"Failed processing record: '{recordId}'. See inner exception for details.", ex),
                        Record = record,
                        RecordId = recordId
                    };

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

        // Populate result
        ProcessingResult.BatchItemFailuresResponse.BatchItemFailures.AddRange(failureRecords.Select(x => new BatchItemFailuresResponse.BatchItemFailure
        {
            ItemIdentifier = x.Key
        }));
        ProcessingResult.BatchRecords.AddRange(batchRecords.Select(x => x.Value));
        ProcessingResult.SuccessRecords.AddRange(successRecords.Values);
        ProcessingResult.FailureRecords.AddRange(failureRecords.Values);

        // Invoke hook
        await AfterBatchProcessingAsync(@event, ProcessingResult);

        // Return result
        return ProcessingResult;
    }

    /// <summary>
    /// Hook invoked before the batch event is processed.
    /// </summary>
    /// <param name="event">The event to be processed.</param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    protected virtual async Task BeforeBatchProcessingAsync(TEvent @event)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Hook for the actual per-record business logic. This is invoked per batch record.
    /// </summary>
    /// <param name="record">The record to be handled.</param>
    /// <param name="recordHandler">The record handler.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    protected virtual async Task<RecordHandlerResult> HandleRecordAsync(
        TRecord record,
        IRecordHandler<TRecord> recordHandler,
        CancellationToken cancellationToken)
    {
        return await recordHandler.HandleAsync(record, cancellationToken);
    }

    /// <summary>
    /// Hook invoked after a batch record has been successfully processed.
    /// </summary>
    /// <param name="record">The record that has been successfully processed.</param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    protected virtual async Task HandleRecordSuccesssAsync(TRecord record)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Hook invoked after a batch record has failed processing.
    /// </summary>
    /// <param name="record">The record that has failed to be processed.</param>
    /// <param name="exception">The exception that was raised during processing of the record.</param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    protected virtual async Task HandleRecordFailureAsync(TRecord record, Exception exception)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Hook invoked after the batch event has been processed.
    /// </summary>
    /// <param name="event">The event that was processed.</param>
    /// <param name="processingResult"></param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    /// <exception cref="BatchProcessingException"/>
    protected virtual async Task AfterBatchProcessingAsync(TEvent @event, ProcessingResult<TRecord> processingResult)
    {
        if (processingResult.BatchRecords.Count == processingResult.FailureRecords.Count)
        {
            throw new BatchProcessingException($"Entire batch of '{processingResult.BatchRecords.Count}' record(s) failed processing. See inner exceptions for details.", processingResult.FailureRecords.Select(x => x.Exception));
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the error handling policy for the batch event.
    /// </summary>
    /// <param name="event">The batch event.</param>
    /// <returns>The <see cref="BatchProcessorErrorHandlingPolicy"/> for the batch event.</returns>
    protected abstract BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(TEvent @event);

    /// <summary>
    /// Gets the ordered set of batch records from the batch event.
    /// </summary>
    /// <param name="event">The batch event.</param>
    /// <returns>An ordered set of batch records.</returns>
    protected abstract ICollection<TRecord> GetRecordsFromEvent(TEvent @event);

    /// <summary>
    /// Gets the record id from the batch record.
    /// </summary>
    /// <param name="record">The batch record.</param>
    /// <returns>The record id of the batch record.</returns>
    protected abstract string GetRecordId(TRecord record);
}