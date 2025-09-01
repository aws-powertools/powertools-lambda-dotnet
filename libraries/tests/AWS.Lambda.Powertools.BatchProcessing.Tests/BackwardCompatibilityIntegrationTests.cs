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
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Custom;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Integration tests to verify backward compatibility with existing batch processing functionality.
/// These tests ensure that all existing IRecordHandler implementations, BatchProcessorAttribute usage,
/// static access patterns, and ProcessingResult structure remain unchanged.
/// </summary>
[Collection("Sequential")]
public class BackwardCompatibilityIntegrationTests
{
    #region Static Access Pattern Tests

    [Fact]
    public void SqsBatchProcessor_StaticInstance_ShouldBeAccessible()
    {
        // Arrange & Act
        var instance = SqsBatchProcessor.Instance;

        // Assert
        Assert.NotNull(instance);
        Assert.IsAssignableFrom<ISqsBatchProcessor>(instance);
        Assert.IsAssignableFrom<IBatchProcessor<SQSEvent, SQSEvent.SQSMessage>>(instance);
    }

    [Fact]
    public void KinesisEventBatchProcessor_StaticInstance_ShouldBeAccessible()
    {
        // Arrange & Act
        var instance = KinesisEventBatchProcessor.Instance;

        // Assert
        Assert.NotNull(instance);
        Assert.IsAssignableFrom<IKinesisEventBatchProcessor>(instance);
        Assert.IsAssignableFrom<IBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>>(instance);
    }

    [Fact]
    public void DynamoDbStreamBatchProcessor_StaticInstance_ShouldBeAccessible()
    {
        // Arrange & Act
        var instance = DynamoDbStreamBatchProcessor.Instance;

        // Assert
        Assert.NotNull(instance);
        Assert.IsAssignableFrom<IDynamoDbStreamBatchProcessor>(instance);
        Assert.IsAssignableFrom<IBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>>(instance);
    }

