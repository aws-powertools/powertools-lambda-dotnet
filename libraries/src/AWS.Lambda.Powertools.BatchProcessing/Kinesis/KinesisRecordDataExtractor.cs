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
using System.IO;
using System.Text;
using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

/// <summary>
/// Extracts data from Kinesis event records for deserialization.
/// </summary>
public class KinesisRecordDataExtractor : IRecordDataExtractor<KinesisEvent.KinesisEventRecord>
{
    /// <summary>
    /// The singleton instance of the Kinesis record data extractor.
    /// </summary>
    public static readonly KinesisRecordDataExtractor Instance = new();

    /// <summary>
    /// Extracts the data from a Kinesis event record by reading from the MemoryStream.
    /// </summary>
    /// <param name="record">The Kinesis event record.</param>
    /// <returns>The decoded data string.</returns>
    public string ExtractData(KinesisEvent.KinesisEventRecord record)
    {
        if (record?.Kinesis?.Data == null)
            return string.Empty;

        try
        {
            // Reset the stream position to the beginning
            record.Kinesis.Data.Position = 0;
            
            // Read the data from the MemoryStream
            using var reader = new StreamReader(record.Kinesis.Data, Encoding.UTF8, leaveOpen: true);
            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            // If reading fails, return empty string
            return string.Empty;
        }
    }
}