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
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for BatchProcessorAttribute with typed handlers.
/// </summary>
[Collection("BatchProcessorTests")]
public class BatchProcessorAttributeTypedHandlerTests
{
    /// <summary>
    /// Test data class for testing typed record handlers.
    /// </summary>
    public class Order
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public List<string> Products { get; set; } = new();
    }

    /// <summary>
    /// Test typed record handler for Order.
    /// </summary>
    public class OrderRecordHandler : ITypedRecordHandler<Order>
    {
        public async Task<RecordHandlerResult> HandleAsync(Order order, CancellationToken cancellationToken)
        {
            // Simulate processing
            if (order.Id == "fail")
            {
                throw new ArgumentException("Simulated failure");
            }

            return await Task.FromResult(RecordHandlerResult.None);
        }
    }

    /// <summary>
    /// Test typed record handler with context for Order.
    /// </summary>
    public class OrderRecordHandlerWithContext : ITypedRecordHandlerWithContext<Order>
    {
        public async Task<RecordHandlerResult> HandleAsync(Order order, ILambdaContext context, CancellationToken cancellationToken)
        {
            // Simulate processing with context
            if (order.Id == "fail")
            {
                throw new ArgumentException("Simulated failure");
            }

            return await Task.FromResult(RecordHandlerResult.None);
        }
    }

    /// <summary>
    /// Test function using typed record handler attribute.
    /// </summary>
    public class TestFunction
    {
        [BatchProcessor(TypedRecordHandler = typeof(OrderRecordHandler))]
        public BatchItemFailuresResponse ProcessOrdersWithTypedHandler(SQSEvent sqsEvent)
        {
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }

        [BatchProcessor(TypedRecordHandlerWithContext = typeof(OrderRecordHandlerWithContext))]
        public BatchItemFailuresResponse ProcessOrdersWithTypedHandlerAndContext(SQSEvent sqsEvent, ILambdaContext context)
        {
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }
    }

    [Fact]
    public void ProcessOrdersWithTypedHandler_ValidOrders_ProcessesSuccessfully()
    {
        // Clear any previous test state
        TypedSqsBatchProcessor.Result?.Clear();
        
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"order-1\",\"Name\":\"Test Order\",\"Amount\":99.99,\"Products\":[\"Product A\",\"Product B\"]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "2",
                    Body = "{\"Id\":\"order-2\",\"Name\":\"Another Order\",\"Amount\":149.99,\"Products\":[\"Product C\"]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunction();

        // Act
        var result = function.ProcessOrdersWithTypedHandler(sqsEvent);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.BatchItemFailures);
    }

    [Fact]
    public void ProcessOrdersWithTypedHandler_OneFailure_ReportsPartialFailure()
    {
        // Clear any previous test state
        TypedSqsBatchProcessor.Result?.Clear();
        
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"order-1\",\"Name\":\"Test Order\",\"Amount\":99.99,\"Products\":[\"Product A\"]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "2",
                    Body = "{\"Id\":\"fail\",\"Name\":\"Failing Order\",\"Amount\":0,\"Products\":[]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunction();

        // Act
        var result = function.ProcessOrdersWithTypedHandler(sqsEvent);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.BatchItemFailures);
        Assert.Equal("2", result.BatchItemFailures[0].ItemIdentifier);
    }

    [Fact]
    public void ProcessOrdersWithTypedHandlerAndContext_ValidOrders_ProcessesSuccessfully()
    {
        // Clear any previous test state
        TypedSqsBatchProcessor.Result?.Clear();
        
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"order-1\",\"Name\":\"Test Order\",\"Amount\":99.99,\"Products\":[\"Product A\"]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var mockContext = Substitute.For<ILambdaContext>();
        var function = new TestFunction();

        // Act
        var result = function.ProcessOrdersWithTypedHandlerAndContext(sqsEvent, mockContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.BatchItemFailures);
    }

    [Fact]
    public void ProcessOrdersWithTypedHandlerAndContext_OneFailure_ReportsPartialFailure()
    {
        // Clear any previous test state
        TypedSqsBatchProcessor.Result?.Clear();
        
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"order-1\",\"Name\":\"Valid Order\",\"Amount\":99.99,\"Products\":[\"Product A\"]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "2",
                    Body = "{\"Id\":\"fail\",\"Name\":\"Failing Order\",\"Amount\":0,\"Products\":[]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var mockContext = Substitute.For<ILambdaContext>();
        var function = new TestFunction();

        // Act
        var result = function.ProcessOrdersWithTypedHandlerAndContext(sqsEvent, mockContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.BatchItemFailures);
        Assert.Equal("2", result.BatchItemFailures[0].ItemIdentifier);
    }

    [Fact]
    public void ProcessOrdersWithTypedHandler_InvalidJson_HandlesDeserializationError()
    {
        // Clear any previous test state
        TypedSqsBatchProcessor.Result?.Clear();
        
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"order-1\",\"Name\":\"Valid Order\",\"Amount\":99.99,\"Products\":[\"Product A\"]}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "2",
                    Body = "invalid json",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunction();

        // Act
        var result = function.ProcessOrdersWithTypedHandler(sqsEvent);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.BatchItemFailures);
        Assert.Equal("2", result.BatchItemFailures[0].ItemIdentifier);
    }
}