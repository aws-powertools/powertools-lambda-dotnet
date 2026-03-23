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
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Metadata.Tests.Exceptions;

public class LambdaMetadataExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_Should_SetMessage()
    {
        // When
        var exception = new LambdaMetadataException("Test message");

        // Then
        exception.Message.Should().Be("Test message");
        exception.StatusCode.Should().Be(-1);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndCause_Should_SetBoth()
    {
        // Given
        var cause = new InvalidOperationException("Root cause");

        // When
        var exception = new LambdaMetadataException("Test message", cause);

        // Then
        exception.Message.Should().Be("Test message");
        exception.InnerException.Should().BeSameAs(cause);
        exception.StatusCode.Should().Be(-1);
    }

    [Fact]
    public void Constructor_WithMessageAndStatusCode_Should_SetBoth()
    {
        // When
        var exception = new LambdaMetadataException("Test message", 500);

        // Then
        exception.Message.Should().Be("Test message");
        exception.StatusCode.Should().Be(500);
        exception.InnerException.Should().BeNull();
    }
}
