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

using AWS.Lambda.Powertools.Metadata.Internal;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metadata.Tests;

[Collection("LambdaMetadataClient")]
public class LambdaMetadataClientConcurrencyTests : IDisposable
{
    private readonly ILambdaMetadataHttpClient _mockHttpClient;

    public LambdaMetadataClientConcurrencyTests()
    {
        _mockHttpClient = Substitute.For<ILambdaMetadataHttpClient>();
        LambdaMetadataClient.SetHttpClient(_mockHttpClient);
    }

    public void Dispose()
    {
        LambdaMetadataClient.ResetCache();
    }

    [Fact]
    public async Task Get_Should_BeThreadSafe()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az1");
        _mockHttpClient.FetchMetadata().Returns(metadata);

        const int threadCount = 50;
        var startSignal = new TaskCompletionSource<bool>();
        var tasks = new List<Task<LambdaMetadata>>();

        // When - all threads try to get metadata simultaneously
        for (var i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                return LambdaMetadataClient.Get();
            }));
        }

        startSignal.SetResult(true);
        var results = await Task.WhenAll(tasks);

        // Then - all threads should get the same instance
        var firstResult = results[0];
        foreach (var result in results)
        {
            result.Should().BeSameAs(firstResult);
            result.AvailabilityZoneId.Should().Be("use1-az1");
        }

        // Should only fetch once despite concurrent access
        _mockHttpClient.Received(1).FetchMetadata();
    }

    [Fact]
    public async Task GetAsync_Should_BeThreadSafe()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az2");
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>()).Returns(metadata);

        const int threadCount = 50;
        var startSignal = new TaskCompletionSource<bool>();
        var tasks = new List<Task<LambdaMetadata>>();

        // When - all tasks try to get metadata simultaneously
        for (var i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                return await LambdaMetadataClient.GetAsync();
            }));
        }

        startSignal.SetResult(true);
        var results = await Task.WhenAll(tasks);

        // Then - all tasks should get the same instance
        var firstResult = results[0];
        foreach (var result in results)
        {
            result.Should().BeSameAs(firstResult);
            result.AvailabilityZoneId.Should().Be("use1-az2");
        }

        // Should only fetch once despite concurrent access
        await _mockHttpClient.Received(1).FetchMetadataAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MixedSyncAndAsync_Should_BeThreadSafe()
    {
        // Given
        var metadata = new LambdaMetadata("use1-az3");
        _mockHttpClient.FetchMetadata().Returns(metadata);
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>()).Returns(metadata);

        const int threadCount = 25;
        var startSignal = new TaskCompletionSource<bool>();
        var syncTasks = new List<Task<LambdaMetadata>>();
        var asyncTasks = new List<Task<LambdaMetadata>>();

        // When - mix of sync and async calls
        for (var i = 0; i < threadCount; i++)
        {
            syncTasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                return LambdaMetadataClient.Get();
            }));

            asyncTasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                return await LambdaMetadataClient.GetAsync();
            }));
        }

        startSignal.SetResult(true);
        
        var syncResults = await Task.WhenAll(syncTasks);
        var asyncResults = await Task.WhenAll(asyncTasks);

        // Then - all should get the same instance
        var allResults = syncResults.Concat(asyncResults).ToList();
        var firstResult = allResults[0];
        
        foreach (var result in allResults)
        {
            result.Should().BeSameAs(firstResult);
            result.AvailabilityZoneId.Should().Be("use1-az3");
        }
    }

    [Fact]
    public async Task ConcurrentRefresh_Should_BeThreadSafe()
    {
        // Given
        var callCount = 0;
        _mockHttpClient.FetchMetadata().Returns(_ =>
        {
            var count = Interlocked.Increment(ref callCount);
            return new LambdaMetadata($"use1-az{count}");
        });

        const int threadCount = 10;
        var startSignal = new TaskCompletionSource<bool>();
        var tasks = new List<Task<LambdaMetadata>>();

        // When - concurrent refresh calls
        for (var i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                return LambdaMetadataClient.Refresh();
            }));
        }

        startSignal.SetResult(true);
        var results = await Task.WhenAll(tasks);

        // Then - all results should be valid (not null)
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.AvailabilityZoneId.Should().StartWith("use1-az");
        }
    }

    [Fact]
    public async Task ConcurrentRefreshAsync_Should_BeThreadSafe()
    {
        // Given
        var callCount = 0;
        _mockHttpClient.FetchMetadataAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var count = Interlocked.Increment(ref callCount);
            return Task.FromResult(new LambdaMetadata($"use1-az{count}"));
        });

        const int threadCount = 10;
        var startSignal = new TaskCompletionSource<bool>();
        var tasks = new List<Task<LambdaMetadata>>();

        // When - concurrent async refresh calls
        for (var i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                return await LambdaMetadataClient.RefreshAsync();
            }));
        }

        startSignal.SetResult(true);
        var results = await Task.WhenAll(tasks);

        // Then - all results should be valid (not null)
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.AvailabilityZoneId.Should().StartWith("use1-az");
        }
    }
}
