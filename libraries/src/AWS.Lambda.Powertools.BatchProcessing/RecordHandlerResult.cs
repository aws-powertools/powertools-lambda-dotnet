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
/// The result of the record handler processing the batch record.
/// </summary>
public class RecordHandlerResult
{
    /// <summary>
    /// Returns an empty <see cref="RecordHandlerResult"/> value.
    /// </summary>
    public static RecordHandlerResult None { get; } = null!;

    /// <summary>
    /// Convenience method for the creation of a <see cref="RecordHandlerResult"/>.
    /// </summary>
    /// <param name="data">The result of the record handler.</param>
    /// <returns>A <see cref="RecordHandlerResult"/> with the provided data.</returns>
    public static RecordHandlerResult FromData(object data)
    {
        return new RecordHandlerResult
        {
            Data = data
        };
    }

    /// <summary>
    /// The data returned from processing the batch record.
    /// </summary>
    public object Data { get; init; }
}
