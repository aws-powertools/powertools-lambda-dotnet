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

using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Function;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS;

// These tests must run last. Injecting the custom processor will change the other tests expectations 
[Collection("X Sequential")]
public class SqsHandlerCustomProcessorTests
{
    [Fact]
    public Task Sqs_Handler_Using_Attribute_Custom_Processor()
    {
        var request = new SQSEvent
        {
            Records = Helpers.Helpers.SqsMessages
        };


        var function = new SQSHandlerFunction();

        var response = function.SqsHandlerUsingAttributeAndCustomBatchProcessor(request);

        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);

        return Task.CompletedTask;
    }
    
    [Fact]
    public Task Sqs_Handler_Using_Attribute_Custom_Processor_Provider()
    {
        var request = new SQSEvent
        {
            Records = Helpers.Helpers.SqsMessages
        };
        
        var function = new SQSHandlerFunction();

        var response = function.SqsHandlerUsingAttributeAndCustomBatchProcessorProvider(request);
        
        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
        
        return Task.CompletedTask;
    }
}