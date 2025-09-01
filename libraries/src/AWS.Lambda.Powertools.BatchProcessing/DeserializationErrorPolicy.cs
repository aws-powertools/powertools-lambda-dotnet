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
/// Defines how deserialization errors should be handled during batch processing.
/// </summary>
public enum DeserializationErrorPolicy
{
    /// <summary>
    /// Mark the record as failed when deserialization fails (default behavior).
    /// The record will be included in the batch failure response.
    /// </summary>
    FailRecord,

    /// <summary>
    /// Skip records that fail deserialization and continue processing other records.
    /// Failed records will not be included in the batch failure response.
    /// </summary>
    IgnoreRecord,

    /// <summary>
    /// Use a custom error handler to process deserialization failures.
    /// The custom handler determines how to handle the failed record.
    /// </summary>
    CustomHandler
}