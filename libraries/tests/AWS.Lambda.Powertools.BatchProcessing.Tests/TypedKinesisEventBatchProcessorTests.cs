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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

[Collection("Sequential")]
public class TypedKinesisEventBatchProcessorTests
{
    private readonly IPowertoolsConfigurations _mockConfigurations;
    private readonly TypedKinesisEventBatchProcessor _processor;

    public TypedKinesisEventBatchProcessorTests()
    {
        _mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        _processor = new TypedKinesisEventBatchProcessor(_mockConfigurations);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandler_DeserializesAndProcessesSuccessfully()
    {
        // Arrange
        var testData = new TestKinesisMessage { Id = 1, Name = "Test Kinesis Message" };
        var messageBody = JsonSerializer.Serialize(testData);
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12345",
                        Data = dataStream,
                        PartitionKey = "partition-1"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();
        handler.HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestKinesisMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContext_PassesContextCorrectly()
    {
        // Arrange
        var testData = new TestKinesisMessage { Id = 2, Name = "Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));
        var context = Substitute.For<ILambdaContext>();
        context.AwsRequestId.Returns("test-request-id");

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12346",
                        Data = dataStream,
                        PartitionKey = "partition-2"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestKinesisMessage>>();
        handler.HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, context);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestKinesisMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Is<ILambdaContext>(c => c.AwsRequestId == "test-request-id"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithDeserializationOptions_UsesCustomOptions()
    {
        // Arrange
        var testData = new TestKinesisMessage { Id = 3, Name = "Custom Options Test" };
        var messageBody = JsonSerializer.Serialize(testData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12347",
                        Data = dataStream,
                        PartitionKey = "partition-3"
                    }
                }
            }
        };

        var deserializationOptions = new DeserializationOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();
        handler.HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestKinesisMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithJsonSerializerContext_UsesAOTCompatibleDeserialization()
    {
        // Arrange
        var testData = new TestKinesisMessage { Id = 4, Name = "AOT Test" };
        var messageBody = JsonSerializer.Serialize(testData, TypedKinesisTestJsonSerializerContext.Default.TestKinesisMessage);
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12348",
                        Data = dataStream,
                        PartitionKey = "partition-4"
                    }
                }
            }
        };

        var deserializationOptions = new DeserializationOptions(TypedKinesisTestJsonSerializerContext.Default);

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();
        handler.HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestKinesisMessage>(m => m.Id == testData.Id && m.Name == testData.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJson_FailsRecordByDefault()
    {
        // Arrange
        var invalidJson = "invalid json";
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12349",
                        Data = dataStream,
                        PartitionKey = "partition-5"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));

        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: '12349'", exception.Message);
        Assert.Single(exception.InnerExceptions);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJsonAndIgnorePolicy_IgnoresRecord()
    {
        // Arrange
        var invalidJson = "invalid json";
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12350",
                        Data = dataStream,
                        PartitionKey = "partition-6"
                    }
                }
            }
        };

        var deserializationOptions = new DeserializationOptions
        {
            ErrorPolicy = DeserializationErrorPolicy.IgnoreRecord
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();

        // Act
        var result = await _processor.ProcessAsync(@event, handler, deserializationOptions);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleRecords_ProcessesAllSuccessfully()
    {
        // Arrange
        var testData1 = new TestKinesisMessage { Id = 7, Name = "Message 1" };
        var testData2 = new TestKinesisMessage { Id = 8, Name = "Message 2" };
        var dataStream1 = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testData1)));
        var dataStream2 = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testData2)));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12351",
                        Data = dataStream1,
                        PartitionKey = "partition-7"
                    }
                },
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12352",
                        Data = dataStream2,
                        PartitionKey = "partition-8"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();
        handler.HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler);

        // Assert
        Assert.Equal(2, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);

        await handler.Received(2).HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithHandlerException_FailsRecord()
    {
        // Arrange
        var testData = new TestKinesisMessage { Id = 9, Name = "Exception Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12353",
                        Data = dataStream,
                        PartitionKey = "partition-9"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();
        handler.WhenForAnyArgs(x => x.HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => throw new InvalidOperationException("Handler failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));

        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: '12353'", exception.Message);
        Assert.Single(exception.InnerExceptions);
    }

    [Fact]
    public async Task ProcessAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var testData = new TestKinesisMessage { Id = 12, Name = "Cancellation Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12354",
                        Data = dataStream,
                        PartitionKey = "partition-12"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() =>
            _processor.ProcessAsync(@event, handler, cancellationTokenSource.Token));

        // Verify the cancellation was the root cause
        Assert.Contains("Failed processing record: '12354'", exception.Message);
        Assert.Single(exception.InnerExceptions);
        Assert.IsType<RecordProcessingException>(exception.InnerExceptions.First());
        Assert.IsType<OperationCanceledException>(exception.InnerExceptions.First().InnerException);
    }

    [Fact]
    public void TypedInstance_ReturnsSingletonInstance()
    {
        // Act
        var instance1 = TypedKinesisEventBatchProcessor.TypedInstance;
        var instance2 = TypedKinesisEventBatchProcessor.TypedInstance;

        // Assert
        Assert.Same(instance1, instance2);
        Assert.IsType<TypedKinesisEventBatchProcessor>(instance1);
    }

    [Fact]
    public async Task ProcessAsync_WithNullContext_HandlesGracefully()
    {
        // Arrange
        var testData = new TestKinesisMessage { Id = 13, Name = "Null Context Test" };
        var messageBody = JsonSerializer.Serialize(testData);
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12355",
                        Data = dataStream,
                        PartitionKey = "partition-13"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandlerWithContext<TestKinesisMessage>>();
        handler.HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<ILambdaContext>(), Arg.Any<CancellationToken>())
            .Returns(RecordHandlerResult.None);

        // Act
        var result = await _processor.ProcessAsync(@event, handler, context: null);

        // Assert
        Assert.Single(result.SuccessRecords);
        Assert.Empty(result.FailureRecords);

        await handler.Received(1).HandleAsync(
            Arg.Any<TestKinesisMessage>(),
            Arg.Is<ILambdaContext>(c => c == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyKinesisData_HandlesGracefully()
    {
        // Arrange
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12356",
                        Data = dataStream,
                        PartitionKey = "partition-14"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));

        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: '12356'", exception.Message);
        Assert.Single(exception.InnerExceptions);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithNullKinesisData_HandlesGracefully()
    {
        // Arrange
        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "12357",
                        Data = null,
                        PartitionKey = "partition-15"
                    }
                }
            }
        };

        var handler = Substitute.For<ITypedRecordHandler<TestKinesisMessage>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BatchProcessingException>(() => _processor.ProcessAsync(@event, handler));

        // Verify the exception contains the expected details
        Assert.Contains("Failed processing record: '12357'", exception.Message);
        Assert.Single(exception.InnerExceptions);

        await handler.DidNotReceive().HandleAsync(Arg.Any<TestKinesisMessage>(), Arg.Any<CancellationToken>());
    }

    // Test data classes
    public class TestKinesisMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

// JsonSerializerContext needs to be outside the test class and partial for source generation
[JsonSerializable(typeof(TypedKinesisEventBatchProcessorTests.TestKinesisMessage))]
public partial class TypedKinesisTestJsonSerializerContext : JsonSerializerContext
{
}