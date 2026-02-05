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

using System.Text.Json;
using AWS.Lambda.Powertools.Metadata.Internal;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Metadata.Tests;

public class LambdaMetadataTests
{
    [Fact]
    public void DefaultConstructor_Should_CreateInstanceWithNullValues()
    {
        // When
        var metadata = new LambdaMetadata();

        // Then
        metadata.AvailabilityZoneId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAvailabilityZoneId_Should_SetValue()
    {
        // When
        var metadata = new LambdaMetadata("use1-az1");

        // Then
        metadata.AvailabilityZoneId.Should().Be("use1-az1");
    }

    [Fact]
    public void Deserialize_Should_MapJsonProperty()
    {
        // Given
        var json = """{"AvailabilityZoneID": "euw1-az3"}""";

        // When
        var metadata = JsonSerializer.Deserialize(json, LambdaMetadataSerializerContext.Default.LambdaMetadata);

        // Then
        metadata.Should().NotBeNull();
        metadata!.AvailabilityZoneId.Should().Be("euw1-az3");
    }

    [Fact]
    public void Deserialize_Should_IgnoreUnknownFields()
    {
        // Given
        var json = """{"AvailabilityZoneID": "apne1-az1", "UnknownField": "value", "AnotherField": 123}""";

        // When
        var metadata = JsonSerializer.Deserialize(json, LambdaMetadataSerializerContext.Default.LambdaMetadata);

        // Then
        metadata.Should().NotBeNull();
        metadata!.AvailabilityZoneId.Should().Be("apne1-az1");
    }
}
