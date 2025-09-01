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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

[Collection("Sequential")]
public class TypedSqsBatchProcessorTests
{
    private readonly IPowertoolsConfigurations _mockConfigurations;
    private readonly TypedSqsBatchProcessor _processor;

    public TypedSqsBatchProcessorTests()
    {
        _mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        _processor = new TypedSqsBatchProcessor(_mockConfigurations);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandler_DeserializesAndProcessesSuccessfully()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Test Message" };
        var messageBody = JsonSerializer.Serialize(testData);
        
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

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContext_PassesContextCorrectly()
    {
        // Arrange
        var testData = new TestMessage { Id = 2, Name = "Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var context = Substitute.For<ILambdaContext>();
        context.AwsRequestId.Returns("test-request-id");
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-2",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, context);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Is<ILambdaContext>(c => c.AwsRequestId == "test-request-id"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithDeserializationOptions_UsesCustomOptions()
    {
        // Arrange
        var testData = new TestMessage { Id = 3, Name = "Custom Options Test" };
        var messageBody = JsonSerializer.Serialize(testData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-3",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var deserializationOptions = new DeserializationOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithJsonSerializerContext_UsesAOTCompatibleDeserialization()
    {
        // Arrange
        var testData = new TestMessage { Id = 4, Name = "AOT Test" };
        var messageBody = JsonSerializer.Serialize(testData, TypedSqsTestJsonSerializerContext.Default.TestMessage);
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-4",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var deserializationOptions = new DeserializationOptions(TypedSqsTestJsonSerializerContext.Default);

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJson_FailsRecordByDefault()
    {
        // Arrange
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-5",
                    Body = "invalid json",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));
        
        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: 'msg-5'", exception.Message);
        Assert.Single(exception.InnerExceptions);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJsonAndIgnorePolicy_IgnoresRecord()
    {
        // Arrange
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-6",
                    Body = "invalid json",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var deserializationOptions = new DeserializationOptions
        {
            ErrorPolicy = DeserializationErrorPolicy.IgnoreRecord
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleRecords_ProcessesAllSuccessfully()
    {
        // Arrange
        var testData1 = new TestMessage { Id = 7, Name = "Message 1" };
        var testData2 = new TestMessage { Id = 8, Name = "Message 2" };
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-7",
                    Body = JsonSerializer.Serialize(testData1),
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                },
                new()
                {
                    MessageId = "msg-8",
                    Body = JsonSerializer.Serialize(testData2),
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Equal(2, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.Received(2).HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithHandlerException_FailsRecord()
    {
        // Arrange
        var testData = new TestMessage { Id = 9, Name = "Exception Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-9",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.WhenForAnyArgs(x => x.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => throw new InvalidOperationException("Handler failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));
        
        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: 'msg-9'", exception.Message);
        Assert.Single(exception.InnerExceptions);
    }

    [Fact]
    public async Task ProcessAsync_WithFifoQueue_UsesStopOnFirstFailurePolicy()
    {
        // Arrange
        var testData1 = new TestMessage { Id = 10, Name = "FIFO Message 1" };
        var testData2 = new TestMessage { Id = 11, Name = "FIFO Message 2" };
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-10",
                    Body = JsonSerializer.Serialize(testData1),
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue.fifo"
                },
                new()
                {
                    MessageId = "msg-11",
                    Body = JsonSerializer.Serialize(testData2),
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue.fifo"
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.WhenForAnyArgs(x => x.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                var data = callInfo.Arg<TestMessage>();
                if (data.Id == 10)
                {
                    throw new InvalidOperationException("First record failed");
                }
            });
        handler.HandleAsync(Arg.Is<TestMessage>(m => m.Id == 11), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));
        
        // Verify that the first record failed and the second was not processed
        Assert.Contains("Failed processing record: 'msg-10'", exception.Message);
        Assert.Contains("Record: 'msg-11' has not been processed", exception.Message);
        Assert.Equal(2, exception.InnerExceptions.Count);
    }

    [Fact]
    public async Task ProcessAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var testData = new TestMessage { Id = 12, Name = "Cancellation Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-12",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => 
            _processor.ProcessAsync(@event, handler, cancellationTokenSource.Token));
        
        // Verify the cancellation was the root cause
        Assert.Contains("Failed processing record: 'msg-12'", exception.Message);
        Assert.Single(exception.InnerExceptions);
        Assert.IsType<RecordProcessingException>(exception.InnerExceptions.First());
        Assert.IsType<OperationCanceledException>(exception.InnerExceptions.First().InnerException);
    }

    [Fact]
    public void TypedInstance_ReturnsSingletonInstance()
    {
        // Act
        var instance1 = TypedSqsBatchProcessor.TypedInstance;
        var instance2 = TypedSqsBatchProcessor.TypedInstance;

        // Assert
        Assert.Same(instance1, instance2);
        Assert.IsType<TypedSqsBatchProcessor>(instance1);
    }

    [Fact]
    public async Task ProcessAsync_WithNullContext_HandlesGracefully()
    {
        // Arrange
        var testData = new TestMessage { Id = 13, Name = "Null Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-13",
                    Body = messageBody,
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, context: null);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Any<TestMessage>(),
            Arg.Is<ILambdaContext>(c => c == null),
            Arg.Any<CancellationToken>());
    }

    // Test data classes
    public class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class UnregisteredSqsMessage
    {
        public string Value { get; set; }
    }

    #region AOT Compatibility Tests

    [Fact]
    public async Task ProcessAsync_WithUnregisteredTypeInContext_ThrowsAotTypeValidationException()
    {
        // Arrange
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = """{"Value":"test"}"""
                }
            }
        };

        var deserializationOptions = new DeserializationOptions(TypedSqsTestJsonSerializerContext.Default);
        var handler = Substitute.For<ITypedRecordHandler<UnregisteredSqsMessage>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AotTypeValidationException>(() =>
            _processor.ProcessAsync(@event, handler, deserializationOptions));

        Assert.Equal(typeof(UnregisteredSqsMessage), exception.TargetType);
        Assert.Contains("UnregisteredSqsMessage", exception.Message);
        Assert.Contains("JsonSerializable", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_WithContextHandler_ValidatesAotCompatibility()
    {
        // Arrange
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = """{"Value":"test"}"""
                }
            }
        };

        var context = Substitute.For<ILambdaContext>();
        var deserializationOptions = new DeserializationOptions(TypedSqsTestJsonSerializerContext.Default);
        var handler = Substitute.For<ITypedRecordHandlerWithContext<UnregisteredSqsMessage>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AotTypeValidationException>(() =>
            _processor.ProcessAsync(@event, handler, context, deserializationOptions));

        Assert.Equal(typeof(UnregisteredSqsMessage), exception.TargetType);
    }

    [Fact]
    public async Task ProcessAsync_WithValidAotContext_ProcessesSuccessfully()
    {
        // Arrange
        var testData = new TestMessage { Id = 5, Name = "AOT Valid Test" };
        var messageBody = JsonSerializer.Serialize(testData, TypedSqsTestJsonSerializerContext.Default.TestMessage);

        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = messageBody
                }
            }
        };

        var deserializationOptions = new DeserializationOptions(TypedSqsTestJsonSerializerContext.Default);
        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Id == 5 && m.Name == "AOT Valid Test"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Delegate Processing Tests

    [Fact]
    public async Task ProcessAsync_WithDelegate_ProcessesSuccessfully()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Delegate Test" };
        var messageBody = JsonSerializer.Serialize(testData);

        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = messageBody
                }
            }
        };

        var handlerCalled = false;
        Task<RecordHandlerResult> DelegateHandler(TestMessage message, CancellationToken ct)
        {
            handlerCalled = true;
            Assert.Equal(1, message.Id);
            Assert.Equal("Delegate Test", message.Name);
            return Task.FromResult(RecordHandlerResult.None);
        }

        // Act
        var result = await _processor.ProcessAsync<TestMessage>(@event, DelegateHandler);

        // Assert
        Assert.NotNull(result);
        Assert.True(handlerCalled);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
    }

    [Fact]
    public async Task ProcessAsync_WithDelegateAndContext_ProcessesSuccessfully()
    {
        // Arrange
        var testData = new TestMessage { Id = 2, Name = "Delegate Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);

        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = messageBody
                }
            }
        };

        var context = Substitute.For<ILambdaContext>();
        var handlerCalled = false;
        
        Task<RecordHandlerResult> DelegateHandler(TestMessage message, ILambdaContext ctx, CancellationToken ct)
        {
            handlerCalled = true;
            Assert.Equal(2, message.Id);
            Assert.Equal("Delegate Context Test", message.Name);
            Assert.Equal(context, ctx);
            return Task.FromResult(RecordHandlerResult.None);
        }

        // Act
        var result = await _processor.ProcessAsync<TestMessage>(@event, DelegateHandler, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(handlerCalled);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
    }

    [Fact]
    public async Task ProcessAsync_WithNullDelegate_ThrowsArgumentNullException()
    {
        // Arrange
        var @event = new SQSEvent { Records = new List<SQSEvent.SQSMessage>() };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.ProcessAsync<TestMessage>(@event, (Delegate)null));
    }

    [Fact]
    public async Task ProcessAsync_WithDelegateAndDeserializationOptions_ProcessesSuccessfully()
    {
        // Arrange
        var testData = new TestMessage { Id = 3, Name = "Delegate Options Test" };
        var messageBody = JsonSerializer.Serialize(testData);

        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = messageBody
                }
            }
        };

        var deserializationOptions = new DeserializationOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        };

        var handlerCalled = false;
        Task<RecordHandlerResult> DelegateHandler(TestMessage message, CancellationToken ct)
        {
            handlerCalled = true;
            Assert.Equal(3, message.Id);
            return Task.FromResult(RecordHandlerResult.None);
        }

        // Act
        var result = await _processor.ProcessAsync<TestMessage>(@event, DelegateHandler, null, deserializationOptions);

        // Assert
        Assert.NotNull(result);
        Assert.True(handlerCalled);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
    }

    #endregion

    #region Constructor and Instance Tests

    [Fact]
    public void TypedInstance_ReturnsSameInstance()
    {
        // Act
        var instance1 = TypedSqsBatchProcessor.TypedInstance;
        var instance2 = TypedSqsBatchProcessor.TypedInstance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Constructor_WithCustomServices_InitializesCorrectly()
    {
        // Arrange
        var mockDeserializationService = Substitute.For<IDeserializationService>();
        var mockRecordDataExtractor = Substitute.For<IRecordDataExtractor<SQSEvent.SQSMessage>>();

        // Act
        var processor = new TypedSqsBatchProcessor(
            _mockConfigurations,
            mockDeserializationService,
            mockRecordDataExtractor);

        // Assert
        Assert.NotNull(processor);
    }

    [Fact]
    public void DefaultConstructor_InitializesCorrectly()
    {
        // Act & Assert - Should not throw
        var processor = new TestableTypedSqsBatchProcessor();
        Assert.NotNull(processor);
    }

    // Helper class to test protected constructor
    private class TestableTypedSqsBatchProcessor : TypedSqsBatchProcessor
    {
        public TestableTypedSqsBatchProcessor() : base()
        {
        }
    }

    #endregion

    #region Wrapper Class Coverage Tests

    [Fact]
    public async Task ProcessAsync_WithIgnoreErrorPolicy_SkipsInvalidRecords()
    {
        // Arrange
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = "invalid-json"
                },
                new()
                {
                    MessageId = "msg-2",
                    Body = JsonSerializer.Serialize(new TestMessage { Id = 1, Name = "Valid" })
                }
            }
        };

        var deserializationOptions = new DeserializationOptions
        {
            ErrorPolicy = DeserializationErrorPolicy.IgnoreRecord
        };

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.NotNull(result);
        // Should only process the valid record
        await handler.Received(1).HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithContextHandlerAndIgnoreErrorPolicy_SkipsInvalidRecords()
    {
        // Arrange
        var @event = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = "invalid-json"
                }
            }
        };

        var context = Substitute.For<ILambdaContext>();
        var deserializationOptions = new DeserializationOptions
        {
            ErrorPolicy = DeserializationErrorPolicy.IgnoreRecord
        };

        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestMessage>>();

        // Act
        var result = await _processor.ProcessAsync(@event, handler, context, deserializationOptions);

        // Assert
        Assert.NotNull(result);
        // Should not call handler for invalid record
        await handler.DidNotReceive().HandleAsync(Arg.Any<TestMessage>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>());
    }

    #endregion
}

// JsonSerializerContext needs to be outside the test class and partial for source generation
[JsonSerializable(typeof(TypedSqsBatchProcessorTests.TestMessage))]
public partial class TypedSqsTestJsonSerializerContext : JsonSerializerContext
{
}