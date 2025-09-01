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
/// Interface for extracting data from event records for deserialization.
/// </summary>
/// <typeparam name="TRecord">The type of the event record.</typeparam>
public interface IRecordDataExtractor<in TRecord>
{
    /// <summary>
    /// Extracts the data string from the event record that should be deserialized.
    /// </summary>
    /// <param name="record">The event record to extract data from.</param>
    /// <returns>The data string to be deserialized.</returns>
    string ExtractData(TRecord record);
}