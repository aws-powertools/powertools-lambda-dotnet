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
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

[Collection("Sequential")]
public class ContextInjectionTests
{
    private readonly IPowertoolsConfigurations _mockConfigurations;
    private readonly TypedSqsBatchProcessor _processor;
    private readonly ILambdaContext _mockContext;

    public ContextInjectionTests()
    {
        _mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        _processor = new TypedSqsBatchProcessor(_mockConfigurations);
        _mockContext = Substitute.For<ILambdaContext>();
        _mockContext.AwsRequestId.Returns("test-request-id");
        _mockContext.RemainingTime.Returns(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void RequiresContext_WithContextParameter_ReturnsTrue()
    {
        // Arrange
        TypedRecordHandlerWithContext<TestMessage> handler = (data, context, cancellationToken) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act
        var requiresContext = ContextInjectionHelper.RequiresContext(handler);

        // Assert
        Assert.True(requiresContext);
    }

    [Fact]
    public void RequiresContext_WithoutContextParameter_ReturnsFalse()
    {
        // Arrange
        TypedRecordHandler<TestMessage> handler = (data, cancellationToken) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act
        var requiresContext = ContextInjectionHelper.RequiresContext(handler);

        // Assert
        Assert.False(requiresContext);
    }

    [Fact]
    public void RequiresContext_WithNullHandler_ReturnsFalse()
    {
        // Act
        var requiresContext = ContextInjectionHelper.RequiresContext(null);

        // Assert
        Assert.False(requiresContext);
    }

    [Fact]
    public async Task InvokeWithContextInjection_HandlerWithContext_PassesContextCorrectly()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Test" };
        var contextReceived = false;
        string requestIdReceived = null;

        TypedRecordHandlerWithContext<TestMessage> handler = (data, context, cancellationToken) =>
        {
            contextReceived = context != null;
            requestIdReceived = context?.AwsRequestId;
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = await ContextInjectionHelper.InvokeWithContextInjection(
            handler, testData, _mockContext, CancellationToken.None);

        // Assert
        Assert.Equal(RecordHandlerResult.None, result);
        Assert.True(contextReceived);
        Assert.Equal("test-request-id", requestIdReceived);
    }

    [Fact]
    public async Task InvokeWithContextInjection_HandlerWithoutContext_DoesNotPassContext()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Test" };
        var handlerCalled = false;

        TypedRecordHandler<TestMessage> handler = (data, cancellationToken) =>
        {
            handlerCalled = true;
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = await ContextInjectionHelper.InvokeWithContextInjection(
            handler, testData, _mockContext, CancellationToken.None);

        // Assert
        Assert.Equal(RecordHandlerResult.None, result);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task InvokeWithContextInjection_HandlerWithNullContext_HandlesGracefully()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Test" };
        var contextReceived = false;

        TypedRecordHandlerWithContext<TestMessage> handler = (data, context, cancellationToken) =>
        {
            contextReceived = context != null;
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = await ContextInjectionHelper.InvokeWithContextInjection(
            handler, testData, null, CancellationToken.None);

        // Assert
        Assert.Equal(RecordHandlerResult.None, result);
        Assert.False(contextReceived);
    }

    [Fact]
    public async Task InvokeWithContextInjection_SimpleHandler_WorksCorrectly()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Test" };
        var handlerCalled = false;

        SimpleTypedRecordHandler<TestMessage> handler = (data) =>
        {
            handlerCalled = true;
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = await ContextInjectionHelper.InvokeWithContextInjection(
            handler, testData, _mockContext, CancellationToken.None);

        // Assert
        Assert.Equal(RecordHandlerResult.None, result);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task InvokeWithContextInjection_SimpleHandlerWithContext_PassesContext()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Test" };
        var contextReceived = false;

        SimpleTypedRecordHandlerWithContext<TestMessage> handler = (data, context) =>
        {
            contextReceived = context != null;
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = await ContextInjectionHelper.InvokeWithContextInjection(
            handler, testData, _mockContext, CancellationToken.None);

        // Assert
        Assert.Equal(RecordHandlerResult.None, result);
        Assert.True(contextReceived);
    }

    [Fact]
    public void ValidateHandlerSignature_ValidHandler_DoesNotThrow()
    {
        // Arrange
        TypedRecordHandler<TestMessage> handler = (data, cancellationToken) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act & Assert
        ContextInjectionHelper.ValidateHandlerSignature<TestMessage>(handler);
    }

    [Fact]
    public void ValidateHandlerSignature_HandlerWithWrongDataType_ThrowsException()
    {
        // Arrange
        TypedRecordHandler<string> handler = (data, cancellationToken) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ContextInjectionHelper.ValidateHandlerSignature<TestMessage>(handler));
        
        Assert.Contains("First parameter of handler method must be of type 'TestMessage'", exception.Message);
    }

    [Fact]
    public void ValidateHandlerSignature_HandlerWithUnsupportedParameter_ThrowsException()
    {
        // Arrange
        Func<TestMessage, int, Task<RecordHandlerResult>> handler = (data, unsupported) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ContextInjectionHelper.ValidateHandlerSignature<TestMessage>(handler));
        
        Assert.Contains("Unsupported parameter type 'Int32'", exception.Message);
    }

    [Fact]
    public void ValidateHandlerSignature_HandlerWithWrongReturnType_ThrowsException()
    {
        // Arrange
        Func<TestMessage, string> handler = (data) => "invalid";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ContextInjectionHelper.ValidateHandlerSignature<TestMessage>(handler));
        
        Assert.Contains("Handler method must return", exception.Message);
    }

    [Fact]
    public void CreateContextAwareWrapper_ValidHandler_CreatesWrapper()
    {
        // Arrange
        TypedRecordHandler<TestMessage> handler = (data, cancellationToken) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act
        var wrapper = ContextInjectionHelper.CreateContextAwareWrapper<TestMessage>(handler);

        // Assert
        Assert.NotNull(wrapper);
        Assert.IsAssignableFrom<ITypedRecordHandlerWithContext<TestMessage>>(wrapper);
    }

    [Fact]
    public async Task CreateContextAwareWrapper_ExecutesHandlerCorrectly()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Test" };
        var handlerCalled = false;

        TypedRecordHandler<TestMessage> handler = (data, cancellationToken) =>
        {
            handlerCalled = true;
            Assert.Equal(testData.Id, data.Id);
            Assert.Equal(testData.Name, data.Name);
            return Task.FromResult(RecordHandlerResult.None);
        };

        var wrapper = ContextInjectionHelper.CreateContextAwareWrapper<TestMessage>(handler);

        // Act
        var result = await wrapper.HandleAsync(testData, _mockContext, CancellationToken.None);

        // Assert
        Assert.Equal(RecordHandlerResult.None, result);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task ProcessAsync_WithDelegateHandler_AutomaticallyInjectsContext()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var contextReceived = false;
        string requestIdReceived = null;

        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        // Handler that expects context
        Func<TestMessage, ILambdaContext, CancellationToken, Task<RecordHandlerResult>> handler = 
            (data, context, cancellationToken) =>
            {
                contextReceived = context != null;
                requestIdReceived = context?.AwsRequestId;
                return Task.FromResult(RecordHandlerResult.None);
            };

        // Act
        var result = await _processor.ProcessAsync<TestMessage>(@event, handler, _mockContext);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.True(contextReceived);
        Assert.Equal("test-request-id", requestIdReceived);
    }

