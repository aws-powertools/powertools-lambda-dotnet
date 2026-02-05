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

using System.Net;
using System.Text.Json;
using AWS.Lambda.Powertools.Metadata.Exceptions;

namespace AWS.Lambda.Powertools.Metadata.Internal;

/// <summary>
/// Internal HTTP client for fetching metadata from the Lambda Metadata Endpoint.
/// <para>
/// Uses <see cref="HttpClient"/> for HTTP requests. The client is designed to be
/// AOT-compatible and uses source-generated JSON serialization.
/// </para>
/// </summary>
internal class LambdaMetadataHttpClient : ILambdaMetadataHttpClient
{
    internal const string EnvMetadataApi = "AWS_LAMBDA_METADATA_API";
    internal const string EnvMetadataToken = "AWS_LAMBDA_METADATA_TOKEN";
    private const string ApiVersion = "2026-01-15";
    private const string MetadataPath = "/metadata/execution-environment";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a new instance of the HTTP client.
    /// </summary>
    public LambdaMetadataHttpClient() : this(CreateDefaultHttpClient())
    {
    }

    /// <summary>
    /// Creates a new instance with a custom HttpClient (for testing).
    /// </summary>
    internal LambdaMetadataHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None
        };
        
        return new HttpClient(handler)
        {
            Timeout = Timeout
        };
    }

    /// <inheritdoc />
    public LambdaMetadata FetchMetadata()
    {
        var (token, api, url) = ValidateAndBuildUrl();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            using var response = _httpClient.Send(request);
            return ProcessResponse(response);
        }
        catch (LambdaMetadataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LambdaMetadataException($"Failed to fetch Lambda metadata: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<LambdaMetadata> FetchMetadataAsync(CancellationToken cancellationToken = default)
    {
        var (token, api, url) = ValidateAndBuildUrl();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await ProcessResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (LambdaMetadataException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LambdaMetadataException($"Failed to fetch Lambda metadata: {ex.Message}", ex);
        }
    }

    private (string token, string api, string url) ValidateAndBuildUrl()
    {
        var token = GetEnvironmentVariable(EnvMetadataToken);
        var api = GetEnvironmentVariable(EnvMetadataApi);

        if (string.IsNullOrEmpty(token))
        {
            throw new LambdaMetadataException(
                $"Lambda metadata token not available. Ensure {EnvMetadataToken} is set.");
        }

        if (string.IsNullOrEmpty(api))
        {
            throw new LambdaMetadataException(
                $"Lambda metadata API endpoint not available. Ensure {EnvMetadataApi} is set.");
        }

        var url = $"http://{api}/{ApiVersion}{MetadataPath}";
        return (token, api, url);
    }

    private LambdaMetadata ProcessResponse(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new LambdaMetadataException(
                $"Metadata request failed with status {statusCode}: {errorContent}",
                statusCode);
        }

        var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return DeserializeMetadata(responseBody);
    }

    private async Task<LambdaMetadata> ProcessResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new LambdaMetadataException(
                $"Metadata request failed with status {statusCode}: {errorContent}",
                statusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return DeserializeMetadata(responseBody);
    }

    private static LambdaMetadata DeserializeMetadata(string responseBody)
    {
        var metadata = JsonSerializer.Deserialize(responseBody, LambdaMetadataSerializerContext.Default.LambdaMetadata);
        return metadata ?? throw new LambdaMetadataException("Failed to deserialize Lambda metadata response.");
    }

    /// <summary>
    /// Gets an environment variable value.
    /// This method is virtual to allow overriding in tests.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <returns>The value, or null if not set.</returns>
    internal virtual string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
