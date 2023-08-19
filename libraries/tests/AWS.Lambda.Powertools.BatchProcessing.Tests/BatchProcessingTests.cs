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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests
{
    [Collection("Sequential")]
    public class BatchProcessingTests
    {
        [Fact]
        public async Task SqsBatchProcessor_StandardQueue_ContinueOnBatchItemFailure()
        {
            // Arrange
            const string eventSourceArn = "arn:aws:sqs:eu-west-1:123456789012:test-queue";
            var @event = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "1",
                    },
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "2"
                    },
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "3"
                    }
                }
            };
            var batchProcessor = new SqsBatchProcessor();
            var recordHandler = Substitute.For<IRecordHandler<SQSEvent.SQSMessage>>();
            
            recordHandler.WhenForAnyArgs(x => x.HandleAsync(Arg.Any<SQSEvent.SQSMessage>(), Arg.Any<CancellationToken>()))
                .Do( callInfo =>
                {
                    if (callInfo.Arg<SQSEvent.SQSMessage>().MessageId == "1")
                    {
                        throw new InvalidOperationException("Business logic failure.");
                    }
                });

            // Act
            var result = await batchProcessor.ProcessAsync(@event, recordHandler);

            // Assert
            await recordHandler.Received(@event.Records.Count).HandleAsync(Arg.Is<SQSEvent.SQSMessage>(s =>  @event.Records.AsEnumerable().Contains(s) ), Arg.Any<CancellationToken>());
            Assert.Single(result.BatchItemFailuresResponse.BatchItemFailures);
        }

        [Fact]
        public async Task SqsBatchProcessor_FifoQueue_StopOnFirstBatchItemFailure_FirstItemFailsEntireBatch_ThrowsException()
        {
            // Arrange
            const string eventSourceArn = "arn:aws:sqs:eu-west-1:123456789012:test-queue.fifo";
            var @event = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "1",
                    },
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "2"
                    },
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "3"
                    }
                }
            };
            var batchProcessor = new SqsBatchProcessor();
            var recordHandler = Substitute.For<IRecordHandler<SQSEvent.SQSMessage>>();
            
            
            recordHandler.WhenForAnyArgs(x => x.HandleAsync(Arg.Any<SQSEvent.SQSMessage>(), Arg.Any<CancellationToken>()))
                .Do( callInfo =>
                {
                    if (callInfo.Arg<SQSEvent.SQSMessage>().MessageId == "1")
                    {
                        throw new InvalidOperationException("Business logic failure.");
                    }
                });

            // Act
            async Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessBatchAsync() => await batchProcessor.ProcessAsync(@event, recordHandler);

            // Assert
            var batchProcessingException = await Assert.ThrowsAsync<BatchProcessingException>(ProcessBatchAsync);
            Assert.Equal(3, batchProcessingException.InnerExceptions.Count);
            Assert.Equal(2, batchProcessingException.InnerExceptions.OfType<UnprocessedRecordException>().Count());
            Assert.Single(batchProcessingException.InnerExceptions.OfType<RecordProcessingException>());
            await recordHandler.Received(1).HandleAsync(Arg.Is<SQSEvent.SQSMessage>(s =>  @event.Records.AsEnumerable().Contains(s) ), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SqsBatchProcessor_FifoQueue_StopOnFirstBatchItemFailure_SecondItemFailsRemainderOfBatch()
        {
            // Arrange
            const string eventSourceArn = "arn:aws:sqs:eu-west-1:123456789012:test-queue.fifo";
            var @event = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "1",
                    },
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "2"
                    },
                    new()
                    {
                        EventSourceArn = eventSourceArn,
                        MessageId = "3"
                    }
                }
            };
            var batchProcessor = new SqsBatchProcessor();
            var recordHandler = Substitute.For<IRecordHandler<SQSEvent.SQSMessage>>();

            recordHandler.WhenForAnyArgs(x => x.HandleAsync(Arg.Any<SQSEvent.SQSMessage>(), Arg.Any<CancellationToken>()))
                .Do( callInfo =>
            {
                if (callInfo.Arg<SQSEvent.SQSMessage>().MessageId == "2")
                {
                    throw new InvalidOperationException("Business logic failure.");
                }
            });
            
            // Act
            var result = await batchProcessor.ProcessAsync(@event, recordHandler);

            // Assert
            Assert.Equal(2, result.BatchItemFailuresResponse.BatchItemFailures.Count);
            Assert.Contains(result.BatchItemFailuresResponse.BatchItemFailures, x => x.ItemIdentifier == "2");
            Assert.Contains(result.BatchItemFailuresResponse.BatchItemFailures, x => x.ItemIdentifier == "3");
            await recordHandler.Received(2).HandleAsync(Arg.Is<SQSEvent.SQSMessage>(s =>  @event.Records.AsEnumerable().Contains(s) ), Arg.Any<CancellationToken>());
        }
    }
}