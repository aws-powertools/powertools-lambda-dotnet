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
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Metadata.Tests.Internal;

public class LambdaMetadataHttpClientTests
{
    private const string TestToken = "test-token-12345";

    #region Synchronous FetchMetadata Tests

    [Fact]
    public void FetchMetadata_Should_ThrowOnMissingToken()
    {
        // Given
        var client = new TestableHttpClient(null, "localhost:8080");

        // When/Then
        var act = () => client.FetchMetadata();
        act.Should().Throw<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataToken}*");
    }

    [Fact]
    public void FetchMetadata_Should_ThrowOnMissingApi()
    {
        // Given
        var client = new TestableHttpClient(TestToken, null);

        // When/Then
        var act = () => client.FetchMetadata();
        act.Should().Throw<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataApi}*");
    }

    [Fact]
    public void FetchMetadata_Should_ThrowOnEmptyToken()
    {
        // Given
        var client = new TestableHttpClient("", "localhost:8080");

        // When/Then
        var act = () => client.FetchMetadata();
        act.Should().Throw<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataToken}*");
    }

    [Fact]
    public void FetchMetadata_Should_ThrowOnEmptyApi()
    {
        // Given
        var client = new TestableHttpClient(TestToken, "");

        // When/Then
        var act = () => client.FetchMetadata();
        act.Should().Throw<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataApi}*");
    }

    #endregion

    #region Asynchronous FetchMetadataAsync Tests

    [Fact]
    public async Task FetchMetadataAsync_Should_ThrowOnMissingToken()
    {
        // Given
        var client = new TestableHttpClient(null, "localhost:8080");

        // When/Then
        var act = async () => await client.FetchMetadataAsync();
        await act.Should().ThrowAsync<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataToken}*");
    }

    [Fact]
    public async Task FetchMetadataAsync_Should_ThrowOnMissingApi()
    {
        // Given
        var client = new TestableHttpClient(TestToken, null);

        // When/Then
        var act = async () => await client.FetchMetadataAsync();
        await act.Should().ThrowAsync<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataApi}*");
    }

    [Fact]
    public async Task FetchMetadataAsync_Should_ThrowOnEmptyToken()
    {
        // Given
        var client = new TestableHttpClient("", "localhost:8080");

        // When/Then
        var act = async () => await client.FetchMetadataAsync();
        await act.Should().ThrowAsync<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataToken}*");
    }

    [Fact]
    public async Task FetchMetadataAsync_Should_ThrowOnEmptyApi()
    {
        // Given
        var client = new TestableHttpClient(TestToken, "");

        // When/Then
        var act = async () => await client.FetchMetadataAsync();
        await act.Should().ThrowAsync<LambdaMetadataException>()
            .WithMessage($"*{LambdaMetadataHttpClient.EnvMetadataApi}*");
    }

    [Fact]
    public async Task FetchMetadataAsync_Should_SupportCancellation()
    {
        // Given
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var client = new TestableHttpClient(TestToken, "localhost:8080");

        // When/Then
        var act = async () => await client.FetchMetadataAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Environment Variable Tests

    [Fact]
    public void GetEnvironmentVariable_Should_ReturnCorrectValues()
    {
        // Given
        var client = new TestableHttpClient(TestToken, "localhost:9000");

        // When/Then
        client.TestGetEnvironmentVariable(LambdaMetadataHttpClient.EnvMetadataToken)
            .Should().Be(TestToken);
        client.TestGetEnvironmentVariable(LambdaMetadataHttpClient.EnvMetadataApi)
            .Should().Be("localhost:9000");
        client.TestGetEnvironmentVariable("UNKNOWN_VAR")
            .Should().BeNull();
    }

    #endregion

    /// <summary>
    /// Testable HTTP client that allows overriding environment variables.
    /// </summary>
    private class TestableHttpClient : LambdaMetadataHttpClient
    {
        private readonly string? _token;
        private readonly string? _api;

        public TestableHttpClient(string? token, string? api)
        {
            _token = token;
            _api = api;
        }

        internal override string? GetEnvironmentVariable(string name)
        {
            return name switch
            {
                EnvMetadataToken => _token,
                EnvMetadataApi => _api,
                _ => null
            };
        }

        public string? TestGetEnvironmentVariable(string name) => GetEnvironmentVariable(name);
    }
}
