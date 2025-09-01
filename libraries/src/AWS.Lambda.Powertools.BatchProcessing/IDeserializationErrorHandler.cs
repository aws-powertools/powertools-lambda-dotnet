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
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Interface for handling deserialization errors during batch processing.
/// </summary>
/// <typeparam name="TRecord">The type of the record being processed.</typeparam>
public interface IDeserializationErrorHandler<in TRecord>
{
    /// <summary>
    /// Handles a deserialization error for a specific record.
    /// </summary>
    /// <param name="record">The record that failed to deserialize.</param>
    /// <param name="exception">The exception that occurred during deserialization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the record handler result.</returns>
    Task<RecordHandlerResult> HandleDeserializationError(TRecord record, Exception exception, CancellationToken cancellationToken);
}