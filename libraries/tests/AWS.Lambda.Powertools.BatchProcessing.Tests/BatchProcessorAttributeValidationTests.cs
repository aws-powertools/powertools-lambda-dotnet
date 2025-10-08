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

    public class ValidSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
    {
        public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            return await Task.FromResult(RecordHandlerResult.None);
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

    // Test classes to trigger constructor exceptions
    public class FailingBatchProcessor : IBatchProcessor<SQSEvent, SQSEvent.SQSMessage>
    {
        public FailingBatchProcessor()
        {
            throw new InvalidOperationException("Constructor failed");
        }

        public ProcessingResult<SQSEvent.SQSMessage> ProcessingResult => throw new NotImplementedException();
        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync(SQSEvent @event, IRecordHandler<SQSEvent.SQSMessage> recordHandler, ProcessingOptions processingOptions) => throw new NotImplementedException();
        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync(SQSEvent @event, IRecordHandler<SQSEvent.SQSMessage> recordHandler) => throw new NotImplementedException();
        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync(SQSEvent @event, IRecordHandler<SQSEvent.SQSMessage> recordHandler, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public class FailingBatchProcessorProvider : IBatchProcessorProvider<SQSEvent, SQSEvent.SQSMessage>
    {
        public FailingBatchProcessorProvider()
        {
            throw new InvalidOperationException("Provider constructor failed");
        }

        public IBatchProcessor<SQSEvent, SQSEvent.SQSMessage> Create() => throw new NotImplementedException();
    }

    public class FailingRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
    {
        public FailingRecordHandler()
        {
            throw new InvalidOperationException("Handler constructor failed");
        }

        public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public class FailingRecordHandlerProvider : IRecordHandlerProvider<SQSEvent.SQSMessage>
    {
        public FailingRecordHandlerProvider()
        {
            throw new InvalidOperationException("Handler provider constructor failed");
        }

        public IRecordHandler<SQSEvent.SQSMessage> Create() => throw new NotImplementedException();
    }

    public class FailingTypedHandler : ITypedRecordHandler<TestData>
    {
        public FailingTypedHandler()
        {
            throw new InvalidOperationException("Typed handler constructor failed");
        }

        public Task<RecordHandlerResult> HandleAsync(TestData data, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public class FailingTypedHandlerWithContext : ITypedRecordHandlerWithContext<TestData>
    {
        public FailingTypedHandlerWithContext()
        {
            throw new InvalidOperationException("Typed handler with context constructor failed");
        }

        public Task<RecordHandlerResult> HandleAsync(TestData data, ILambdaContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public class FailingJsonSerializerContext : JsonSerializerContext
    {
        public FailingJsonSerializerContext() : base(null)
        {
            throw new InvalidOperationException("JsonSerializerContext constructor failed");
        }

        protected override System.Text.Json.JsonSerializerOptions GeneratedSerializerOptions => throw new NotImplementedException();
        public override System.Text.Json.Serialization.Metadata.JsonTypeInfo GetTypeInfo(Type type) => throw new NotImplementedException();
    }

    public class FailingTypedHandlerProvider
    {
        public FailingTypedHandlerProvider()
        {
            throw new InvalidOperationException("Typed handler provider constructor failed");
        }

        public ITypedRecordHandler<TestData> Create() => throw new NotImplementedException();
    }

    public class FailingTypedHandlerWithContextProvider
    {
        public FailingTypedHandlerWithContextProvider()
        {
            throw new InvalidOperationException("Typed handler with context provider constructor failed");
        }

        public ITypedRecordHandlerWithContext<TestData> Create() => throw new NotImplementedException();
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingBatchProcessor_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            BatchProcessor = typeof(FailingBatchProcessor),
            RecordHandler = typeof(ValidSqsRecordHandler)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of: 'FailingBatchProcessor'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingBatchProcessorProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            BatchProcessorProvider = typeof(FailingBatchProcessorProvider),
            RecordHandler = typeof(ValidSqsRecordHandler)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of batch processor using provider: 'FailingBatchProcessorProvider'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingRecordHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            RecordHandler = typeof(FailingRecordHandler)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of: 'FailingRecordHandler'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingRecordHandlerProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            RecordHandlerProvider = typeof(FailingRecordHandlerProvider)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of record handler using provider: 'FailingRecordHandlerProvider'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingTypedHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandler = typeof(FailingTypedHandler)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of: 'FailingTypedHandler'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingTypedHandlerWithContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandlerWithContext = typeof(FailingTypedHandlerWithContext)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of: 'FailingTypedHandlerWithContext'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingJsonSerializerContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandler = typeof(ValidHandler),
            JsonSerializerContext = typeof(FailingJsonSerializerContext)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of JsonSerializerContext: 'FailingJsonSerializerContext'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingTypedHandlerProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandlerProvider = typeof(FailingTypedHandlerProvider)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of typed record handler using provider: 'FailingTypedHandlerProvider'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithFailingTypedHandlerWithContextProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandlerWithContextProvider = typeof(FailingTypedHandlerWithContextProvider)
        };
        var sqsEvent = new SQSEvent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => attribute.CreateAspectHandler(new object[] { sqsEvent }));
        Assert.Contains("Error during creation of typed record handler with context using provider: 'FailingTypedHandlerWithContextProvider'", ex.Message);
    }

    [Fact]
    public void BatchProcessorAttribute_CreateAspectHandler_WithErrorHandlingPolicyOverride_UsesOverride()
    {
        // Arrange
        var attribute = new BatchProcessorAttribute
        {
            RecordHandler = typeof(ValidSqsRecordHandler),
            ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure
        };
        var sqsEvent = new SQSEvent();

        // Act
        var handler = attribute.CreateAspectHandler(new object[] { sqsEvent });

        // Assert - Should not throw, policy should be applied
        Assert.NotNull(handler);
    }
}