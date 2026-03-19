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
/// Fetches metadata from the Lambda Metadata Endpoint (LMDS).
/// </summary>
internal sealed class MetadataFetcher : IMetadataFetcher
{
    private const string EnvMetadataApi = "AWS_LAMBDA_METADATA_API";
    private const string EnvMetadataToken = "AWS_LAMBDA_METADATA_TOKEN";
    private const string ApiVersion = "2026-01-15";
    private const string MetadataPath = "/metadata/execution-environment";

    private readonly HttpClient _httpClient;

    public MetadataFetcher() : this(CreateHttpClient())
    {
    }

    internal MetadataFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None
        })
        {
            Timeout = TimeSpan.FromSeconds(1)
        };
    }

    public MetadataValues Fetch()
    {
        var (token, url) = GetEndpointInfo();

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

    private static (string token, string url) GetEndpointInfo()
    {
        var token = Environment.GetEnvironmentVariable(EnvMetadataToken);
        var api = Environment.GetEnvironmentVariable(EnvMetadataApi);

        if (string.IsNullOrEmpty(token))
            throw new LambdaMetadataException(
                $"Lambda metadata token not available. Ensure {EnvMetadataToken} is set.");

        if (string.IsNullOrEmpty(api))
            throw new LambdaMetadataException(
                $"Lambda metadata API endpoint not available. Ensure {EnvMetadataApi} is set.");

        return (token, $"http://{api}/{ApiVersion}{MetadataPath}");
    }

    private static MetadataValues ProcessResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new LambdaMetadataException(
                $"Metadata request failed with status {(int)response.StatusCode}: {error}",
                (int)response.StatusCode);
        }

        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonSerializer.Deserialize(body, LambdaMetadataSerializerContext.Default.MetadataValues)
               ?? throw new LambdaMetadataException("Failed to deserialize Lambda metadata response.");
    }
}
