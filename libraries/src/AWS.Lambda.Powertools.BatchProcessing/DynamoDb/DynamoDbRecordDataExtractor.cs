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

using System.Text.Json;
using Amazon.Lambda.DynamoDBEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <summary>
/// Extracts data from DynamoDB stream records for deserialization.
/// </summary>
public class DynamoDbRecordDataExtractor : IRecordDataExtractor<DynamoDBEvent.DynamodbStreamRecord>
{
    /// <summary>
    /// The singleton instance of the DynamoDB record data extractor.
    /// </summary>
    public static readonly DynamoDbRecordDataExtractor Instance = new();

    /// <summary>
    /// Extracts the data from a DynamoDB stream record by serializing the entire DynamoDB record.
    /// For INSERT and MODIFY events, this includes the NewImage. For REMOVE events, this includes the OldImage.
    /// </summary>
    /// <param name="record">The DynamoDB stream record.</param>
    /// <returns>The serialized DynamoDB record data.</returns>
    public string ExtractData(DynamoDBEvent.DynamodbStreamRecord record)
    {
        if (record?.Dynamodb == null)
            return string.Empty;

        // Create a simplified representation of the DynamoDB record for deserialization
        var recordData = new
        {
            EventName = record.EventName,
            Keys = record.Dynamodb.Keys,
            NewImage = record.Dynamodb.NewImage,
            OldImage = record.Dynamodb.OldImage,
            SequenceNumber = record.Dynamodb.SequenceNumber,
            SizeBytes = record.Dynamodb.SizeBytes,
            StreamViewType = record.Dynamodb.StreamViewType
        };

        return JsonSerializer.Serialize(recordData);
    }
}