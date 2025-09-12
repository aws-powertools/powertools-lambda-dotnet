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
/// Enum to specify the error handling policy applied during batch processing.
/// </summary>
public enum BatchProcessorErrorHandlingPolicy
{
    /// <summary>
    /// Auto-derive the policy based on the event.
    /// </summary>
    DeriveFromEvent,

    /// <summary>
    /// Continue processing regardless of whether other batch items fails during processing.
    /// </summary>
    ContinueOnBatchItemFailure,

    /// <summary>
    /// Stop processing other batch items after the first batch item has failed processing.
    /// This is useful to preserve ordered processing of events.
    /// <br/>
    /// When parallel processing is enabled, all batch items already scheduled to be processed,
    /// will be allowed to complete before the batch processing stops.
    /// <br/>
    /// Therefore, if order is important, it is recommended to use sequential (non-parallel) processing
    /// together with this value.
    /// </summary>
    StopOnFirstBatchItemFailure
}
