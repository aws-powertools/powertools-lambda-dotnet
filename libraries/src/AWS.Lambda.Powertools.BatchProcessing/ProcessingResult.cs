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

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Contains the result of the most recent batch processing run.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class ProcessingResult<TRecord>
{
    /// <summary>
    /// The batch item failures response, containing a list of partial batch item failures.
    /// This is the response to be returned from the Lambda function handler.
    /// </summary>
    public BatchItemFailuresResponse BatchItemFailuresResponse { get; } = new();

    /// <summary>
    /// The set of batch records from the batch event.
    /// </summary>
    public List<TRecord> BatchRecords { get; } = new();

    /// <summary>
    /// The set of batch records that were successfully processed.
    /// </summary>
    public List<RecordSuccess<TRecord>> SuccessRecords { get; } = new();

    /// <summary>
    /// The set of batch records that failed processing.
    /// </summary>
    public List<RecordFailure<TRecord>> FailureRecords { get; } = new();

    /// <summary>
    /// Clears the result object.
    /// </summary>
    public void Clear()
    {
        BatchRecords.Clear();
        SuccessRecords.Clear();
        FailureRecords.Clear();
    }
}