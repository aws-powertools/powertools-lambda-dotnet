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
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <summary>
/// The default batch processor for DynamoDB Stream events.
/// </summary>
public class DynamoDbStreamBatchProcessor : BatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>
{
    /// <summary>
    /// The singleton instance of the batch processor.
    /// </summary>
    private static DynamoDbStreamBatchProcessor _instance;
 
    /// <summary>
    /// The singleton instance of the batch processor.
    /// </summary>
    public static DynamoDbStreamBatchProcessor Instance =>
        _instance ??= new DynamoDbStreamBatchProcessor(PowertoolsConfigurations.Instance);
    
    /// <summary>
    /// This is the default constructor
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    public DynamoDbStreamBatchProcessor(IPowertoolsConfigurations powertoolsConfigurations) : base(powertoolsConfigurations)
    {
        _instance = this;
    }

    /// <summary>
    /// Need default constructor for when consumers create a custom batch processor
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected DynamoDbStreamBatchProcessor() : this(PowertoolsConfigurations.Instance)
    {
    }

    /// <summary>
    /// Return the instance ProcessingResult
    /// </summary>
    public static ProcessingResult<DynamoDBEvent.DynamodbStreamRecord> Result => _instance.ProcessingResult;

    /// <inheritdoc />
    protected override BatchProcessorErrorHandlingPolicy GetErrorHandlingPolicyForEvent(DynamoDBEvent _) => BatchProcessorErrorHandlingPolicy.ContinueOnBatchItemFailure;

    /// <inheritdoc />
    protected override ICollection<DynamoDBEvent.DynamodbStreamRecord> GetRecordsFromEvent(DynamoDBEvent @event) => @event.Records;

    /// <inheritdoc />
    protected override string GetRecordId(DynamoDBEvent.DynamodbStreamRecord record) => record.Dynamodb.SequenceNumber;
}
