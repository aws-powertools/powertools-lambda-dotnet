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

[Collection("LambdaMetadata")]
public class LambdaMetadataTests : IDisposable
{
    private readonly IMetadataFetcher _mockFetcher;

    public LambdaMetadataTests()
    {
        _mockFetcher = Substitute.For<IMetadataFetcher>();
        LambdaMetadata.SetFetcher(_mockFetcher);
    }

    public void Dispose()
    {
        LambdaMetadata.Reset();
    }

    [Fact]
    public void AvailabilityZoneId_ReturnsValue()
    {
        // Arrange
        _mockFetcher.Fetch().Returns(new MetadataValues("use1-az1"));

        // Act
        var result = LambdaMetadata.AvailabilityZoneId;

        // Assert
        result.Should().Be("use1-az1");
    }

    [Fact]
    public void AvailabilityZoneId_CachesValue()
    {
        // Arrange
        _mockFetcher.Fetch().Returns(new MetadataValues("use1-az1"));

        // Act
        var first = LambdaMetadata.AvailabilityZoneId;
        var second = LambdaMetadata.AvailabilityZoneId;

        // Assert
        first.Should().Be(second);
        _mockFetcher.Received(1).Fetch();
    }

    [Fact]
    public void AvailabilityZoneId_ThrowsOnError()
    {
        // Arrange
        _mockFetcher.Fetch().Returns(_ => throw new LambdaMetadataException("Test error"));

        // Act & Assert
        var act = () => LambdaMetadata.AvailabilityZoneId;
        act.Should().Throw<LambdaMetadataException>().WithMessage("Test error");
    }

    [Fact]
    public void AvailabilityZoneId_ThrowsWithStatusCode()
    {
        // Arrange
        _mockFetcher.Fetch().Returns(_ => throw new LambdaMetadataException("Server error", 500));

        // Act & Assert
        var act = () => LambdaMetadata.AvailabilityZoneId;
        act.Should().Throw<LambdaMetadataException>().Where(e => e.StatusCode == 500);
    }

    [Fact]
    public void Refresh_FetchesNewValue()
    {
        // Arrange
        _mockFetcher.Fetch().Returns(
            new MetadataValues("use1-az1"),
            new MetadataValues("use1-az2"));

        // Act
        var first = LambdaMetadata.AvailabilityZoneId;
        LambdaMetadata.Refresh();
        var second = LambdaMetadata.AvailabilityZoneId;

        // Assert
        first.Should().Be("use1-az1");
        second.Should().Be("use1-az2");
        _mockFetcher.Received(2).Fetch();
    }

    [Fact]
    public void AvailabilityZoneId_ReturnsNullWhenNotSet()
    {
        // Arrange
        _mockFetcher.Fetch().Returns(new MetadataValues(null));

        // Act
        var result = LambdaMetadata.AvailabilityZoneId;

        // Assert
        result.Should().BeNull();
    }
}
