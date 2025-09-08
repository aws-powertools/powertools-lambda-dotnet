

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for TypedDynamoDbStreamBatchProcessorBuilder fluent API and DynamoDB stream-specific functionality.
/// </summary>
public class TypedDynamoDbStreamBatchProcessorBuilderTests
{
    /// <summary>
    /// Test data class for testing DynamoDB stream typed record handlers.
    /// </summary>
    public class TestDynamoDbData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public DateTime LastModified { get; set; }
        public Dictionary<string, object> Attributes { get; set; }
    }

    private readonly ITypedBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord> _mockBatchProcessor;
    private readonly DynamoDBEvent _testDynamoDbEvent;
    private readonly ILambdaContext _mockContext;

    public TypedDynamoDbStreamBatchProcessorBuilderTests()
    {
        _mockBatchProcessor = Substitute.For<ITypedBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>>();
        _mockContext = Substitute.For<ILambdaContext>();
        
        // Setup a realistic DynamoDB stream event for testing
        _testDynamoDbEvent = new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord>
            {
                new DynamoDBEvent.DynamodbStreamRecord
                {
                    EventID = "dynamodb-event-1",
                    EventName = "INSERT",
                    EventSource = "aws:dynamodb",
                    Dynamodb = new DynamoDBEvent.StreamRecord
                    {
                        SequenceNumber = "ddb-seq-001",
                        StreamViewType = "NEW_AND_OLD_IMAGES",
                        NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                        {
                            ["Id"] = new DynamoDBEvent.AttributeValue { S = "item-001" },
                            ["Name"] = new DynamoDBEvent.AttributeValue { S = "Test Item" },
                            ["Status"] = new DynamoDBEvent.AttributeValue { S = "Active" },
                            ["LastModified"] = new DynamoDBEvent.AttributeValue { S = "2023-01-01T00:00:00Z" }
                        }
                    }
                },
                new DynamoDBEvent.DynamodbStreamRecord
                {
                    EventID = "dynamodb-event-2",
                    EventName = "MODIFY",
                    EventSource = "aws:dynamodb",
                    Dynamodb = new DynamoDBEvent.StreamRecord
                    {
                        SequenceNumber = "ddb-seq-002",
                        StreamViewType = "NEW_AND_OLD_IMAGES",
                        NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                        {
                            ["Id"] = new DynamoDBEvent.AttributeValue { S = "item-002" },
                            ["Name"] = new DynamoDBEvent.AttributeValue { S = "Updated Item" },
                            ["Status"] = new DynamoDBEvent.AttributeValue { S = "Modified" },
                            ["LastModified"] = new DynamoDBEvent.AttributeValue { S = "2023-01-02T00:00:00Z" }
                        },
                        OldImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                        {
                            ["Id"] = new DynamoDBEvent.AttributeValue { S = "item-002" },
                            ["Name"] = new DynamoDBEvent.AttributeValue { S = "Original Item" },
                            ["Status"] = new DynamoDBEvent.AttributeValue { S = "Active" },
                            ["LastModified"] = new DynamoDBEvent.AttributeValue { S = "2023-01-01T12:00:00Z" }
                        }
                    }
                }
            }
        };

        // Setup mock to return a basic processing result
        var processingResult = new ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>();
        _mockBatchProcessor.ProcessingResult.Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<DynamoDBEvent>(),
                Arg.Any<ITypedRecordHandler<TestDynamoDbData>>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<DynamoDBEvent>(),
                Arg.Any<ITypedRecordHandlerWithContext<TestDynamoDbData>>(),
                Arg.Any<ILambdaContext>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);
    }

    [Fact]
    public void Constructor_WithValidBatchProcessor_CreatesInstance()
    {
        // Act
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Constructor_WithNullBatchProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TypedDynamoDbStreamBatchProcessorBuilder(null));
    }

    [Fact]
    public void Create_WithDefaultInstance_ReturnsBuilder()
    {
        // Act
        var builder = TypedDynamoDbStreamBatchProcessorBuilder.Create();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<TypedDynamoDbStreamBatchProcessorBuilder>(builder);
    }

    [Fact]
    public void Create_WithCustomProcessor_ReturnsBuilder()
    {
        // Arrange
        var customProcessor = new TypedDynamoDbStreamBatchProcessor(Substitute.For<AWS.Lambda.Powertools.Common.IPowertoolsConfigurations>());

        // Act
        var builder = TypedDynamoDbStreamBatchProcessorBuilder.Create(customProcessor);

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<TypedDynamoDbStreamBatchProcessorBuilder>(builder);
    }

    [Fact]
    public void Create_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TypedDynamoDbStreamBatchProcessorBuilder.Create(null));
    }

    [Fact]
    public void FluentConfiguration_WithDynamoDbSpecificOptions_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedDynamoDbStreamBatchProcessorBuilderTestJsonSerializerContext.Default;
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
    public void Handler_WithDynamoDbSpecificTypedHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandler<TestDynamoDbData> handler = (data, cancellationToken) => 
        {
            // Simulate DynamoDB stream-specific processing
            Assert.NotNull(data.Id);
            Assert.NotNull(data.Name);
            Assert.NotNull(data.Status);
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithDynamoDbSpecificContextHandler_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandlerWithContext<TestDynamoDbData> handler = (data, context, cancellationToken) => 
        {
            // Simulate DynamoDB stream-specific processing with context
            Assert.NotNull(data.Id);
            Assert.NotNull(context);
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task ProcessAsync_WithDynamoDbEvent_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbData>>();

        // Act
        await builder.ProcessAsync(_testDynamoDbEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testDynamoDbEvent,
            handler,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithDynamoDbEventAndContext_CallsUnderlyingProcessor()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestDynamoDbData>>();

        // Act
        await builder.ProcessAsync(_testDynamoDbEvent, handler, _mockContext);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testDynamoDbEvent,
            handler,
            _mockContext,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public async Task ProcessAsync_WithDynamoDbSpecificConfiguration_PassesCorrectOptions()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        var context = TypedDynamoDbStreamBatchProcessorBuilderTestJsonSerializerContext.Default;
        var processingOptions = new ProcessingOptions 
        { 
            BatchParallelProcessingEnabled = true,
            CancellationToken = CancellationToken.None
        };
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbData>>();

        builder
            .WithJsonSerializerContext(context)
            .WithDeserializationErrorPolicy(DeserializationErrorPolicy.FailRecord)
            .WithProcessingOptions(processingOptions);

        // Act
        await builder.ProcessAsync(_testDynamoDbEvent, handler);

        // Assert
        await _mockBatchProcessor.Received(1).ProcessAsync(
            _testDynamoDbEvent,
            handler,
            Arg.Is<DeserializationOptions>(opts => 
                opts.JsonSerializerContext == context && 
                opts.ErrorPolicy == DeserializationErrorPolicy.FailRecord),
            Arg.Is<ProcessingOptions>(opts => 
                opts.BatchParallelProcessingEnabled == true));
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleDynamoDbRecords_HandlesCorrectly()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbData>>();
        
        // Setup processing result with multiple records
        var processingResult = new ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>();
        foreach (var record in _testDynamoDbEvent.Records)
        {
            processingResult.SuccessRecords.Add(new RecordSuccess<DynamoDBEvent.DynamodbStreamRecord>
            {
                Record = record,
                RecordId = record.Dynamodb.SequenceNumber,
                HandlerResult = RecordHandlerResult.None
            });
        }
        _mockBatchProcessor.ProcessAsync(
                Arg.Any<DynamoDBEvent>(),
                Arg.Any<ITypedRecordHandler<TestDynamoDbData>>(),
                Arg.Any<DeserializationOptions>(),
                Arg.Any<ProcessingOptions>())
            .Returns(processingResult);

        // Act
        var result = await builder.ProcessAsync(_testDynamoDbEvent, handler);

        // Assert
        Assert.Equal(2, result.SuccessRecords.Count);
        await _mockBatchProcessor.Received(1).ProcessAsync(
            Arg.Is<DynamoDBEvent>(e => e.Records.Count == 2),
            handler,
            Arg.Any<DeserializationOptions>(),
            Arg.Any<ProcessingOptions>());
    }

    [Fact]
    public void InheritsFromBatchProcessorBuilder_VerifyInheritance()
    {
        // Arrange & Act
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);

        // Assert
        Assert.IsAssignableFrom<BatchProcessorBuilder<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>>(builder);
    }

    [Fact]
    public async Task ProcessAsync_WithNullDynamoDbEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbData>>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(null, handler));
    }

    [Fact]
    public async Task ProcessAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => builder.ProcessAsync(_testDynamoDbEvent, (ITypedRecordHandler<TestDynamoDbData>)null));
    }

    [Fact]
    public void Handler_WithChangeDataCaptureScenario_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandler<TestDynamoDbData> handler = (data, cancellationToken) => 
        {
            // Simulate change data capture scenario
            if (data.Status == "Modified")
            {
                // Process modified records differently
                Assert.True(data.LastModified != default(DateTime));
                Assert.NotNull(data.Attributes);
            }
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Handler_WithEventTypeBasedProcessing_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new TypedDynamoDbStreamBatchProcessorBuilder(_mockBatchProcessor);
        TypedRecordHandlerWithContext<TestDynamoDbData> handler = (data, context, cancellationToken) => 
        {
            // Simulate event type-based processing (INSERT, MODIFY, REMOVE)
            Assert.NotNull(data.Id);
            
            // In a real scenario, you might check the event name from the DynamoDB record
            // and process accordingly (INSERT vs MODIFY vs REMOVE)
            if (!string.IsNullOrEmpty(data.Name))
            {
                // Process records with names
                Assert.True(data.Name.Length > 0);
            }
            
            return Task.FromResult(RecordHandlerResult.None);
        };

        // Act
        var result = builder.Handler(handler);

        // Assert
        Assert.Same(builder, result);
    }
}

// JsonSerializerContext for DynamoDB stream-specific test data
[System.Text.Json.Serialization.JsonSerializable(typeof(TypedDynamoDbStreamBatchProcessorBuilderTests.TestDynamoDbData))]
public partial class TypedDynamoDbStreamBatchProcessorBuilderTestJsonSerializerContext : System.Text.Json.Serialization.JsonSerializerContext
{
}