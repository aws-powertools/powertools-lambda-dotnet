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

using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metadata;

/// <summary>
/// Data class representing Lambda execution environment metadata.
/// <para>
/// This class is immutable and contains metadata retrieved from the Lambda Metadata Endpoint (LMDS).
/// Use <see cref="LambdaMetadataClient.Get"/> to obtain an instance.
/// </para>
/// <para>
/// Unknown properties in the JSON response are ignored to ensure forward compatibility.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var metadata = LambdaMetadataClient.Get();
/// var azId = metadata.AvailabilityZoneId;
/// </code>
/// </example>
/// <seealso cref="LambdaMetadataClient"/>
public sealed class LambdaMetadata
{
    /// <summary>
    /// Gets the Availability Zone ID.
    /// <para>
    /// The Availability Zone ID is a unique identifier for the availability zone
    /// where the Lambda function is executing (e.g., "use1-az1").
    /// </para>
    /// </summary>
    [JsonPropertyName("AvailabilityZoneID")]
    public string? AvailabilityZoneId { get; init; }

    /// <summary>
    /// Default constructor for JSON deserialization.
    /// </summary>
    public LambdaMetadata()
    {
    }

    /// <summary>
    /// Constructor with availability zone ID.
    /// </summary>
    /// <param name="availabilityZoneId">The availability zone ID.</param>
    public LambdaMetadata(string? availabilityZoneId)
    {
        AvailabilityZoneId = availabilityZoneId;
    }
}
