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
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for TypedKinesisEventBatchProcessorBuilder fluent API and Kinesis-specific functionality.
/// </summary>
public class TypedKinesisEventBatchProcessorBuilderTests
{
    /// <summary>
    /// Test data class for testing Kinesis typed record handlers.
    /// </summary>
    public class TestKinesisData
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public Dictionary<string, object> Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private readonly ITypedBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord> _mockBatchProcessor;
    private readonly KinesisEvent _testKinesisEvent;
    private readonly ILambdaContext _mockContext;

    public TypedKinesisEventBatchProcessorBuilderTests()
    {
        _mockBatchProcessor = Substitute.For<ITypedBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>>();
        _mockContext = Substitute.For<ILambdaContext>();
        
        // Setup a realistic Kinesis event for testing
        _testKinesisEvent = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new KinesisEvent.KinesisEventRecord
                {
                    EventId = "kinesis-event-1",
                    EventName = "aws:kinesis:record",
                    EventSource = "aws:kinesis",
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "1",
                        PartitionKey = "partition-1",
                        Data = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                            "{\"EventId\":\"evt-001\",\"EventType\":\"UserAction\",\"Payload\":{\"userId\":\"123\",\"action\":\"login\"},\"Timestamp\":\"2023-01-01T00:00:00Z\"}")),
                        ApproximateArrivalTimestamp = DateTime.UtcNow
                    }
                },
                new KinesisEvent.KinesisEventRecord
                {
                    EventId = "kinesis-event-2",
                    EventName = "aws:kinesis:record",
                    EventSource = "aws:kinesis",
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "2",
                        PartitionKey = "partition-2",
                        Data = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                            "{\"EventId\":\"evt-002\",\"EventType\":\"UserAction\",\"Payload\":{\"userId\":\"456\",\"action\":\"logout\"},\"Timestamp\":\"2023-01-02T00:00:00Z\"}")),
                        ApproximateArrivalTimestamp = DateTime.UtcNow
                    }
                }
            }
        };

        // Setup mock to return a basic processing result
        var processingResult = new ProcessingResult<KinesisEvent.KinesisEventRecord>();
        _mockBatchProcessor.ProcessingResult.Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<KinesisEvent>(),
                Arg.Any<ITypedRecordHandler<TestKinesisData>>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<KinesisEvent>(),
                Arg.Any<ITypedRecordHandlerWithContext<TestKinesisData>>(),
                Arg.Any<ILambdaContext>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
    }

    [Fact]
    public void Constructor_WithValidBatchProcessor_CreatesInstance()
    {
        // Act
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Constructor_WithNullBatchProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TypedKinesisEventBatchProcessorBuilder(null));
    }

    [Fact]
    public void Create_WithDefaultInstance_ReturnsBuilder()
    {
        // Act
        var builder = TypedKinesisEventBatchProcessorBuilder.Create();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<TypedKinesisEventBatchProcessorBuilder>(builder);
    }

    [Fact]
    public void Create_WithCustomProcessor_ReturnsBuilder()
    {
        // Arrange
        var customProcessor = new TypedKinesisEventBatchProcessor(Substitute.For<AWS.Lambda.Powertools.Common.IPowertoolsConfigurations>());

        // Act
        var builder = TypedKinesisEventBatchProcessorBuilder.Create(customProcessor);

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<TypedKinesisEventBatchProcessorBuilder>(builder);
    }

    [Fact]
    public void Create_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TypedKinesisEventBatchProcessorBuilder.Create(null));
    }

    [Fact]
    public void FluentConfiguration_WithKinesisSpecificOptions_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedKinesisEventBatchProcessorBuilderTestJsonSerializerContext.Default;
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var processingOptions = new ProcessingOptions { BatchParallelProcessingEnabled = true };

        // Act
        var result = builder
            .WithJsonSerializerContext(context)
            .WithJsonSerializerOptions(options)
            .WithDeserializationErrorPolicy(DeserializationErrorPolicy.IgnoreRecord)
            .WithProcessingOptions(processingOptions);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithKinesisSpecificTypedHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandler<TestKinesisData> handler = (data, cancellationToken) => 
        {
            // Simulate Kinesis-specific processing
            Assert.NotNull(data.EventId);
            Assert.NotNull(data.EventType);
            Assert.NotNull(data.Payload);
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithKinesisSpecificContextHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandlerWithContext<TestKinesisData> handler = (data, context, cancellationToken) => 
        {
            // Simulate Kinesis-specific processing with context
            Assert.NotNull(data.EventId);
            Assert.NotNull(context);
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task ProcessAsync_WithKinesisEvent_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestKinesisData>>();

        // Act
        await builder.ProcessAsync(_testKinesisEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testKinesisEvent,
            handler,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithKinesisEventAndContext_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestKinesisData>>();

        // Act
        await builder.ProcessAsync(_testKinesisEvent, handler, _mockContext);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testKinesisEvent,
            handler,
            _mockContext,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithKinesisSpecificConfiguration_PassesCorrectOptions()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedKinesisEventBatchProcessorBuilderTestJsonSerializerContext.Default;
        var processingOptions = new ProcessingOptions 
        { 
            BatchParallelProcessingEnabled = false,
            CancellationToken = CancellationToken.None
        };
        var handler = Substitute.For<ITypedRecordHandler<TestKinesisData>>();

        builder
            .WithJsonSerializerContext(context)
            .WithDeserializationErrorPolicy(DeserializationErrorPolicy.FailRecord)
            .WithProcessingOptions(processingOptions);

        // Act
        await builder.ProcessAsync(_testKinesisEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testKinesisEvent,
            handler,
            Arg.Is<DeserializationOptions>(opts => 
                opts.JsonSerializerContext == context && 
                opts.ErrorPolicy == DeserializationErrorPolicy.FailRecord),
            Arg.Is<ProcessingOptions>(opts => 
                opts.BatchParallelProcessingEnabled == false));
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleKinesisRecords_HandlesCorrectly()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestKinesisData>>();
        
        // Setup processing result with multiple records
        var processingResult = new ProcessingResult<KinesisEvent.KinesisEventRecord>();
        foreach (var record in _testKinesisEvent.Records)
        {
            processingResult.SuccessRecords.Add(new RecordSuccess<KinesisEvent.KinesisEventRecord>
            {
                Record = record,
                RecordId = record.Kinesis.SequenceNumber,
                HandlerResult = RecordHandlerResult.None
            });
        }
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<KinesisEvent>(),
                Arg.Any<ITypedRecordHandler<TestKinesisData>>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);

        // Act
        var result = await builder.ProcessAsync(_testKinesisEvent, handler);

        // Assert
        Assert.Equal(2, result.SuccessRecords.Count);
        await _mockBatchProcessor.Received(1).ProcessAsync(
            Arg.Is<KinesisEvent>(e => e.Records.Count == 2),
            handler,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public void InheritsFromBatchProcessorBuilder_VerifyInheritance()
    {
        // Arrange & Act
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);

        // Assert
        Assert.IsAssignableFrom<BatchProcessorBuilder<KinesisEvent, KinesisEvent.KinesisEventRecord>>(builder);
    }

    [Fact]
    public async Task ProcessAsync_WithNullKinesisEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestKinesisData>>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(null, handler));
    }

    [Fact]
    public async Task ProcessAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(_testKinesisEvent, (ITypedRecordHandler<TestKinesisData>)null));
    }

    [Fact]
    public void Handler_WithStreamProcessingScenario_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedKinesisEventBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandler<TestKinesisData> handler = (data, cancellationToken) => 
        {
            // Simulate stream processing scenario
            if (data.EventType == "UserAction")
            {
                // Process user action events
                Assert.Contains("userId", data.Payload.Keys);
                Assert.Contains("action", data.Payload.Keys);
            }
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }
}

// JsonSerializerContext for Kinesis-specific test data
[System.Text.Json.Serialization.JsonSerializable(typeof(TypedKinesisEventBatchProcessorBuilderTests.TestKinesisData))]
public partial class TypedKinesisEventBatchProcessorBuilderTestJsonSerializerContext : System.Text.Json.Serialization.JsonSerializerContext
{
}