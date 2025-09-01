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

using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// Extracts data from SQS message records for deserialization.
/// </summary>
public class SqsRecordDataExtractor : IRecordDataExtractor<SQSEvent.SQSMessage>
{
    /// <summary>
    /// The singleton instance of the SQS record data extractor.
    /// </summary>
    public static readonly SqsRecordDataExtractor Instance = new();

    /// <summary>
    /// Extracts the message body from an SQS message record.
    /// </summary>
    /// <param name="record">The SQS message record.</param>
    /// <returns>The message body string.</returns>
    public string ExtractData(SQSEvent.SQSMessage record)
    {
        return record?.Body ?? string.Empty;
    }
}