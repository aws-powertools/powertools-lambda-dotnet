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
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Custom;

internal class CustomDynamoDbBatchProcessor : DynamoDbStreamBatchProcessor
{
    public override async Task<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>> ProcessAsync(DynamoDBEvent @event,
        IRecordHandler<DynamoDBEvent.DynamodbStreamRecord> recordHandler, ProcessingOptions processingOptions)
    {
        ProcessingResult = new ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>();

        // Prepare batch records (order is preserved)
        var batchRecords = GetRecordsFromEvent(@event).Select(x => new KeyValuePair<string, DynamoDBEvent.DynamodbStreamRecord>(GetRecordId(x), x))
            .ToArray();

        // We assume all records fail by default to avoid loss of data
        var failureBatchRecords = batchRecords.Select(x => new KeyValuePair<string, RecordFailure<DynamoDBEvent.DynamodbStreamRecord>>(x.Key,
            new RecordFailure<DynamoDBEvent.DynamodbStreamRecord>
            {
                Exception = new UnprocessedRecordException($"Record: '{x.Key}' has not been processed."),
                Record = x.Value
            }));

        // Override to fail on first failure
        var errorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure;
        
        var successRecords = new Dictionary<string, RecordSuccess<DynamoDBEvent.DynamodbStreamRecord>>();
        var failureRecords = new Dictionary<string, RecordFailure<DynamoDBEvent.DynamodbStreamRecord>>(failureBatchRecords);

        try
        {
            foreach (var pair in batchRecords)
            {
                var (recordId, record) = pair;

                try
                {
                    var result = await HandleRecordAsync(record, recordHandler, CancellationToken.None);
                    failureRecords.Remove(recordId, out _);
                    successRecords.TryAdd(recordId, new RecordSuccess<DynamoDBEvent.DynamodbStreamRecord>
                    {
                        Record = record,
                        RecordId = recordId,
                        HandlerResult = result
                    });
                }
                catch (Exception ex)
                {
                    // Capture exception
                    failureRecords[recordId] = new RecordFailure<DynamoDBEvent.DynamodbStreamRecord>
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
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
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

    // ReSharper disable once RedundantOverriddenMember
    protected override async Task HandleRecordFailureAsync(DynamoDBEvent.DynamodbStreamRecord record, Exception exception)
    {
        await base.HandleRecordFailureAsync(record, exception);
    }
}

internal class CustomDynamoDbBatchProcessorProvider : IBatchProcessorProvider<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>
{
    public IBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord> Create()
    {
        return Services.Provider.GetRequiredService<CustomDynamoDbBatchProcessor>();
    }
}

public class BadCustomDynamoDbRecordProcessor
{
}