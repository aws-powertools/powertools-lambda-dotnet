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
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Simple test to verify typed handler attribute works.
/// </summary>
public class TypedHandlerAttributeSimpleTest
{
    public class SimpleOrder
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class SimpleOrderHandler : ITypedRecordHandler<SimpleOrder>
    {
        public async Task<RecordHandlerResult> HandleAsync(SimpleOrder order, CancellationToken cancellationToken)
        {
            return await Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class TestFunction
    {
        [BatchProcessor(TypedRecordHandler = typeof(SimpleOrderHandler))]
        public BatchItemFailuresResponse ProcessOrders(SQSEvent sqsEvent)
        {
            return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
        }
    }

    [Fact]
    public void TypedHandlerAttribute_BasicTest_DoesNotThrowException()
    {
        // Arrange
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "1",
                    Body = "{\"Id\":\"order-1\",\"Name\":\"Test Order\"}",
                    EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
                }
            }
        };

        var function = new TestFunction();

        // Act & Assert - Should not throw the NotSupportedException anymore
        var result = function.ProcessOrders(sqsEvent);
        Assert.NotNull(result);
    }
}