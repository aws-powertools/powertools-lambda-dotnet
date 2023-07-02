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
using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// The default batch processor for SQS events.
/// </summary>
public class SqsBatchProcessor : BatchProcessor<SQSEvent, SQSEvent.SQSMessage>
{
    /// <summary>
    /// The singleton instance of the batch processor.
    /// </summary>
    public static readonly SqsBatchProcessor Instance = new();

    /// <inheritdoc />
    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(SQSEvent @event)
    {
        var isSqsFifoSource = @event.Records.FirstOrDefault()?.EventSourceArn?.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
        return isSqsFifoSource == true
            ? BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure
            : BatchProcessorErrorHandlingPolicy.ContinueOnBatchItemFailure;
    }

    /// <inheritdoc />
    protected override ICollection<SQSEvent.SQSMessage> GetRecordsFromEvent(SQSEvent @event) => @event.Records;

    /// <inheritdoc />
    protected override string GetRecordId(SQSEvent.SQSMessage record) => record.MessageId;
}