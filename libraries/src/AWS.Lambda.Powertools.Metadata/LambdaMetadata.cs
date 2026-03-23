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
using AWS.Lambda.Powertools.Metadata.Exceptions;
using AWS.Lambda.Powertools.Metadata.Internal;

namespace AWS.Lambda.Powertools.Metadata;

/// <summary>
/// Provides access to Lambda execution environment metadata from the Lambda Metadata Endpoint (LMDS).
/// <para>
/// Metadata is automatically fetched on first access and cached for the Lambda sandbox lifetime.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var azId = LambdaMetadata.AvailabilityZoneId;
/// </code>
/// </example>
public static class LambdaMetadata
{
    private static readonly object Lock = new();
    private static volatile MetadataValues? _cached;
    private static IMetadataFetcher _fetcher = new MetadataFetcher();

    /// <summary>
    /// Gets the Availability Zone ID where the Lambda function is executing.
    /// </summary>
    /// <example>Example value: "use1-az1"</example>
    /// <exception cref="LambdaMetadataException">
    /// Thrown if the metadata endpoint is unavailable or returns an error.
    /// </exception>
    public static string? AvailabilityZoneId => GetCached().AvailabilityZoneId;

    /// <summary>
    /// Forces a refresh of the cached metadata.
    /// <para>
    /// In most cases, you don't need this since metadata remains constant for the
    /// Lambda sandbox lifetime.
    /// </para>
    /// </summary>
    /// <exception cref="LambdaMetadataException">
    /// Thrown if the metadata endpoint is unavailable or returns an error.
    /// </exception>
    public static void Refresh()
    {
        lock (Lock)
        {
            _cached = _fetcher.Fetch();
        }
    }

    private static MetadataValues GetCached()
    {
        var instance = _cached;
        if (instance is not null)
            return instance;

        lock (Lock)
        {
            instance = _cached;
            if (instance is not null)
                return instance;

            var newInstance = _fetcher.Fetch();
            _cached = newInstance;
            return newInstance;
        }
    }

    /// <summary>
    /// Sets the metadata fetcher (for testing only).
    /// </summary>
    internal static void SetFetcher(IMetadataFetcher fetcher)
    {
        lock (Lock)
        {
            _fetcher = fetcher;
            _cached = null;
        }
    }

    /// <summary>
    /// Resets the cached instance (for testing only).
    /// </summary>
    internal static void Reset()
    {
        lock (Lock)
        {
            _cached = null;
        }
    }
}

/// <summary>
/// Internal class for JSON deserialization of metadata values.
/// </summary>
internal sealed class MetadataValues
{
    [JsonPropertyName("AvailabilityZoneID")]
    public string? AvailabilityZoneId { get; init; }

    public MetadataValues() { }

    public MetadataValues(string? availabilityZoneId)
    {
        AvailabilityZoneId = availabilityZoneId;
    }
}
