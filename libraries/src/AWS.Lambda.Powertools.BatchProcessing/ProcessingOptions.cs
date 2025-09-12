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
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Batch processing options to control settings such as cancellation, error handling policy and parallelism.
/// </summary>
public class ProcessingOptions
{
    /// <summary>
    /// The cancellation token to monitor.
    /// </summary>
    public CancellationToken? CancellationToken { get; init; }

    /// <summary>
    /// The maximum degree of parallelism to apply during batch processing if <see cref="BatchParallelProcessingEnabled"/> is enabled (default is -1, which means <see cref="Environment.ProcessorCount"/>).
    /// </summary>
    public int? MaxDegreeOfParallelism { get; init; }

    /// <summary>
    /// The error handling policy to apply during batch processing (default is <see cref="BatchProcessorErrorHandlingPolicy.DeriveFromEvent"/>).
    /// </summary>
    public BatchProcessorErrorHandlingPolicy? ErrorHandlingPolicy { get; init; }

    /// <summary>
    /// Controls whether parallel batch processing is enabled (default false).
    /// </summary>
    public bool BatchParallelProcessingEnabled { get; init; } = false;

    /// <summary>
    /// Controls whether the Batch processor throws a <see cref="BatchProcessingException"/> on full batch failure (default true).
    /// </summary>
    public bool ThrowOnFullBatchFailure { get; init; } = true;
}