    [Fact]
    public void SqsBatchProcessor_StaticResult_ShouldBeAccessible()
    {
        // Arrange & Act
        var result = SqsBatchProcessor.Result;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task KinesisEventBatchProcessor_StaticResult_ShouldBeAccessible()
    {
        // Arrange - First use the processor to initialize the result
        var processor = KinesisEventBatchProcessor.Instance;
        var handler = new TraditionalKinesisRecordHandler();
        var kinesisEvent = CreateSampleKinesisEvent();
        
        // Act - Process to initialize the static result
        await processor.ProcessAsync(kinesisEvent, handler);
        var result = KinesisEventBatchProcessor.Result;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<KinesisEvent.KinesisEventRecord>>(result);
    }

    [Fact]
    public void DynamoDbStreamBatchProcessor_StaticResult_ShouldBeAccessible()
    {
        // Arrange & Act
        var result = DynamoDbStreamBatchProcessor.Result;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>>(result);
    }

    #endregion

    #region Traditional IRecordHandler Tests

    [Fact]
    public async Task SqsBatchProcessor_WithTraditionalRecordHandler_ShouldProcessSuccessfully()
    {
        // Arrange
        var processor = SqsBatchProcessor.Instance;
        var handler = new TraditionalSqsRecordHandler();
        var sqsEvent = CreateSampleSqsEvent();

        // Act
        var result = await processor.ProcessAsync(sqsEvent, handler);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
        Assert.Equal(2, result.BatchRecords.Count);
        Assert.Equal(2, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
    }

    [Fact]
    public async Task KinesisEventBatchProcessor_WithTraditionalRecordHandler_ShouldProcessSuccessfully()
    {
        // Arrange
        var processor = KinesisEventBatchProcessor.Instance;
        var handler = new TraditionalKinesisRecordHandler();
        var kinesisEvent = CreateSampleKinesisEvent();

        // Act
        var result = await processor.ProcessAsync(kinesisEvent, handler);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<KinesisEvent.KinesisEventRecord>>(result);
        Assert.Equal(2, result.BatchRecords.Count);
        Assert.Equal(2, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
    }

    [Fact]
    public async Task DynamoDbStreamBatchProcessor_WithTraditionalRecordHandler_ShouldProcessSuccessfully()
    {
        // Arrange
        var processor = DynamoDbStreamBatchProcessor.Instance;
        var handler = new TraditionalDynamoDbRecordHandler();
        var dynamoDbEvent = CreateSampleDynamoDbEvent();

        // Act
        var result = await processor.ProcessAsync(dynamoDbEvent, handler);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<DynamoDBEvent.DynamodbStreamRecord>>(result);
        Assert.Equal(2, result.BatchRecords.Count);
        Assert.Equal(2, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
        Assert.Empty(result.BatchItemFailuresResponse.BatchItemFailures);
    }

    #endregion

    #region ProcessingResult Structure Compatibility Tests

    [Fact]
    public async Task ProcessingResult_Structure_ShouldRemainUnchanged()
    {
        // Arrange
        var processor = SqsBatchProcessor.Instance;
        var handler = new TraditionalSqsRecordHandler();
        var sqsEvent = CreateSampleSqsEvent();

        // Act
        var result = await processor.ProcessAsync(sqsEvent, handler);

        // Assert - Verify all expected properties exist and have correct types
        Assert.NotNull(result.BatchItemFailuresResponse);
        Assert.IsType<BatchItemFailuresResponse>(result.BatchItemFailuresResponse);
        
        Assert.NotNull(result.BatchRecords);
        Assert.IsType<List<SQSEvent.SQSMessage>>(result.BatchRecords);
        
        Assert.NotNull(result.SuccessRecords);
        Assert.IsType<List<RecordSuccess<SQSEvent.SQSMessage>>>(result.SuccessRecords);
        
        Assert.NotNull(result.FailureRecords);
        Assert.IsType<List<RecordFailure<SQSEvent.SQSMessage>>>(result.FailureRecords);

        // Verify BatchItemFailuresResponse structure
        Assert.NotNull(result.BatchItemFailuresResponse.BatchItemFailures);
        Assert.IsType<List<BatchItemFailuresResponse.BatchItemFailure>>(result.BatchItemFailuresResponse.BatchItemFailures);
    }

    [Fact]
    public async Task ProcessingResult_WithFailures_ShouldMaintainStructure()
    {
        // Arrange
        var processor = SqsBatchProcessor.Instance;
        var handler = new FailingTraditionalSqsRecordHandler();
        var sqsEvent = CreateSampleSqsEvent();
        var processingOptions = new ProcessingOptions
        {
            ThrowOnFullBatchFailure = false // Disable throwing on full batch failure for this test
        };

        // Act
        var result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.BatchRecords.Count);
        Assert.Empty(result.SuccessRecords);
        Assert.Equal(2, result.FailureRecords.Count);
        Assert.Equal(2, result.BatchItemFailuresResponse.BatchItemFailures.Count);

        // Verify failure record structure
        var failureRecord = result.FailureRecords.First();
        Assert.NotNull(failureRecord.Record);
        Assert.NotNull(failureRecord.Exception);
        Assert.NotNull(failureRecord.RecordId);

        // Verify batch item failure structure
        var batchItemFailure = result.BatchItemFailuresResponse.BatchItemFailures.First();
        Assert.NotNull(batchItemFailure.ItemIdentifier);
    }

    #endregion

    #region BatchProcessorAttribute Compatibility Tests

    [Fact]
    public void BatchProcessorAttribute_WithTraditionalHandler_ShouldCreateAspectHandler()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            RecordHandler = typeof(TraditionalSqsRecordHandler)
        };

        // Act
        var handler = attribute.CreateAspectHandler(new object[] { new SQSEvent() });

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void BatchProcessorAttribute_WithRecordHandlerProvider_ShouldCreateAspectHandler()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            RecordHandlerProvider = typeof(TraditionalSqsRecordHandlerProvider)
        };

        // Act
        var handler = attribute.CreateAspectHandler(new object[] { new SQSEvent() });

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void BatchProcessorAttribute_WithCustomBatchProcessor_ShouldCreateAspectHandler()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            BatchProcessor = typeof(CustomSqsBatchProcessor),
            RecordHandler = typeof(TraditionalSqsRecordHandler)
        };

