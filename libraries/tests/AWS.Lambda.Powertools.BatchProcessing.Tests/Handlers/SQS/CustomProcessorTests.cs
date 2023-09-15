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
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Handler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TestHelper = AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers.Helpers;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS;

// These tests must run last. Injecting the custom processor will change the other tests expectations 
[Collection("X Sequential")]
public class HandlerCustomProcessorTests
{
    [Fact]
    public Task Sqs_Handler_Using_Attribute_Custom_Processor()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };


        var function = new HandlerFunction();

        var response = function.HandlerUsingAttributeAndCustomBatchProcessor(request);

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
            Records = TestHelper.SqsMessages
        };
        
        var function = new HandlerFunction();

        var response = function.HandlerUsingAttributeAndCustomBatchProcessorProvider(request);
        
        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
        
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task Sqs_Handler_Using_Utility_IoC_Custom_Providers()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        
        var function = new HandlerFunction();
    
        var response = await function.HandlerUsingUtilityFromIoc(request);
    
        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
    }
    
    [Fact]
    public async Task Sqs_Handler_Using_Utility_IoC_Constructor()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };

        var batchProcessor = Services.Provider.GetRequiredService<ISqsBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<ISqsRecordHandler>();
        var function = new HandlerFunction(batchProcessor, recordHandler);

        var response = await function.HandlerUsingUtilityFromIocConstructor(request);

        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
    }
}