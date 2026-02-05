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

namespace AWS.Lambda.Powertools.Metadata.Internal;

/// <summary>
/// Interface for the Lambda Metadata HTTP client.
/// </summary>
internal interface ILambdaMetadataHttpClient
{
    /// <summary>
    /// Fetches metadata from the Lambda Metadata Endpoint synchronously.
    /// </summary>
    /// <returns>The Lambda metadata.</returns>
    LambdaMetadata FetchMetadata();

    /// <summary>
    /// Fetches metadata from the Lambda Metadata Endpoint asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Lambda metadata.</returns>
    Task<LambdaMetadata> FetchMetadataAsync(CancellationToken cancellationToken = default);
}