        // Act
        var handler = attribute.CreateAspectHandler(new object[] { new SQSEvent() });

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void BatchProcessorAttribute_WithBatchProcessorProvider_ShouldCreateAspectHandler()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            BatchProcessorProvider = typeof(CustomSqsBatchProcessorProvider),
            RecordHandler = typeof(TraditionalSqsRecordHandler)
        };

        // Act
        var handler = attribute.CreateAspectHandler(new object[] { new SQSEvent() });

        // Assert
        Assert.NotNull(handler);
    }

    #endregion

    #region Mixed Usage Compatibility Tests

    [Fact]
    public async Task BatchProcessor_MixedUsage_TraditionalAndDirectCalls_ShouldWork()
    {
        // Arrange
        var processor = SqsBatchProcessor.Instance;
        var traditionalHandler = new TraditionalSqsRecordHandler();
        var sqsEvent = CreateSampleSqsEvent();

        // Act - First use traditional handler
        var result1 = await processor.ProcessAsync(sqsEvent, traditionalHandler);

        // Then use direct processor call (simulating mixed usage)
        var result2 = await processor.ProcessAsync(sqsEvent, traditionalHandler, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.BatchRecords.Count, result2.BatchRecords.Count);
        Assert.Equal(result1.SuccessRecords.Count, result2.SuccessRecords.Count);
    }

    #endregion

    #region Error Handling Compatibility Tests

    [Fact]
    public async Task BatchProcessor_ErrorHandling_ShouldMaintainExistingBehavior()
    {
        // Arrange
        var processor = SqsBatchProcessor.Instance;
        var handler = new PartiallyFailingTraditionalSqsRecordHandler();
        var sqsEvent = CreateSampleSqsEvent();

        // Act
        var result = await processor.ProcessAsync(sqsEvent, handler);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.BatchRecords.Count);
        Assert.Single(result.SuccessRecords);
        Assert.Single(result.FailureRecords);
        Assert.Single(result.BatchItemFailuresResponse.BatchItemFailures);

        // Verify error structure remains the same
        var failureRecord = result.FailureRecords.First();
        Assert.Contains("Failed processing record", failureRecord.Exception.Message);
        Assert.IsType<RecordProcessingException>(failureRecord.Exception);
    }

    #endregion

    #region Performance Compatibility Tests

    [Fact]
    public async Task BatchProcessor_ParallelProcessing_ShouldMaintainCompatibility()
    {
        // Arrange
        var processor = SqsBatchProcessor.Instance;
        var handler = new TraditionalSqsRecordHandler();
        var sqsEvent = CreateLargeSampleSqsEvent(10);
        var processingOptions = new ProcessingOptions
        {
            BatchParallelProcessingEnabled = true,
            MaxDegreeOfParallelism = 4
        };

        // Act
        var result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.BatchRecords.Count);
        Assert.Equal(10, result.SuccessRecords.Count);
        Assert.Empty(result.FailureRecords);
    }

    #endregion

    #region Helper Methods and Classes

    private static SQSEvent CreateSampleSqsEvent()
    {
        return new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "msg-1",
                    Body = JsonSerializer.Serialize(new { Id = 1, Name = "Product 1" }),
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                },
                new()
                {
                    MessageId = "msg-2",
                    Body = JsonSerializer.Serialize(new { Id = 2, Name = "Product 2" }),
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };
    }

    private static SQSEvent CreateLargeSampleSqsEvent(int recordCount)
    {
        var records = new List<SQSEvent.SQSMessage>();
        for (int i = 1; i <= recordCount; i++)
        {
            records.Add(new SQSEvent.SQSMessage
            {
                MessageId = $"msg-{i}",
                Body = JsonSerializer.Serialize(new { Id = i, Name = $"Product {i}" }),
                EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
            });
        }

        return new SQSEvent { Records = records };
    }

    private static KinesisEvent CreateSampleKinesisEvent()
    {
        return new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "1",
                        Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Id = 1, Name = "Event 1" })))
                    }
                },
                new()
                {
                    Kinesis = new KinesisEvent.Record
                    {
                        SequenceNumber = "2",
                        Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Id = 2, Name = "Event 2" })))
                    }
                }
            }
        };
    }

    private static DynamoDBEvent CreateSampleDynamoDbEvent()
    {
        return new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord>
            {
                new()
                {
                    Dynamodb = new DynamoDBEvent.StreamRecord
                    {
                        SequenceNumber = "seq-1"
                    }
                },
                new()
                {
                    Dynamodb = new DynamoDBEvent.StreamRecord
                    {
                        SequenceNumber = "seq-2"
                    }
                }
            }
        };
    }

    // Traditional record handlers for testing
    public class TraditionalSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
    {
        public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class TraditionalKinesisRecordHandler : IRecordHandler<KinesisEvent.KinesisEventRecord>
    {
        public Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class TraditionalDynamoDbRecordHandler : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>
    {
        public Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class FailingTraditionalSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
    {
        public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated failure");
        }
    }

    public class PartiallyFailingTraditionalSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
    {
        public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            if (record.MessageId == "msg-1")
            {
                throw new InvalidOperationException("Simulated failure for first record");
            }
            return Task.FromResult(RecordHandlerResult.None);
        }
    }

    // Provider classes for testing
    public class TraditionalSqsRecordHandlerProvider : IRecordHandlerProvider<SQSEvent.SQSMessage>
    {
        public IRecordHandler<SQSEvent.SQSMessage> Create()
        {
            return new TraditionalSqsRecordHandler();
        }
    }

    public class CustomSqsBatchProcessor : SqsBatchProcessor
    {
        // Custom implementation for testing
    }

    public class CustomSqsBatchProcessorProvider : IBatchProcessorProvider<SQSEvent, SQSEvent.SQSMessage>
    {
        public IBatchProcessor<SQSEvent, SQSEvent.SQSMessage> Create()
        {
            return new CustomSqsBatchProcessor();
        }
    }

    #endregion
}