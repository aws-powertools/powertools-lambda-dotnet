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

[Collection("LambdaMetadata")]
public class LambdaMetadataConcurrencyTests : IDisposable
{
    private readonly IMetadataFetcher _mockFetcher;

    public LambdaMetadataConcurrencyTests()
    {
        _mockFetcher = Substitute.For<IMetadataFetcher>();
        LambdaMetadata.SetFetcher(_mockFetcher);
    }

    public void Dispose()
    {
        LambdaMetadata.Reset();
    }

    [Fact]
    public async Task AvailabilityZoneId_IsThreadSafe()
    {
        // Arrange
        _mockFetcher.Fetch().Returns(new MetadataValues("use1-az1"));

        const int threadCount = 50;
        var startSignal = new TaskCompletionSource<bool>();
        var tasks = new List<Task<string?>>();

        // Act
        for (var i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                return LambdaMetadata.AvailabilityZoneId;
            }));
        }

        startSignal.SetResult(true);
        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            result.Should().Be("use1-az1");
        }

        _mockFetcher.Received(1).Fetch();
    }
}
