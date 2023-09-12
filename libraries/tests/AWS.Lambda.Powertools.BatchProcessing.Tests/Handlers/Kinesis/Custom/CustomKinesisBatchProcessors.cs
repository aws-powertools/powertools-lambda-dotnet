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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Custom;

internal class CustomKinesisDataStreamBatchProcessor : KinesisDataStreamBatchProcessor
{
    public override async Task<ProcessingResult<KinesisEvent.KinesisEventRecord>> ProcessAsync(KinesisEvent @event,
        IRecordHandler<KinesisEvent.KinesisEventRecord> recordHandler, ProcessingOptions processingOptions)
    {
        Logger.LogInformation($"Processing {@event.Records.Count} record(s) using: '{GetType().Name}'.");

        ProcessingResult = new ProcessingResult<KinesisEvent.KinesisEventRecord>();

        // Prepare batch records (order is preserved)
        var batchRecords = GetRecordsFromEvent(@event).Select(x => new KeyValuePair<string, KinesisEvent.KinesisEventRecord>(GetRecordId(x), x))
            .ToArray();

        // We assume all records fail by default to avoid loss of data
        var failureBatchRecords = batchRecords.Select(x => new KeyValuePair<string, RecordFailure<KinesisEvent.KinesisEventRecord>>(x.Key,
            new RecordFailure<KinesisEvent.KinesisEventRecord>
            {
                Exception = new UnprocessedRecordException($"Record: '{x.Key}' has not been processed."),
                Record = x.Value
            }));

        // Override to fail on first failure
        var errorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure;
        
        var successRecords = new Dictionary<string, RecordSuccess<KinesisEvent.KinesisEventRecord>>();
        var failureRecords = new Dictionary<string, RecordFailure<KinesisEvent.KinesisEventRecord>>(failureBatchRecords);

        try
        {
            foreach (var pair in batchRecords)
            {
                var (recordId, record) = pair;

                try
                {
                    var result = await HandleRecordAsync(record, recordHandler, CancellationToken.None);
                    failureRecords.Remove(recordId, out _);
                    successRecords.TryAdd(recordId, new RecordSuccess<KinesisEvent.KinesisEventRecord>
                    {
                        Record = record,
                        RecordId = recordId,
                        HandlerResult = result
                    });
                }
                catch (Exception ex)
                {
                    // Capture exception
                    failureRecords[recordId] = new RecordFailure<KinesisEvent.KinesisEventRecord>
                    {
                        Exception = new RecordProcessingException(
                            $"Failed processing record: '{recordId}'. See inner exception for details.", ex),
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
                        throw new CircuitBreakerException(
                            "Error handling policy is configured to stop processing on first batch item failure. See inner exception for details.",
                            ex);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is CircuitBreakerException or OperationCanceledException)
        {
            // NOOP
        }
        
        ProcessingResult.BatchRecords.AddRange(batchRecords.Select(x => x.Value));
        ProcessingResult.BatchItemFailuresResponse.BatchItemFailures.AddRange(failureRecords.Select(x =>
            new BatchItemFailuresResponse.BatchItemFailure
            {
                ItemIdentifier = x.Key
            }));
        ProcessingResult.FailureRecords.AddRange(failureRecords.Values);

        ProcessingResult.SuccessRecords.AddRange(successRecords.Values);

        return ProcessingResult;
    }

    protected override async Task HandleRecordFailureAsync(KinesisEvent.KinesisEventRecord record, Exception exception)
    {
        Logger.LogWarning(exception, $"Failed to process record: '{record.Kinesis.SequenceNumber}'.");
        await base.HandleRecordFailureAsync(record, exception);
    }
}

internal class CustomKinesisDataStreamBatchProcessorProvider : IBatchProcessorProvider<KinesisEvent, KinesisEvent.KinesisEventRecord>
{
    public IBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord> Create()
    {
        Logger.LogInformation($"Creating SQS batch processor using: '{GetType().Name}'.");
        return Services.Provider.GetRequiredService<CustomKinesisDataStreamBatchProcessor>();
    }
}

public class BadCustomKinesisDataStreamRecordProcessor
{
}