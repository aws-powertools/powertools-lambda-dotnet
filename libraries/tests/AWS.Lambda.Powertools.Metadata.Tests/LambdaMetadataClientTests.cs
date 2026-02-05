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
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metadata.Tests;

[Collection("LambdaMetadataClient")]
public class LambdaMetadataClientTests : IDisposable
{
    private readonly ILambdaMetadataHttpClient _mockHttpClient;

    public LambdaMetadataClientTests()
    {
        _mockHttpClient = Substitute.For<ILambdaMetadataHttpClient>();
        LambdaMetadataClient.SetHttpClient(_mockHttpClient);
    }

    public void Dispose()
    {
        LambdaMetadataClient.ResetCache();
    }

    #region Synchronous Get Tests

    [Fact]
    public void Get_Should_ReturnMetadata()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadata().Returns(metadata);

        // When
        var result = LambdaMetadataClient.Get();

        // Then
        result.Should().NotBeNull();
        result.AvailabilityZoneId.Should().Be("use1-az1");
    }

    [Fact]
    public void Get_Should_CacheMetadata()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadata().Returns(metadata);

        // When
        var first = LambdaMetadataClient.Get();
        var second = LambdaMetadataClient.Get();

        // Then
        first.Should().BeSameAs(second);
        _mockHttpClient.Received(1).FetchMetadata();
    }

    [Fact]
    public void Get_Should_ThrowExceptionOnError()
    {
        // Given
        _mockHttpClient.FetchMetadata().Returns(_ => throw new LambdaMetadataException("Test error"));

        // When/Then
        var act = () => LambdaMetadataClient.Get();
        act.Should().Throw<LambdaMetadataException>()
            .WithMessage("Test error");
    }

    [Fact]
    public void Get_Should_ThrowExceptionWithStatusCode()
    {
        // Given
        _mockHttpClient.FetchMetadata().Returns(_ => throw new LambdaMetadataException("Server error", 500));

        // When/Then
        var act = () => LambdaMetadataClient.Get();
        act.Should().Throw<LambdaMetadataException>()
            .Where(e => e.StatusCode == 500);
    }

    #endregion

    #region Asynchronous GetAsync Tests

    [Fact]
    public async Task GetAsync_Should_ReturnMetadata()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>()).Returns(metadata);

        // When
        var result = await LambdaMetadataClient.GetAsync();

        // Then
        result.Should().NotBeNull();
        result.AvailabilityZoneId.Should().Be("use1-az1");
    }

    [Fact]
    public async Task GetAsync_Should_CacheMetadata()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>()).Returns(metadata);

        // When
        var first = await LambdaMetadataClient.GetAsync();
        var second = await LambdaMetadataClient.GetAsync();

        // Then
        first.Should().BeSameAs(second);
        await _mockHttpClient.Received(1).FetchMetadataAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_Should_ThrowExceptionOnError()
    {
        // Given
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>())
            .Returns<LambdaMetadata>(_ => throw new LambdaMetadataException("Test error"));

        // When/Then
        var act = async () => await LambdaMetadataClient.GetAsync();
        await act.Should().ThrowAsync<LambdaMetadataException>()
            .WithMessage("Test error");
    }

    [Fact]
    public async Task GetAsync_Should_SupportCancellation()
    {
        // Given
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>())
            .Returns<LambdaMetadata>(_ => throw new OperationCanceledException());

        // When/Then
        var act = async () => await LambdaMetadataClient.GetAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Synchronous Refresh Tests

    [Fact]
    public void Refresh_Should_FetchNewMetadata()
    {
        // Given
        var metadata1 = new LambdaMetadata("use1-az1");
        var metadata2 = new LambdaMetadata("use1-az2");
        _mockHttpClient.FetchMetadata().Returns(metadata1, metadata2);

        // When
        var first = LambdaMetadataClient.Get();
        var refreshed = LambdaMetadataClient.Refresh();

        // Then
        first.AvailabilityZoneId.Should().Be("use1-az1");
        refreshed.AvailabilityZoneId.Should().Be("use1-az2");
        _mockHttpClient.Received(2).FetchMetadata();
    }

    [Fact]
    public void Refresh_Should_UpdateCache()
    {
        // Given
        var metadata1 = new LambdaMetadata("use1-az1");
        var metadata2 = new LambdaMetadata("use1-az2");
        _mockHttpClient.FetchMetadata().Returns(metadata1, metadata2);

        // When
        LambdaMetadataClient.Get();
        var refreshed = LambdaMetadataClient.Refresh();
        var afterRefresh = LambdaMetadataClient.Get();

        // Then
        refreshed.Should().BeSameAs(afterRefresh);
        refreshed.AvailabilityZoneId.Should().Be("use1-az2");
    }

    #endregion

    #region Asynchronous RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_Should_FetchNewMetadata()
    {
        // Given
        var metadata1 = new LambdaMetadata("use1-az1");
        var metadata2 = new LambdaMetadata("use1-az2");
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>()).Returns(metadata1, metadata2);

        // When
        var first = await LambdaMetadataClient.GetAsync();
        var refreshed = await LambdaMetadataClient.RefreshAsync();

        // Then
        first.AvailabilityZoneId.Should().Be("use1-az1");
        refreshed.AvailabilityZoneId.Should().Be("use1-az2");
        await _mockHttpClient.Received(2).FetchMetadataAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_Should_SupportCancellation()
    {
        // Given
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>())
            .Returns<LambdaMetadata>(_ => throw new OperationCanceledException());

        // When/Then
        var act = async () => await LambdaMetadataClient.RefreshAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Cache Reset Tests

    [Fact]
    public void ResetCache_Should_InvalidateCache()
    {
        // Given
        var metadata1 = new LambdaMetadata("use1-az1");
        var metadata2 = new LambdaMetadata("use1-az2");
        _mockHttpClient.FetchMetadata().Returns(metadata1, metadata2);

        // When
        var first = LambdaMetadataClient.Get();
        LambdaMetadataClient.ResetCache();
        var afterReset = LambdaMetadataClient.Get();

        // Then
        first.AvailabilityZoneId.Should().Be("use1-az1");
        afterReset.AvailabilityZoneId.Should().Be("use1-az2");
        _mockHttpClient.Received(2).FetchMetadata();
    }

    [Fact]
    public void ResetCache_Should_AllowNewFetch()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadata().Returns(metadata);

        // When
        LambdaMetadataClient.Get();
        LambdaMetadataClient.ResetCache();
        LambdaMetadataClient.Get();

        // Then
        _mockHttpClient.Received(2).FetchMetadata();
    }

    #endregion

    #region Mixed Sync/Async Tests

    [Fact]
    public async Task Get_And_GetAsync_Should_ShareCache()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadata().Returns(metadata);

        // When - sync first
        var syncResult = LambdaMetadataClient.Get();
        var asyncResult = await LambdaMetadataClient.GetAsync();

        // Then
        syncResult.Should().BeSameAs(asyncResult);
        _mockHttpClient.Received(1).FetchMetadata();
        await _mockHttpClient.DidNotReceive().FetchMetadataAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_And_Get_Should_ShareCache()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>()).Returns(metadata);

        // When - async first
        var asyncResult = await LambdaMetadataClient.GetAsync();
        var syncResult = LambdaMetadataClient.Get();

        // Then
        asyncResult.Should().BeSameAs(syncResult);
        await _mockHttpClient.Received(1).FetchMetadataAsync(Arg.Any<CancellationToken>());
        _mockHttpClient.DidNotReceive().FetchMetadata();
    }

    #endregion
}