    [Fact]
    public async Task ProcessAsync_WithDelegateHandlerWithoutContext_DoesNotRequireContext()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "No Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var handlerCalled = false;

        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        // Handler that doesn't expect context
        Func<TestMessage, CancellationToken, Task<RecordHandlerResult>> handler = 
            (data, cancellationToken) =>
            {
                handlerCalled = true;
                return Task.FromResult(RecordHandlerResult.None);
            };

        // Act
        var result = await _processor.ProcessAsync<TestMessage>(@event, handler, null);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task ProcessAsync_WithSimpleDelegateHandler_WorksCorrectly()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Simple Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var handlerCalled = false;

        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        // Simple handler with just data parameter
        Func<TestMessage, Task<RecordHandlerResult>> handler = 
            (data) =>
            {
                handlerCalled = true;
                return Task.FromResult(RecordHandlerResult.None);
            };

        // Act
        var result = await _processor.ProcessAsync<TestMessage>(@event, handler);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.True(handlerCalled);
    }

    [Fact]
    public void ProcessAsync_WithInvalidDelegateHandler_ThrowsException()
    {
        // Arrange
        var @event = new SQSEvent { Records = new List<SQSEvent.SQSMessage>() };

        // Handler with wrong parameter type
        Func<string, Task<RecordHandlerResult>> handler = (data) => Task.FromResult(RecordHandlerResult.None);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(async () => 
            await _processor.ProcessAsync<TestMessage>(@event, handler));
    }

    public class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}