

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for BatchProcessorBuilder fluent API and method chaining.
/// </summary>
public class BatchProcessorBuilderTests
{
    /// <summary>
    /// Test data class for testing typed record handlers.
    /// </summary>
    public class TestData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }

    private readonly ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage> _mockBatchProcessor;
    private readonly SQSEvent _testEvent;
    private readonly ILambdaContext _mockContext;

    public BatchProcessorBuilderTests()
    {
        _mockBatchProcessor = Substitute.For<ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage>>();
        _mockContext = Substitute.For<ILambdaContext>();
        
        // Setup a basic SQS event for testing
        _testEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "test-message-1",
                    Body = "{\"Id\":\"1\",\"Name\":\"Test\",\"Value\":42}"
                }
            }
        };

        // Setup mock to return a basic processing result
        var processingResult = new ProcessingResult<SQSEvent.SQSMessage>();
        _mockBatchProcessor.ProcessingResult.Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<SQSEvent>(),
                Arg.Any<ITypedRecordHandler<TestData>>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<SQSEvent>(),
                Arg.Any<ITypedRecordHandlerWithContext<TestData>>(),
                Arg.Any<ILambdaContext>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
    }

    [Fact]
    public void Constructor_WithNullBatchProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(null));
    }

    [Fact]
    public void Constructor_WithValidBatchProcessor_CreatesInstance()
    {
        // Act
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithJsonSerializerContext_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var context = BatchProcessorBuilderTestJsonSerializerContext.Default;

        // Act
        var result = builder.WithJsonSerializerContext(context);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithJsonSerializerOptions_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var result = builder.WithJsonSerializerOptions(options);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithDeserializationErrorPolicy_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);

        // Act
        var result = builder.WithDeserializationErrorPolicy(DeserializationErrorPolicy.IgnoreRecord);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithProcessingOptions_WithValidOptions_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var processingOptions = new ProcessingOptions { BatchParallelProcessingEnabled = true };

        // Act
        var result = builder.WithProcessingOptions(processingOptions);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithProcessingOptions_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithProcessingOptions(null));
    }

    [Fact]
    public void Handler_WithTypedRecordHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        TypedRecordHandler<TestData> handler = (data, cancellationToken) => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithTypedRecordHandlerWithContext_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        TypedRecordHandlerWithContext<TestData> handler = (data, context, cancellationToken) => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithSimpleTypedRecordHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        SimpleTypedRecordHandler<TestData> handler = data => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithSimpleTypedRecordHandlerWithContext_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        SimpleTypedRecordHandlerWithContext<TestData> handler = (data, context) => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithFuncNoContext_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        Func<TestData, Task<RecordHandlerResult>> handler = data => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithFuncWithCancellationToken_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        Func<TestData, CancellationToken, Task<RecordHandlerResult>> handler = (data, cancellationToken) => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithFuncWithContext_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        Func<TestData, ILambdaContext, Task<RecordHandlerResult>> handler = (data, context) => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithFuncWithContextAndCancellationToken_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        Func<TestData, ILambdaContext, CancellationToken, Task<RecordHandlerResult>> handler = (data, context, cancellationToken) => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((TypedRecordHandler<TestData>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((TypedRecordHandlerWithContext<TestData>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((SimpleTypedRecordHandler<TestData>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((SimpleTypedRecordHandlerWithContext<TestData>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((Func<TestData, Task<RecordHandlerResult>>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((Func<TestData, CancellationToken, Task<RecordHandlerResult>>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((Func<TestData, ILambdaContext, Task<RecordHandlerResult>>)null));
        Assert.Throws<ArgumentNullException>(() => builder.Handler<TestData>((Func<TestData, ILambdaContext, CancellationToken, Task<RecordHandlerResult>>)null));
    }

    [Fact]
    public void FluentChaining_WithMultipleConfigurations_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var context = BatchProcessorBuilderTestJsonSerializerContext.Default;
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var processingOptions = new ProcessingOptions { BatchParallelProcessingEnabled = true };
        TypedRecordHandler<TestData> handler = (data, cancellationToken) => Task.FromResult(RecordHandlerResult.None);

        // Act
        var result = builder
            .WithJsonSerializerContext(context)
            .WithJsonSerializerOptions(options)
            .WithDeserializationErrorPolicy(DeserializationErrorPolicy.IgnoreRecord)
            .WithProcessingOptions(processingOptions)
            .Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task ProcessAsync_WithITypedRecordHandler_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestData>>();

        // Act
        await builder.ProcessAsync(_testEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testEvent,
            handler,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithITypedRecordHandlerAndContext_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestData>>();

        // Act
        await builder.ProcessAsync(_testEvent, handler, _mockContext);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testEvent,
            Arg.Any<ITypedRecordHandlerWithContext<TestData>>(),
            _mockContext,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithITypedRecordHandlerWithContext_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestData>>();

        // Act
        await builder.ProcessAsync(_testEvent, handler, _mockContext);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testEvent,
            handler,
            _mockContext,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestData>>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(null, handler));
    }

    [Fact]
    public async Task ProcessAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(_testEvent, (ITypedRecordHandler<TestData>)null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(_testEvent, (ITypedRecordHandlerWithContext<TestData>)null, _mockContext));
    }

    [Fact]
    public async Task ProcessAsync_WithNullContextForContextHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestData>>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(_testEvent, handler, null));
    }

    [Fact]
    public async Task ProcessAsync_WithConfiguredOptions_PassesOptionsToProcessor()
    {
        // Arrange
        var builder = new BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>(_mockBatchProcessor);
        var context = BatchProcessorBuilderTestJsonSerializerContext.Default;
        var processingOptions = new ProcessingOptions { BatchParallelProcessingEnabled = true };
        var handler = Substitute.For<ITypedRecordHandler<TestData>>();

        builder
            .WithJsonSerializerContext(context)
            .WithDeserializationErrorPolicy(DeserializationErrorPolicy.IgnoreRecord)
            .WithProcessingOptions(processingOptions);

        // Act
        await builder.ProcessAsync(_testEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testEvent,
            handler,
            Arg.Is<DeserializationOptions>(opts => 
                opts.JsonSerializerContext == context && 
                opts.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord),
            Arg.Is<ProcessingOptions>(opts => 
                opts.BatchParallelProcessingEnabled == true));
    }
}

// JsonSerializerContext needs to be outside the test class and partial for source generation
[System.Text.Json.Serialization.JsonSerializable(typeof(BatchProcessorBuilderTests.TestData))]
public partial class BatchProcessorBuilderTestJsonSerializerContext : System.Text.Json.Serialization.JsonSerializerContext
{
}