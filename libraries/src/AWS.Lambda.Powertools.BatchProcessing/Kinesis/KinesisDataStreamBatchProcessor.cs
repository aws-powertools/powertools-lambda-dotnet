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
using AWS.Lambda.Powertools.Common;

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
    
    /// <summary>
    /// This is the default constructor
    /// </summary>
    /// <param name="instance"></param>
    public KinesisDataStreamBatchProcessor(IPowertoolsConfigurations instance)
    {
    }

    /// <summary>
    /// Need default constructor for when consumers create a custom batch processor
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected KinesisDataStreamBatchProcessor() : this(PowertoolsConfigurations.Instance)
    {
    }
    
    /// <summary>
    /// Return the instance ProcessingResult.BatchItemFailuresResponse
    /// </summary>
    public static BatchItemFailuresResponse BatchItemFailuresResponse()
    {
        return Instance.ProcessingResult.BatchItemFailuresResponse;
    }

    /// <inheritdoc />
    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(KinesisEvent _) => BatchProcessorErrorHandlingPolicy.ContinueOnBatchItemFailure;

    /// <inheritdoc />
    protected override ICollection<KinesisEvent.KinesisEventRecord> GetRecordsFromEvent(KinesisEvent @event) => @event.Records;

    /// <inheritdoc />
    protected override string GetRecordId(KinesisEvent.KinesisEventRecord record) => record.Kinesis.SequenceNumber;
}
