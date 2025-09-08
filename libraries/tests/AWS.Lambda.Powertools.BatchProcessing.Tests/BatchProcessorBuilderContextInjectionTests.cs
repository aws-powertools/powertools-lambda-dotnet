

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

[Collection("Sequential")]
public class BatchProcessorBuilderContextInjectionTests
{
    private readonly IPowertoolsConfigurations _mockConfigurations;
    private readonly TypedSqsBatchProcessor _processor;
    private readonly ILambdaContext _mockContext;

    public BatchProcessorBuilderContextInjectionTests()
    {
        _mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        _processor = new TypedSqsBatchProcessor(_mockConfigurations);
        _mockContext = Substitute.For<ILambdaContext>();
        _mockContext.AwsRequestId.Returns("test-request-id");
        _mockContext.RemainingTime.Returns(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Handler_WithInvalidSignature_ThrowsException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_processor);
        Func<string, Task<RecordHandlerResult>> handler = (data) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Handler<TestMessage>(handler));
        Assert.Contains("First parameter of handler method must be of type 'TestMessage'", exception.Message);
    }

    [Fact]
    public void Handler_WithUnsupportedParameterType_ThrowsException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_processor);
        Func<TestMessage, int, Task<RecordHandlerResult>> handler = (data, unsupported) => 
            Task.FromResult(RecordHandlerResult.None);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Handler<TestMessage>(handler));
        Assert.Contains("Unsupported parameter type 'Int32'", exception.Message);
    }

    [Fact]
    public void Handler_WithInvalidReturnType_ThrowsException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_processor);
        Func<TestMessage, string> handler = (data) => "invalid";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Handler<TestMessage>(handler));
        Assert.Contains("Handler method must return", exception.Message);
    }

    [Fact]
    public void Handler_WithNullHandler_ThrowsException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_processor);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestMessage>((TypedRecordHandler<TestMessage>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestMessage>((TypedRecordHandlerWithContext<TestMessage>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestMessage>((SimpleTypedRecordHandler<TestMessage>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestMessage>((SimpleTypedRecordHandlerWithContext<TestMessage>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestMessage>((Func<TestMessage, Task<RecordHandlerResult>>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestMessage>((Delegate)null));
    }

    [Fact]
    public async Task ProcessAsync_WithHandlerRequiringContext_InjectsContextCorrectly()
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

        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var context = callInfo.ArgAt<ILambdaContext>(1);
                contextReceived = context != null;
                requestIdReceived = context?.AwsRequestId;
                return RecordHandlerResult.None;
            });

        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_processor);

        // Act
        var result = await builder.ProcessAsync(@event, handler, _mockContext);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.True(contextReceived);
        Assert.Equal("test-request-id", requestIdReceived);
    }

    [Fact]
    public async Task ProcessAsync_WithHandlerNotRequiringContext_WorksWithoutContext()
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

        var handler = Substitute.For<ITypedRecordHandler<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                handlerCalled = true;
                return RecordHandlerResult.None;
            });

        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_processor);

        // Act
        var result = await builder.ProcessAsync(@event, handler);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task ProcessAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Null Context Test" };
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

        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestMessage>>();
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_processor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(@event, handler, null));
    }

    [Fact]
    public async Task ProcessAsync_WithDelegateAndNullContext_HandlesGracefully()
    {
        // Arrange
        var testData = new TestMessage { Id = 1, Name = "Null Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var contextReceived = false;

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

        // Use delegate handler that expects context - this should handle null gracefully
        TypedRecordHandlerWithContext<TestMessage> handler = (data, context, cancellationToken) =>
        {
            contextReceived = context != null;
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = await _processor.ProcessAsync<TestMessage>(@event, handler, null);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.False(contextReceived);
    }

    public class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}