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
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for BatchProcessorAttribute validation and error handling scenarios.
/// </summary>
[Collection("BatchProcessorTests")]
public partial class BatchProcessorAttributeValidationTests
{
    public class TestData
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class ValidHandler : ITypedRecordHandler<TestData>
    {
        public async Task<RecordHandlerResult> HandleAsync(TestData data, CancellationToken cancellationToken)
        {
            return await Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class ValidHandlerWithContext : ITypedRecordHandlerWithContext<TestData>
    {
        public async Task<RecordHandlerResult> HandleAsync(TestData data, ILambdaContext context, CancellationToken cancellationToken)
        {
            return await Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class InvalidHandler
    {
        // Does not implement any batch processing interface
    }

    public class ValidHandlerProvider : ITypedRecordHandlerProvider<TestData>
    {
        public ITypedRecordHandler<TestData> Create()
        {
            return new ValidHandler();
        }
    }

    public class InvalidHandlerProvider
    {
        // Missing Create method
    }

    public class ValidHandlerWithContextProvider : ITypedRecordHandlerWithContextProvider<TestData>
    {
        public ITypedRecordHandlerWithContext<TestData> Create()
        {
            return new ValidHandlerWithContext();
        }
    }

    public class InvalidHandlerWithContextProvider
    {
        // Missing Create method
    }

    [JsonSerializable(typeof(TestData))]
    public partial class TestJsonContext : JsonSerializerContext
    {
    }

    public class InvalidJsonContext
    {
        // Does not inherit from JsonSerializerContext
    }

    public class TestFunctions
    {
        [BatchProcessor(TypedRecordHandler = typeof(ValidHandler), TypedRecordHandlerWithContext = typeof(ValidHandlerWithContext))]
        public BatchItemFailuresResponse ProcessWithMultipleHandlers(SQSEvent sqsEvent)
        {
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        [BatchProcessor(JsonSerializerContext = typeof(TestJsonContext), TypedRecordHandler = typeof(ValidHandler))]
        public BatchItemFailuresResponse ProcessWithJsonContext(SQSEvent sqsEvent)
        {
            TypedSqsBatchProcessor.Result?.Clear();
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        [BatchProcessor(JsonSerializerContext = typeof(InvalidJsonContext), TypedRecordHandler = typeof(ValidHandler))]
        public BatchItemFailuresResponse ProcessWithInvalidJsonContext(SQSEvent sqsEvent)
        {
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        [BatchProcessor(TypedRecordHandlerProvider = typeof(ValidHandlerProvider))]
        public BatchItemFailuresResponse ProcessWithValidProvider(SQSEvent sqsEvent)
        {
            TypedSqsBatchProcessor.Result?.Clear();
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        [BatchProcessor(TypedRecordHandlerProvider = typeof(InvalidHandlerProvider))]
        public BatchItemFailuresResponse ProcessWithInvalidProvider(SQSEvent sqsEvent)
        {
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        [BatchProcessor(TypedRecordHandlerWithContextProvider = typeof(ValidHandlerWithContextProvider))]
        public BatchItemFailuresResponse ProcessWithValidContextProvider(SQSEvent sqsEvent, ILambdaContext context)
        {
            TypedSqsBatchProcessor.Result?.Clear();
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        [BatchProcessor(TypedRecordHandlerWithContextProvider = typeof(InvalidHandlerWithContextProvider))]
        public BatchItemFailuresResponse ProcessWithInvalidContextProvider(SQSEvent sqsEvent, ILambdaContext context)
        {
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        public BatchItemFailuresResponse ProcessWithoutAttribute(SQSEvent sqsEvent)
        {
            return new BatchItemFailuresResponse();
        }
    }

    [Fact]
    public void BatchProcessorAttribute_WithMultipleHandlers_ThrowsInvalidOperationException()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"test-1\",\"Name\":\"Test Data\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunctions();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => function.ProcessWithMultipleHandlers(sqsEvent));
    }

    [Fact]
    public void BatchProcessorAttribute_WithJsonContext_ProcessesSuccessfully()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"test-1\",\"Name\":\"Test Data\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunctions();

        // Act
        var result = function.ProcessWithJsonContext(sqsEvent);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.BatchItemFailures);
    }

    [Fact]
    public void BatchProcessorAttribute_WithInvalidJsonContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"test-1\",\"Name\":\"Test Data\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunctions();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => function.ProcessWithInvalidJsonContext(sqsEvent));
    }

    [Fact]
    public void BatchProcessorAttribute_WithValidProvider_ProcessesSuccessfully()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"test-1\",\"Name\":\"Test Data\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunctions();

        // Act
        var result = function.ProcessWithValidProvider(sqsEvent);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.BatchItemFailures);
    }

    [Fact]
    public void BatchProcessorAttribute_WithInvalidProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"test-1\",\"Name\":\"Test Data\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunctions();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => function.ProcessWithInvalidProvider(sqsEvent));
    }

    [Fact]
    public void BatchProcessorAttribute_WithValidContextProvider_ProcessesSuccessfully()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"test-1\",\"Name\":\"Test Data\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var mockContext = Substitute.For<ILambdaContext>();
        var function = new TestFunctions();

        // Act
        var result = function.ProcessWithValidContextProvider(sqsEvent, mockContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.BatchItemFailures);
    }

    [Fact]
    public void BatchProcessorAttribute_WithInvalidContextProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"test-1\",\"Name\":\"Test Data\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var mockContext = Substitute.For<ILambdaContext>();
        var function = new TestFunctions();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => function.ProcessWithInvalidContextProvider(sqsEvent, mockContext));
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithNullArgs_ThrowsArgumentException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandler = typeof(ValidHandler)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => attribute.CreateAspectHandler(null));
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithEmptyArgs_ThrowsArgumentException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandler = typeof(ValidHandler)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => attribute.CreateAspectHandler(new object[0]));
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithInvalidEventType_ThrowsArgumentException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandler = typeof(ValidHandler)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => attribute.CreateAspectHandler(new object[] { "invalid event" }));
    }
}