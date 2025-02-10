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

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Represents a batch record that was successfully processed.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class RecordSuccess<TRecord>
{
    /// <summary>
    /// The result returned by the record handler processing the batch record.
    /// </summary>
    public RecordHandlerResult HandlerResult { get; init; }

    /// <summary>
    /// The batch record.
    /// </summary>
    public TRecord Record { get; init; }

    /// <summary>
    /// The batch record identifier.
    /// </summary>
    public string RecordId { get; init; }
}
