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

using AWS.Lambda.Powertools.Metadata.Exceptions;
using AWS.Lambda.Powertools.Metadata.Internal;

namespace AWS.Lambda.Powertools.Metadata;

/// <summary>
/// Client for accessing Lambda execution environment metadata.
/// <para>
/// This utility provides idiomatic access to the Lambda Metadata Endpoint (LMDS),
/// eliminating boilerplate code for retrieving execution environment metadata
/// like Availability Zone ID.
/// </para>
/// <para>
/// Features:
/// <list type="bullet">
///   <item><description>Automatic caching for the sandbox lifetime</description></item>
///   <item><description>Thread-safe access for concurrent executions</description></item>
///   <item><description>Async/await support</description></item>
///   <item><description>Lazy loading on first access</description></item>
///   <item><description>Native AOT compatible</description></item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// Basic usage:
/// <code>
/// public string HandleRequest(object input, ILambdaContext context)
/// {
///     var metadata = LambdaMetadataClient.Get();
///     var azId = metadata.AvailabilityZoneId;
///     return $"{{\"az\": \"{azId}\"}}";
/// }
/// </code>
/// </example>
/// <example>
/// Async usage:
/// <code>
/// public async Task&lt;string&gt; HandleRequestAsync(object input, ILambdaContext context)
/// {
///     var metadata = await LambdaMetadataClient.GetAsync();
///     var azId = metadata.AvailabilityZoneId;
///     return $"{{\"az\": \"{azId}\"}}";
/// }
/// </code>
/// </example>
/// <example>
/// Eager loading during cold start:
/// <code>
/// public class MyHandler
/// {
///     // Fetch during cold start
///     private static readonly LambdaMetadata Metadata = LambdaMetadataClient.Get();
///
///     public string HandleRequest(object input, ILambdaContext context)
///     {
///         return $"{{\"az\": \"{Metadata.AvailabilityZoneId}\"}}";
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="LambdaMetadata"/>
public static class LambdaMetadataClient
{
    private static readonly object Lock = new();
    private static readonly SemaphoreSlim AsyncLock = new(1, 1);
    private static volatile LambdaMetadata? _cachedInstance;
    private static ILambdaMetadataHttpClient _httpClient = new LambdaMetadataHttpClient();

    /// <summary>
    /// Retrieves the cached metadata, fetching from the endpoint if not cached.
    /// <para>
    /// This method is thread-safe and handles concurrent access correctly.
    /// The first call fetches metadata from the Lambda Metadata Endpoint,
    /// subsequent calls return the cached value.
    /// </para>
    /// </summary>
    /// <returns>The <see cref="LambdaMetadata"/> instance.</returns>
    /// <exception cref="LambdaMetadataException">
    /// Thrown if the metadata endpoint is unavailable or returns an error.
    /// </exception>
    public static LambdaMetadata Get()
    {
        // Fast path: return cached instance if available (volatile read)
        var instance = _cachedInstance;
        if (instance is not null)
        {
            return instance;
        }

        // Slow path: acquire lock and fetch
        lock (Lock)
        {
            // Double-check after acquiring lock
            instance = _cachedInstance;
            if (instance is not null)
            {
                return instance;
            }

            var newInstance = _httpClient.FetchMetadata();
            _cachedInstance = newInstance;
            return newInstance;
        }
    }

    /// <summary>
    /// Retrieves the cached metadata asynchronously, fetching from the endpoint if not cached.
    /// <para>
    /// This method is thread-safe and handles concurrent access correctly.
    /// The first call fetches metadata from the Lambda Metadata Endpoint,
    /// subsequent calls return the cached value.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="LambdaMetadata"/> instance.</returns>
    /// <exception cref="LambdaMetadataException">
    /// Thrown if the metadata endpoint is unavailable or returns an error.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled.
    /// </exception>
    public static async Task<LambdaMetadata> GetAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: return cached instance if available (volatile read)
        var instance = _cachedInstance;
        if (instance is not null)
        {
            return instance;
        }

        // Slow path: acquire async lock and fetch
        await AsyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            instance = _cachedInstance;
            if (instance is not null)
            {
                return instance;
            }

            var newInstance = await _httpClient.FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
            _cachedInstance = newInstance;
            return newInstance;
        }
        finally
        {
            AsyncLock.Release();
        }
    }

    /// <summary>
    /// Forces a refresh of the cached metadata.
    /// <para>
    /// This method clears the cache and fetches fresh metadata from the endpoint.
    /// Use this only for advanced use cases where you need to force a refresh.
    /// </para>
    /// </summary>
    /// <returns>The refreshed <see cref="LambdaMetadata"/> instance.</returns>
    /// <exception cref="LambdaMetadataException">
    /// Thrown if the metadata endpoint is unavailable or returns an error.
    /// </exception>
    public static LambdaMetadata Refresh()
    {
        lock (Lock)
        {
            _cachedInstance = null;
            var newInstance = _httpClient.FetchMetadata();
            _cachedInstance = newInstance;
            return newInstance;
        }
    }

    /// <summary>
    /// Forces a refresh of the cached metadata asynchronously.
    /// <para>
    /// This method clears the cache and fetches fresh metadata from the endpoint.
    /// Use this only for advanced use cases where you need to force a refresh.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refreshed <see cref="LambdaMetadata"/> instance.</returns>
    /// <exception cref="LambdaMetadataException">
    /// Thrown if the metadata endpoint is unavailable or returns an error.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled.
    /// </exception>
    public static async Task<LambdaMetadata> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await AsyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cachedInstance = null;
            var newInstance = await _httpClient.FetchMetadataAsync(cancellationToken).ConfigureAwait(false);
            _cachedInstance = newInstance;
            return newInstance;
        }
        finally
        {
            AsyncLock.Release();
        }
    }

    /// <summary>
    /// Sets the HTTP client (for testing purposes only).
    /// </summary>
    /// <param name="client">The client to use.</param>
    internal static void SetHttpClient(ILambdaMetadataHttpClient client)
    {
        lock (Lock)
        {
            _httpClient = client;
            _cachedInstance = null;
        }
    }

    /// <summary>
    /// Resets the cached instance (for testing purposes only).
    /// </summary>
    internal static void ResetCache()
    {
        lock (Lock)
        {
            _cachedInstance = null;
        }
    }
}
