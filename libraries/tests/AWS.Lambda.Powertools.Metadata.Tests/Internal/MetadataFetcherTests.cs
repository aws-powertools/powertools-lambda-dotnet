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
using AWS.Lambda.Powertools.Metadata.Exceptions;
using AWS.Lambda.Powertools.Metadata.Internal;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Metadata.Tests.Internal;

public class MetadataFetcherTests : IDisposable
{
    public MetadataFetcherTests()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_TOKEN", "test-token");
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_API", "localhost:8080");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_TOKEN", null);
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_API", null);
    }

    [Fact]
    public void Fetch_ThrowsWhenTokenMissing()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_TOKEN", null);

        var fetcher = new MetadataFetcher();
        var act = () => fetcher.Fetch();
        act.Should().Throw<LambdaMetadataException>()
            .WithMessage("*AWS_LAMBDA_METADATA_TOKEN*");
    }

    [Fact]
    public void Fetch_ThrowsWhenApiMissing()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_METADATA_API", null);

        var fetcher = new MetadataFetcher();
        var act = () => fetcher.Fetch();
        act.Should().Throw<LambdaMetadataException>()
            .WithMessage("*AWS_LAMBDA_METADATA_API*");
    }

    [Fact]
    public void Fetch_ReturnsMetadata_WhenSuccessful()
    {
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"AvailabilityZoneID\":\"use1-az1\"}")
        });
        var fetcher = new MetadataFetcher(new HttpClient(handler));

        var result = fetcher.Fetch();

        result.AvailabilityZoneId.Should().Be("use1-az1");
    }

    [Fact]
    public void Fetch_ThrowsWithStatusCode_WhenHttpError()
    {
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("access denied")
        });
        var fetcher = new MetadataFetcher(new HttpClient(handler));

        var act = () => fetcher.Fetch();

        act.Should().Throw<LambdaMetadataException>()
            .Where(e => e.Message.Contains("403") && e.Message.Contains("access denied"))
            .Where(e => e.StatusCode == 403);
    }

    [Fact]
    public void Fetch_WrapsGenericException()
    {
        var handler = new MockHandler(new InvalidOperationException("connection refused"));
        var fetcher = new MetadataFetcher(new HttpClient(handler));

        var act = () => fetcher.Fetch();

        act.Should().Throw<LambdaMetadataException>()
            .WithMessage("*connection refused*")
            .WithInnerException<InvalidOperationException>();
    }

    [Fact]
    public void Fetch_ThrowsWhenDeserializationReturnsNull()
    {
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        });
        var fetcher = new MetadataFetcher(new HttpClient(handler));

        var act = () => fetcher.Fetch();

        act.Should().Throw<LambdaMetadataException>()
            .WithMessage("*deserialize*");
    }

    [Fact]
    public void Fetch_SetsAuthorizationHeader()
    {
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"AvailabilityZoneID\":\"use1-az1\"}")
        });
        var fetcher = new MetadataFetcher(new HttpClient(handler));

        fetcher.Fetch();

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("test-token");
    }

    [Fact]
    public void Fetch_UsesCorrectUrl()
    {
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"AvailabilityZoneID\":\"use1-az1\"}")
        });
        var fetcher = new MetadataFetcher(new HttpClient(handler));

        fetcher.Fetch();

        handler.LastRequest!.RequestUri!.ToString()
            .Should().Be("http://localhost:8080/2026-01-15/metadata/execution-environment");
    }

    [Fact]
    public void Fetch_RethrowsLambdaMetadataException()
    {
        var handler = new MockHandler(new LambdaMetadataException("original error"));
        var fetcher = new MetadataFetcher(new HttpClient(handler));

        var act = () => fetcher.Fetch();

        act.Should().Throw<LambdaMetadataException>()
            .WithMessage("original error")
            .Where(e => e.InnerException == null);
    }

    private class MockHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;
        public HttpRequestMessage? LastRequest { get; private set; }

        public MockHandler(HttpResponseMessage response) => _response = response;
        public MockHandler(Exception exception) => _exception = exception;

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (_exception is not null) throw _exception;
            return _response!;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(Send(request, cancellationToken));
    }
}
