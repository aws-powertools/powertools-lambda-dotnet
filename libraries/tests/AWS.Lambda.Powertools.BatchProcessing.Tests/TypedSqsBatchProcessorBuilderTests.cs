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
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for TypedSqsBatchProcessorBuilder fluent API and SQS-specific functionality.
/// </summary>
public class TypedSqsBatchProcessorBuilderTests
{
    /// <summary>
    /// Test data class for testing typed record handlers.
    /// </summary>
    public class TestSqsData
    {
        public string OrderId { get; set; }
        public string ProductName { get; set; }
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
    }

    private readonly ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage> _mockBatchProcessor;
    private readonly SQSEvent _testSqsEvent;
    private readonly ILambdaContext _mockContext;

    public TypedSqsBatchProcessorBuilderTests()
    {
        _mockBatchProcessor = Substitute.For<ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage>>();
        _mockContext = Substitute.For<ILambdaContext>();
        
        // Setup a realistic SQS event for testing
        _testSqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "sqs-message-1",
                    Body = "{\"OrderId\":\"ORD-001\",\"ProductName\":\"Test Product\",\"Amount\":99.99,\"OrderDate\":\"2023-01-01T00:00:00Z\"}",
                    ReceiptHandle = "receipt-handle-1",
                    Attributes = new Dictionary<string, string>
                    {
                        ["SentTimestamp"] = "1672531200000"
                    }
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "sqs-message-2",
                    Body = "{\"OrderId\":\"ORD-002\",\"ProductName\":\"Another Product\",\"Amount\":149.99,\"OrderDate\":\"2023-01-02T00:00:00Z\"}",
                    ReceiptHandle = "receipt-handle-2",
                    Attributes = new Dictionary<string, string>
                    {
                        ["SentTimestamp"] = "1672617600000"
                    }
                }
            }
        };

        // Setup mock to return a basic processing result
        var processingResult = new ProcessingResult<SQSEvent.SQSMessage>();
        _mockBatchProcessor.ProcessingResult.Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<SQSEvent>(),
                Arg.Any<ITypedRecordHandler<TestSqsData>>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<SQSEvent>(),
                Arg.Any<ITypedRecordHandlerWithContext<TestSqsData>>(),
                Arg.Any<ILambdaContext>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
    }

    [Fact]
    public void Constructor_WithValidBatchProcessor_CreatesInstance()
    {
        // Act
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Constructor_WithNullBatchProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TypedSqsBatchProcessorBuilder(null));
    }

    [Fact]
    public void Create_WithDefaultInstance_ReturnsBuilder()
    {
        // Act
        var builder = TypedSqsBatchProcessorBuilder.Create();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<TypedSqsBatchProcessorBuilder>(builder);
    }

    [Fact]
    public void Create_WithCustomProcessor_ReturnsBuilder()
    {
        // Arrange
        var customProcessor = new TypedSqsBatchProcessor(Substitute.For<AWS.Lambda.Powertools.Common.IPowertoolsConfigurations>());

        // Act
        var builder = TypedSqsBatchProcessorBuilder.Create(customProcessor);

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<TypedSqsBatchProcessorBuilder>(builder);
    }

    [Fact]
    public void Create_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TypedSqsBatchProcessorBuilder.Create(null));
    }

    [Fact]
    public void FluentConfiguration_WithSqsSpecificOptions_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedSqsBatchProcessorBuilderTestJsonSerializerContext.Default;
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
    public void Handler_WithSqsSpecificTypedHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandler<TestSqsData> handler = (data, cancellationToken) => 
        {
            // Simulate SQS-specific processing
            Assert.NotNull(data.OrderId);
            Assert.NotNull(data.ProductName);
            Assert.True(data.Amount > 0);
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithSqsSpecificContextHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandlerWithContext<TestSqsData> handler = (data, context, cancellationToken) => 
        {
            // Simulate SQS-specific processing with context
            Assert.NotNull(data.OrderId);
            Assert.NotNull(context);
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task ProcessAsync_WithSqsEvent_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestSqsData>>();

        // Act
        await builder.ProcessAsync(_testSqsEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testSqsEvent,
            handler,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithSqsEventAndContext_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestSqsData>>();

        // Act
        await builder.ProcessAsync(_testSqsEvent, handler, _mockContext);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testSqsEvent,
            handler,
            _mockContext,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithSqsSpecificConfiguration_PassesCorrectOptions()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedSqsBatchProcessorBuilderTestJsonSerializerContext.Default;
        var processingOptions = new ProcessingOptions 
        { 
            BatchParallelProcessingEnabled = true,
            CancellationToken = CancellationToken.None
        };
        var handler = Substitute.For<ITypedRecordHandler<TestSqsData>>();

        builder
            .WithJsonSerializerContext(context)
            .WithDeserializationErrorPolicy(DeserializationErrorPolicy.IgnoreRecord)
            .WithProcessingOptions(processingOptions);

        // Act
        await builder.ProcessAsync(_testSqsEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testSqsEvent,
            handler,
            Arg.Is<DeserializationOptions>(opts => 
                opts.JsonSerializerContext == context && 
                opts.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord),
            Arg.Is<ProcessingOptions>(opts => 
                opts.BatchParallelProcessingEnabled == true));
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleSqsMessages_HandlesCorrectly()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestSqsData>>();
        
        // Setup processing result with multiple records
        var processingResult = new ProcessingResult<SQSEvent.SQSMessage>();
        foreach (var record in _testSqsEvent.Records)
        {
            processingResult.SuccessRecords.Add(new RecordSuccess<SQSEvent.SQSMessage>
            {
                Record = record,
                RecordId = record.MessageId,
                HandlerResult = RecordHandlerResult.None
            });
        }
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<SQSEvent>(),
                Arg.Any<ITypedRecordHandler<TestSqsData>>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);

        // Act
        var result = await builder.ProcessAsync(_testSqsEvent, handler);

        // Assert
        Assert.Equal(2, result.SuccessRecords.Count);
        await _mockBatchProcessor.Received(1).ProcessAsync(
            Arg.Is<SQSEvent>(e => e.Records.Count == 2),
            handler,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public void InheritsFromBatchProcessorBuilder_VerifyInheritance()
    {
        // Arrange & Act
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);

        // Assert
        Assert.IsAssignableFrom<BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>>(builder);
    }

    [Fact]
    public async Task ProcessAsync_WithNullSqsEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestSqsData>>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(null, handler));
    }

    [Fact]
    public async Task ProcessAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(_testSqsEvent, (ITypedRecordHandler<TestSqsData>)null));
    }

    public class UnregisteredBuilderData
    {
        public string Value { get; set; }
    }

    #region AOT Compatibility Tests

    [Fact]
    public async Task ProcessAsync_WithUnregisteredTypeInContext_ThrowsAotTypeValidationException()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedSqsBatchProcessorBuilderTestJsonSerializerContext.Default;
        
        builder.WithJsonSerializerContext(context);

        var handler = Substitute.For<ITypedRecordHandler<UnregisteredBuilderData>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AotTypeValidationException>(() =>
            builder.ProcessAsync(_testSqsEvent, handler));

        Assert.Equal(typeof(UnregisteredBuilderData), exception.TargetType);
        Assert.Contains("UnregisteredBuilderData", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_WithContextHandler_ValidatesAotCompatibility()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedSqsBatchProcessorBuilderTestJsonSerializerContext.Default;
        var lambdaContext = Substitute.For<ILambdaContext>();
        
        builder.WithJsonSerializerContext(context);

        var handler = Substitute.For<ITypedRecordHandlerWithContext<UnregisteredBuilderData>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AotTypeValidationException>(() =>
            builder.ProcessAsync(_testSqsEvent, handler, lambdaContext));

        Assert.Equal(typeof(UnregisteredBuilderData), exception.TargetType);
    }

    [Fact]
    public async Task ProcessAsync_WithValidAotContext_CallsProcessor()
    {
        // Arrange
        var builder = new TypedSqsBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedSqsBatchProcessorBuilderTestJsonSerializerContext.Default;
        
        builder.WithJsonSerializerContext(context);

        var handler = Substitute.For<ITypedRecordHandler<TestSqsData>>();
        var expectedResult = new ProcessingResult<SQSEvent.SQSMessage>();

        _mockBatchProcessor.ProcessAsync(
            Arg.Any<SQSEvent>(),
            Arg.Any<ITypedRecordHandler<TestSqsData>>(),
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>())
            .Returns(expectedResult);

        // Act
        var result = await builder.ProcessAsync(_testSqsEvent, handler);

        // Assert
        Assert.Equal(expectedResult, result);
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testSqsEvent,
            handler,
            Arg.Is<DeserializationOptions>(opts => opts.JsonSerializerContext == context),
            Arg.Any<ProcessingOptions>());
    }

    #endregion
}

// JsonSerializerContext for SQS-specific test data
[System.Text.Json.Serialization.JsonSerializable(typeof(TypedSqsBatchProcessorBuilderTests.TestSqsData))]
public partial class TypedSqsBatchProcessorBuilderTestJsonSerializerContext : System.Text.Json.Serialization.JsonSerializerContext
{
}