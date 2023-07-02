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

using System.Collections.Generic;
using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

/// <summary>
/// The default batch processor for Kinesis Data Stream events.
/// </summary>
public class KinesisDataStreamBatchProcessor : BatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>
{
    /// <summary>
    /// The singleton instance of the batch processor.
    /// </summary>
    public static readonly KinesisDataStreamBatchProcessor Instance = new();

    /// <inheritdoc />
    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(KinesisEvent _) => BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure;

    /// <inheritdoc />
    protected override ICollection<KinesisEvent.KinesisEventRecord> GetRecordsFromEvent(KinesisEvent @event) => @event.Records;

    /// <inheritdoc />
    protected override string GetRecordId(KinesisEvent.KinesisEventRecord record) => record.Kinesis.SequenceNumber;
}
