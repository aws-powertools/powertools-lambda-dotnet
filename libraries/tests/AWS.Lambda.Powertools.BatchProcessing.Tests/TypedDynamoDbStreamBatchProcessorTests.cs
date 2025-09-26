

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

[Collection("Sequential")]
public class TypedDynamoDbStreamBatchProcessorTests
{
    private readonly TypedDynamoDbStreamBatchProcessor _processor;

    public TypedDynamoDbStreamBatchProcessorTests()
    {
        Substitute.For<IPowertoolsConfigurations>();
        _processor = new TypedDynamoDbStreamBatchProcessor();
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandler_DeserializesAndProcessesSuccessfully()
    {
        // Arrange
        var @event = CreateDynamoDbEvent("INSERT", "seq-1", "1", "Test Record");

        TestDynamoDbRecord capturedRecord = null;
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRecord = callInfo.Arg<TestDynamoDbRecord>();
                return RecordHandlerResult.None;
            });

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
        Assert.NotNull(capturedRecord);
        Assert.Equal("INSERT", capturedRecord.EventName);
        Assert.Equal("seq-1", capturedRecord.SequenceNumber);

        await handler.Received(1).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContext_PassesContextCorrectly()
    {
        // Arrange
        var context = Substitute.For<ILambdaContext>();
        context.AwsRequestId.Returns("test-request-id");
        
        var @event = CreateDynamoDbEvent("MODIFY", "seq-2", "2", "Context Test");

        TestDynamoDbRecord capturedRecord = null;
        ILambdaContext capturedContext = null;
        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRecord = callInfo.Arg<TestDynamoDbRecord>();
                capturedContext = callInfo.Arg<ILambdaContext>();
                return RecordHandlerResult.None;
            });

        // Act
        var result = await _processor.ProcessAsync(@event, handler, context);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.NotNull(capturedRecord);
        Assert.Equal("MODIFY", capturedRecord.EventName);
        Assert.Equal("seq-2", capturedRecord.SequenceNumber);
        Assert.NotNull(capturedContext);
        Assert.Equal("test-request-id", capturedContext.AwsRequestId);

        await handler.Received(1).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithDeserializationOptions_UsesCustomOptions()
    {
        // Arrange
        var @event = CreateDynamoDbEvent("INSERT", "seq-3", "3", "Custom Options Test");

        var deserializationOptions = new DeserializationOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        };

        TestDynamoDbRecord capturedRecord = null;
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRecord = callInfo.Arg<TestDynamoDbRecord>();
                return RecordHandlerResult.None;
            });

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.NotNull(capturedRecord);
        Assert.Equal("INSERT", capturedRecord.EventName);
        Assert.Equal("seq-3", capturedRecord.SequenceNumber);
        Assert.NotNull(capturedRecord.Keys);
        Assert.NotNull(capturedRecord.NewImage);

        await handler.Received(1).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithJsonSerializerContext_UsesAOTCompatibleDeserialization()
    {
        // Arrange
        var @event = CreateDynamoDbEvent("INSERT", "seq-4", "4", "AOT Test");

        var deserializationOptions = new DeserializationOptions(TypedDynamoDbTestJsonSerializerContext.Default);

        TestDynamoDbRecord capturedRecord = null;
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRecord = callInfo.Arg<TestDynamoDbRecord>();
                return RecordHandlerResult.None;
            });

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.NotNull(capturedRecord);
        Assert.Equal("INSERT", capturedRecord.EventName);
        Assert.Equal("seq-4", capturedRecord.SequenceNumber);

        await handler.Received(1).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJson_FailsRecordByDefault()
    {
        // Arrange - Create a record that will produce invalid JSON for our TestDynamoDbRecord type
        // We'll use a custom deserializer that always throws to simulate deserialization failure
        var @event = CreateDynamoDbEvent("INSERT", "seq-5", "5", "Invalid Test");

        var mockDeserializationService = Substitute.For<IDeserializationService>();
        mockDeserializationService.Deserialize<TestDynamoDbRecord>(Arg.Any<string>(), Arg.Any<DeserializationOptions>())
            .Returns(callInfo => throw new DeserializationException("Test deserialization failure", typeof(TestDynamoDbRecord), "seq-5", new JsonException("Invalid JSON")));

        var processor = new TypedDynamoDbStreamBatchProcessor(mockDeserializationService);
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => processor.ProcessAsync(@event, handler));
        
        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: 'seq-5'", exception.Message);
        Assert.Single(exception.InnerExceptions);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJsonAndIgnorePolicy_IgnoresRecord()
    {
        // Arrange - Create a record that will produce invalid JSON for our TestDynamoDbRecord type
        // We'll use a custom deserializer that always throws to simulate deserialization failure
        var @event = CreateDynamoDbEvent("INSERT", "seq-6", "6", "Invalid Test");

        var mockDeserializationService = Substitute.For<IDeserializationService>();
        mockDeserializationService.Deserialize<TestDynamoDbRecord>(Arg.Any<string>(), Arg.Any<DeserializationOptions>())
            .Returns(callInfo => throw new DeserializationException("Test deserialization failure", typeof(TestDynamoDbRecord), "seq-6", new JsonException("Invalid JSON")));

        var processor = new TypedDynamoDbStreamBatchProcessor(mockDeserializationService);

        var deserializationOptions = new DeserializationOptions
        {
            ErrorPolicy = DeserializationErrorPolicy.IgnoreRecord
        };

        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();

        // Act
        var result = await processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleRecords_ProcessesAllSuccessfully()
    {
        // Arrange
        var @event = new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord>
            {
                CreateDynamoDbRecord("INSERT", "seq-7", "7", "Record 1"),
                CreateDynamoDbRecord("MODIFY", "seq-8", "8", "Record 2")
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Equal(2, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.Received(2).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithHandlerException_FailsRecord()
    {
        // Arrange
        var @event = CreateDynamoDbEvent("INSERT", "seq-9", "9", "Exception Test");

        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        handler.WhenForAnyArgs(x => x.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => throw new InvalidOperationException("Handler failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));
        
        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: 'seq-9'", exception.Message);
        Assert.Single(exception.InnerExceptions);
    }

    [Fact]
    public async Task ProcessAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var @event = CreateDynamoDbEvent("INSERT", "seq-10", "10", "Cancellation Test");

        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => 
            _processor.ProcessAsync(@event, handler, cancellationTokenSource.Token));
        
        // Verify the cancellation was the root cause
        Assert.Contains("Failed processing record: 'seq-10'", exception.Message);
        Assert.Single(exception.InnerExceptions);
        Assert.IsType<RecordProcessingException>(exception.InnerExceptions.First());
        Assert.IsType<OperationCanceledException>(exception.InnerExceptions.First().InnerException);
    }



    [Fact]
    public async Task ProcessAsync_WithNullContext_HandlesGracefully()
    {
        // Arrange
        var @event = CreateDynamoDbEvent("INSERT", "seq-11", "11", "Null Context Test");

        TestDynamoDbRecord capturedRecord = null;
        ILambdaContext capturedContext = null;
        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRecord = callInfo.Arg<TestDynamoDbRecord>();
                capturedContext = callInfo.Arg<ILambdaContext>();
                return RecordHandlerResult.None;
            });

        // Act
        var result = await _processor.ProcessAsync(@event, handler, context: null);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.NotNull(capturedRecord);
        Assert.Equal("INSERT", capturedRecord.EventName);
        Assert.Equal("seq-11", capturedRecord.SequenceNumber);
        Assert.Null(capturedContext);

        await handler.Received(1).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithRemoveEvent_ProcessesOldImageCorrectly()
    {
        // Arrange
        var @event = CreateDynamoDbEvent("REMOVE", "seq-12", "12", "Remove Test");

        TestDynamoDbRecord capturedRecord = null;
        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRecord = callInfo.Arg<TestDynamoDbRecord>();
                return RecordHandlerResult.None;
            });

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.NotNull(capturedRecord);
        Assert.Equal("REMOVE", capturedRecord.EventName);
        Assert.Equal("seq-12", capturedRecord.SequenceNumber);

        await handler.Received(1).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMixedEventTypes_ProcessesAllCorrectly()
    {
        // Arrange
        var @event = new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord>
            {
                CreateDynamoDbRecord("INSERT", "seq-13", "13", "Insert Record"),
                CreateDynamoDbRecord("MODIFY", "seq-14", "14", "Modify Record"),
                CreateDynamoDbRecord("REMOVE", "seq-15", "15", "Remove Record")
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestDynamoDbRecord>>();
        handler.HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Equal(3, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.Received(3).HandleAsync(Arg.Any<TestDynamoDbRecord>(), Arg.Any<CancellationToken>());
    }

    // Helper methods
    private DynamoDBEvent CreateDynamoDbEvent(string eventName, string sequenceNumber, string id, string name)
    {
        return new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord>
            {
                CreateDynamoDbRecord(eventName, sequenceNumber, id, name)
            }
        };
    }

    private DynamoDBEvent.DynamodbStreamRecord CreateDynamoDbRecord(string eventName, string sequenceNumber, string id, string name)
    {
        var record = new DynamoDBEvent.DynamodbStreamRecord
        {
            EventName = eventName,
            Dynamodb = new DynamoDBEvent.StreamRecord
            {
                SequenceNumber = sequenceNumber,
                Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["Id"] = new DynamoDBEvent.AttributeValue { N = id }
                },
                StreamViewType = "NEW_AND_OLD_IMAGES"
            }
        };

        // For INSERT and MODIFY events, include NewImage
        if (eventName == "INSERT" || eventName == "MODIFY")
        {
            record.Dynamodb.NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
            {
                ["Id"] = new DynamoDBEvent.AttributeValue { N = id },
                ["Name"] = new DynamoDBEvent.AttributeValue { S = name }
            };
        }

        // For REMOVE and MODIFY events, include OldImage
        if (eventName == "REMOVE" || eventName == "MODIFY")
        {
            record.Dynamodb.OldImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
            {
                ["Id"] = new DynamoDBEvent.AttributeValue { N = id },
                ["Name"] = new DynamoDBEvent.AttributeValue { S = name }
            };
        }

        return record;
    }



    // Test data classes that match the DynamoDbRecordDataExtractor output structure
    public class TestDynamoDbRecord
    {
        public string EventName { get; set; }
        public Dictionary<string, DynamoDBEvent.AttributeValue> Keys { get; set; }
        public Dictionary<string, DynamoDBEvent.AttributeValue> NewImage { get; set; }
        public Dictionary<string, DynamoDBEvent.AttributeValue> OldImage { get; set; }
        public string SequenceNumber { get; set; }
        public long SizeBytes { get; set; }
        public string StreamViewType { get; set; }
    }
}

// JsonSerializerContext needs to be outside the test class and partial for source generation
[JsonSerializable(typeof(TypedDynamoDbStreamBatchProcessorTests.TestDynamoDbRecord))]
public partial class TypedDynamoDbTestJsonSerializerContext : JsonSerializerContext
{
}