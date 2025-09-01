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

using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="ITypedRecordHandlerWithContext{T}"/> interface for strongly-typed record handling with Lambda context.
/// </summary>
/// <typeparam name="T">Type of the deserialized data from the batch record.</typeparam>
public interface ITypedRecordHandlerWithContext<in T>
{
    /// <summary>
    /// Handles processing of a given batch record with strongly-typed data and Lambda context.
    /// </summary>
    /// <param name="data">The deserialized data from the record to process.</param>
    /// <param name="context">The Lambda context for the current invocation.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>An awaitable <see cref="Task"/> with a <see cref="RecordHandlerResult"/>.</returns>
    Task<RecordHandlerResult> HandleAsync(T data, ILambdaContext context, CancellationToken cancellationToken);
}