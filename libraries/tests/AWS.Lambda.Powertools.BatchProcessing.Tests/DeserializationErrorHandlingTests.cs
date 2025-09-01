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

using System;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

public class DeserializationErrorHandlingTests
{
    [Fact]
    public void DeserializationException_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var recordData = "{\"invalid\": json}";
        var targetType = typeof(TestModel);
        var recordId = "test-record-123";
        var innerException = new ArgumentException("Invalid JSON");

        // Act
        var exception = new DeserializationException(recordData, targetType, recordId, innerException);

        // Assert
        Assert.Equal(recordData, exception.RecordData);
        Assert.Equal(targetType, exception.TargetType);
        Assert.Equal(recordId, exception.RecordId);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Contains("Failed to deserialize record 'test-record-123' to type 'TestModel'", exception.Message);
    }

    [Fact]
    public void DeserializationException_WithoutRecordId_UsesUnknownAsDefault()
    {
        // Arrange
        var recordData = "{\"invalid\": json}";
        var targetType = typeof(TestModel);
        var innerException = new ArgumentException("Invalid JSON");

        // Act
        var exception = new DeserializationException(recordData, targetType, innerException);

        // Assert
        Assert.Equal(recordData, exception.RecordData);
        Assert.Equal(targetType, exception.TargetType);
        Assert.Equal("Unknown", exception.RecordId);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Contains("Failed to deserialize record 'Unknown' to type 'TestModel'", exception.Message);
    }

    [Fact]
    public void DeserializationException_WithNullTargetType_HandlesGracefully()
    {
        // Arrange
        var recordData = "{\"invalid\": json}";
        Type targetType = null;
        var recordId = "test-record-123";
        var innerException = new ArgumentException("Invalid JSON");

        // Act
        var exception = new DeserializationException(recordData, targetType, recordId, innerException);

        // Assert
        Assert.Equal(recordData, exception.RecordData);
        Assert.Null(exception.TargetType);
        Assert.Equal(recordId, exception.RecordId);
        Assert.Contains("Failed to deserialize record 'test-record-123' to type 'Unknown'", exception.Message);
    }

    [Fact]
    public void DeserializationException_WithMessageOnly_CreatesCorrectly()
    {
        // Arrange
        var message = "Custom error message";

        // Act
        var exception = new DeserializationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void DeserializationException_WithMessageAndInnerException_CreatesCorrectly()
    {
        // Arrange
        var message = "Custom error message";
        var innerException = new ArgumentException("Inner error");

        // Act
        var exception = new DeserializationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(DeserializationErrorPolicy.FailRecord)]
    [InlineData(DeserializationErrorPolicy.IgnoreRecord)]
    [InlineData(DeserializationErrorPolicy.CustomHandler)]
    public void DeserializationErrorPolicy_AllValuesAreDefined(DeserializationErrorPolicy policy)
    {
        // Act & Assert - Should not throw
        var policyName = policy.ToString();
        Assert.NotNull(policyName);
        Assert.NotEmpty(policyName);
    }

    [Fact]
    public void DeserializationOptions_DefaultErrorPolicy_IsFailRecord()
    {
        // Act
        var options = new DeserializationOptions();

        // Assert
        Assert.Equal(DeserializationErrorPolicy.FailRecord, options.ErrorPolicy);
    }

    [Fact]
    public void DeserializationOptions_CanSetErrorPolicy()
    {
        // Arrange
        var options = new DeserializationOptions();

        // Act
        options.ErrorPolicy = DeserializationErrorPolicy.IgnoreRecord;

        // Assert
        Assert.Equal(DeserializationErrorPolicy.IgnoreRecord, options.ErrorPolicy);
    }

    [Fact]
    public void DeserializationOptions_BackwardCompatibility_IgnoreDeserializationErrorsStillWorks()
    {
        // Arrange
        var options = new DeserializationOptions();

        // Act
        #pragma warning disable CS0618 // Type or member is obsolete
        options.IgnoreDeserializationErrors = true;
        #pragma warning restore CS0618 // Type or member is obsolete

        // Assert
        #pragma warning disable CS0618 // Type or member is obsolete
        Assert.True(options.IgnoreDeserializationErrors);
        #pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public async Task TestDeserializationErrorHandler_HandleDeserializationError_ReturnsExpectedResult()
    {
        // Arrange
        var handler = new TestDeserializationErrorHandler();
        var record = new TestRecord { Id = "test-123", Data = "test-data" };
        var exception = new DeserializationException("Test error");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await handler.HandleDeserializationError(record, exception, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Error handling record: test-123", result.Data);
    }

    [Fact]
    public async Task TestDeserializationErrorHandler_WithNullRecord_HandlesGracefully()
    {
        // Arrange
        var handler = new TestDeserializationErrorHandler();
        TestRecord record = null;
        var exception = new DeserializationException("Test error");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await handler.HandleDeserializationError(record, exception, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Error handling record: unknown", result.Data);
    }

    [Fact]
    public async Task TestDeserializationErrorHandler_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var handler = new TestDeserializationErrorHandler();
        var record = new TestRecord { Id = "test-123", Data = "test-data" };
        var exception = new DeserializationException("Test error");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleDeserializationError(record, exception, cancellationTokenSource.Token));
    }

    // Test models and helpers
    public class TestModel
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class TestRecord
    {
        public string Id { get; set; }
        public string Data { get; set; }
    }

    public class TestDeserializationErrorHandler : IDeserializationErrorHandler<TestRecord>
    {
        public Task<RecordHandlerResult> HandleDeserializationError(TestRecord record, Exception exception, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recordId = record?.Id ?? "unknown";
            return Task.FromResult(RecordHandlerResult.FromData($"Error handling record: {recordId}"));
        }
    }
}