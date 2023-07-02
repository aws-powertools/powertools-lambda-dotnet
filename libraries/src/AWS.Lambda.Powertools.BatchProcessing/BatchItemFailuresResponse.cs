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

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The batch items failure response. Follows the signature outlined in the <see href="https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html#services-sqs-batchfailurereporting">AWS Lambda documentation</see>.
/// <br/>
/// <example>
/// An example JSON serialization of the object:
/// <code>
/// { 
///   "batchItemFailures": [ 
///         {
///             "itemIdentifier": "id2"
///         },
///         {
///             "itemIdentifier": "id4"
///         }
///     ]
/// }
/// </code>
///
/// This object is what enables the Lambda function to return a partial success:
/// 
/// <list type="bullet|number|table">
///     <item>
///         <term>If a batch item identifier is EXCLUDED from this list:</term>
///         <description>Then the batch item processed succesfully and will not be processed again.</description>
///     </item>
///     <item>
///         <term>If a batch item identifier is INCLUDED in this list:</term>
///         <description>Then the batch item failed processing and will be made available for re-processing.</description>
///     </item>
/// </list>
/// </example>
/// </summary>
[DataContract]
public class BatchItemFailuresResponse
{
    /// <summary>
    /// <see cref="BatchItemFailuresResponse"/> constructor.
    /// </summary>
    public BatchItemFailuresResponse() : this(new List<BatchItemFailure>())
    {
    }

    /// <summary>
    /// <see cref="BatchItemFailuresResponse"/> constructor.
    /// </summary>
    /// <param name="batchItemFailures">A list of batch item failures.</param>
    public BatchItemFailuresResponse(List<BatchItemFailure> batchItemFailures)
    {
        BatchItemFailures = batchItemFailures;
    }

    /// <summary>
    /// The set of batch item failures.
    /// </summary>
    [DataMember(Name = "batchItemFailures")]
    [JsonPropertyName("batchItemFailures")]
    public List<BatchItemFailure> BatchItemFailures { get; init; }

    /// <summary>
    /// The batch item failure definition.
    /// </summary>
    [DataContract]
    public class BatchItemFailure
    {
        /// <summary>
        /// The identifier of the batch item that failed processing.
        /// </summary>
        [DataMember(Name = "itemIdentifier")]
        [JsonPropertyName("itemIdentifier")]
        public string ItemIdentifier { get; init; }
    }
}