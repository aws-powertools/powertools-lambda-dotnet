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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using Xunit;
using Xunit.Abstractions;

namespace HelloWorld.Tests
{
    public class FunctionTest
    {
        private readonly ITestOutputHelper _output;

        public FunctionTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public Task TestSqs()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "3",
                        Body = "{\"Id\":3,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "4",
                        Body = "{\"Id\":4,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "5",
                        Body = "{\"Id\":5,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };


            // Act
            var function = new Function();
            var response = function.SqsHandlerUsingAttribute(request);
            
            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine(failure.ItemIdentifier);
            }
            
            // Assert
            Assert.Equal(2, response.BatchItemFailures.Count);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
            return Task.CompletedTask;
        }
        
        [Fact]
        public async Task TestSqsFiFo()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue.fifo"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue.fifo"
                    },
                    new()
                    {
                        MessageId = "3",
                        Body = "{\"Id\":3,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue.fifo"
                    },
                    new()
                    {
                        MessageId = "4",
                        Body = "{\"Id\":4,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue.fifo"
                    },
                    new()
                    {
                        MessageId = "5",
                        Body = "{\"Id\":5,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue.fifo"
                    },
                }
            };

            // Act
            var function = new Function();

            var response = await function.HandlerUsingUtilityFromIoc(request);
            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine(failure.ItemIdentifier);
            }
            
            // Assert
            Assert.Equal(4, response.BatchItemFailures.Count);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
            Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
            Assert.Equal("5", response.BatchItemFailures[3].ItemIdentifier);
        }
        
        [Fact]
        public Task TestKinesis()
        {
            // Arrange
            var request = new KinesisEvent()
            {
                Records = new List<KinesisEvent.KinesisEventRecord>()
                {
                    new()
                    {
                        Kinesis = new KinesisEvent.Record()
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":1,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "1"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record()
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(Encoding.UTF8.GetBytes("fail")),
                            SequenceNumber = "2"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record()
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":3,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "3"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record()
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":4,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "4"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record()
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":5,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "5"
                        }
                    },
                }
            };

            // Act
            var function = new Function();
            var response = function.KinesisEventHandlerUsingAttribute(request);
            
            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine(failure.ItemIdentifier);
            }

            // Assert
            Assert.Equal(2, response.BatchItemFailures.Count);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
            return Task.CompletedTask;
        }

        [Fact]
        public Task TestDynamoDb()
        {
            // Arrange
            var request = new DynamoDBEvent
            {
                Records = new List<DynamoDBEvent.DynamodbStreamRecord>
                {
                    new()
                    {
                        EventID = "1",
                        Dynamodb = new DynamoDBEvent.StreamRecord
                        {
                            Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Id", new DynamoDBEvent.AttributeValue { N = "1" } }
                            },
                            NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Product", new DynamoDBEvent.AttributeValue { S = "{\"Id\":1,\"Name\":\"product-name\",\"Price\":14}" } }
                            },
                            SequenceNumber = "1"
                        }
                    },
                    new()
                    {
                        EventID = "1",
                        Dynamodb = new DynamoDBEvent.StreamRecord
                        {
                            Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Id", new DynamoDBEvent.AttributeValue { N = "1" } }
                            },
                            NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Product", new DynamoDBEvent.AttributeValue { S = "failure" } }
                            },
                            SequenceNumber = "2"
                        }
                    },
                    new()
                    {
                        EventID = "1",
                        Dynamodb = new DynamoDBEvent.StreamRecord
                        {
                            Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Id", new DynamoDBEvent.AttributeValue { N = "1" } }
                            },
                            NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Product", new DynamoDBEvent.AttributeValue { S = "{\"Id\":1,\"Name\":\"product-name\",\"Price\":14}" } }
                            },
                            SequenceNumber = "3"
                        }
                    },
                }
            };

            // Act
            var function = new Function();
            var response = function.DynamoDbStreamHandlerUsingAttribute(request);
            
            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine(failure.ItemIdentifier);
            }
            
            // Assert
            Assert.Single(response.BatchItemFailures);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            return Task.CompletedTask;
        }

        #region Additional Handler Tests

        [Fact]
        public Task TestSqsWithErrorPolicy()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-1\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "3",
                        Body = "{\"Id\":3,\"Name\":\"product-3\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };

            // Act
            var function = new Function();
            var response = function.SqsHandlerUsingAttributeWithErrorPolicy(request);

            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[ErrorPolicy] Failed item: {failure.ItemIdentifier}");
            }

            // Assert - StopOnFirstBatchItemFailure should stop after first failure and return remaining
            Assert.Equal(2, response.BatchItemFailures.Count);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
            return Task.CompletedTask;
        }

        [Fact]
        public Task TestHandlerUsingAttributeAndCustomRecordHandlerProvider()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-1\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };

            // Act
            var function = new Function();
            var response = function.HandlerUsingAttributeAndCustomRecordHandlerProvider(request);

            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[CustomRecordHandlerProvider] Failed item: {failure.ItemIdentifier}");
            }

            // Assert
            Assert.Single(response.BatchItemFailures);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            return Task.CompletedTask;
        }

        [Fact]
        public Task TestHandlerUsingAttributeAndCustomBatchProcessor()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-1\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };

            // Act
            var function = new Function();
            var response = function.HandlerUsingAttributeAndCustomBatchProcessor(request);

            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[CustomBatchProcessor] Failed item: {failure.ItemIdentifier}");
            }

            // Assert
            Assert.Single(response.BatchItemFailures);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            return Task.CompletedTask;
        }

        [Fact]
        public Task TestHandlerUsingAttributeAndCustomBatchProcessorProvider()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-1\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };

            // Act
            var function = new Function();
            var response = function.HandlerUsingAttributeAndCustomBatchProcessorProvider(request);

            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[CustomBatchProcessorProvider] Failed item: {failure.ItemIdentifier}");
            }

            // Assert
            Assert.Single(response.BatchItemFailures);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task TestHandlerUsingUtility()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-1\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "{\"Id\":2,\"Name\":\"product-2\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };

            // Act
            var function = new Function();
            var response = await function.HandlerUsingUtility(request);

            // Assert - inline handler just logs, no failures expected
            Assert.Empty(response.BatchItemFailures);
        }

        #endregion

        #region Typed Batch Processing Tests

        [Fact]
        public async Task TestTypedSqs()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-1\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "3",
                        Body = "{\"Id\":3,\"Name\":\"product-3\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "4",
                        Body = "{\"Id\":4,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "5",
                        Body = "{\"Id\":5,\"Name\":\"product-5\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };

            // Act
            var function = new Function();
            var response = await function.TypedSqsHandler(request);
            
            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[Typed SQS] Failed item: {failure.ItemIdentifier}");
            }
            
            // Assert - message 2 fails deserialization, message 4 fails in handler
            Assert.Equal(2, response.BatchItemFailures.Count);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
        }

        [Fact]
        public async Task TestTypedKinesis()
        {
            // Arrange
            var request = new KinesisEvent
            {
                Records = new List<KinesisEvent.KinesisEventRecord>
                {
                    new()
                    {
                        Kinesis = new KinesisEvent.Record
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":1,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "1"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(Encoding.UTF8.GetBytes("fail")),
                            SequenceNumber = "2"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":3,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "3"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":4,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "4"
                        }
                    },
                    new()
                    {
                        Kinesis = new KinesisEvent.Record
                        {
                            PartitionKey = "1",
                            Data = new MemoryStream(
                                Encoding.UTF8.GetBytes("{\"Id\":5,\"Name\":\"product-name\",\"Price\":14}")),
                            SequenceNumber = "5"
                        }
                    },
                }
            };

            // Act
            var function = new Function();
            var response = await function.TypedKinesisHandler(request);
            
            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[Typed Kinesis] Failed item: {failure.ItemIdentifier}");
            }

            // Assert - record 2 fails deserialization, record 4 fails in handler
            Assert.Equal(2, response.BatchItemFailures.Count);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
        }

        [Fact]
        public async Task TestTypedDynamoDb()
        {
            // Arrange
            var request = new DynamoDBEvent
            {
                Records = new List<DynamoDBEvent.DynamodbStreamRecord>
                {
                    new()
                    {
                        EventID = "1",
                        EventName = "INSERT",
                        Dynamodb = new DynamoDBEvent.StreamRecord
                        {
                            Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Id", new DynamoDBEvent.AttributeValue { N = "1" } }
                            },
                            NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Product", new DynamoDBEvent.AttributeValue { S = "{\"Id\":1,\"Name\":\"product-name\",\"Price\":14}" } }
                            },
                            SequenceNumber = "1"
                        }
                    },
                    new()
                    {
                        EventID = "2",
                        EventName = "INSERT",
                        Dynamodb = new DynamoDBEvent.StreamRecord
                        {
                            Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Id", new DynamoDBEvent.AttributeValue { N = "2" } }
                            },
                            NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Product", new DynamoDBEvent.AttributeValue { S = "failure" } }
                            },
                            SequenceNumber = "2"
                        }
                    },
                    new()
                    {
                        EventID = "3",
                        EventName = "INSERT",
                        Dynamodb = new DynamoDBEvent.StreamRecord
                        {
                            Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Id", new DynamoDBEvent.AttributeValue { N = "3" } }
                            },
                            NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                            {
                                { "Product", new DynamoDBEvent.AttributeValue { S = "{\"Id\":3,\"Name\":\"product-name\",\"Price\":14}" } }
                            },
                            SequenceNumber = "3"
                        }
                    },
                }
            };

            // Act
            var function = new Function();
            var response = await function.TypedDynamoDbHandler(request);
            
            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[Typed DynamoDB] Failed item: {failure.ItemIdentifier}");
            }
            
            // Assert - record 2 fails in handler due to "failure" product
            Assert.Single(response.BatchItemFailures);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        }

        [Fact]
        public async Task TestTypedSqsInline()
        {
            // Arrange
            var request = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new()
                    {
                        MessageId = "1",
                        Body = "{\"Id\":1,\"Name\":\"product-1\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "2",
                        Body = "fail",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "3",
                        Body = "{\"Id\":3,\"Name\":\"product-3\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                    new()
                    {
                        MessageId = "4",
                        Body = "{\"Id\":4,\"Name\":\"product-4\",\"Price\":14}",
                        EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
                    },
                }
            };

            // Act
            var function = new Function();
            var response = await function.TypedSqsHandlerInline(request);

            foreach (var failure in response.BatchItemFailures)
            {
                _output.WriteLine($"[Typed SQS Inline] Failed item: {failure.ItemIdentifier}");
            }

            // Assert - message 2 fails deserialization, message 4 fails in handler (id == 4)
            Assert.Equal(2, response.BatchItemFailures.Count);
            Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
            Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
        }

        #endregion
    }
}
