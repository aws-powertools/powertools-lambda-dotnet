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
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Handler;
using Xunit;
using TestHelper = AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers.Helpers;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB;

[Collection("A Sequential")]
public class HandlerTests : IDisposable
{
    
    [Fact]
    public Task DynamoDb_Handler_Using_Attribute()
    {
        var request = new DynamoDBEvent()
        {
            Records = TestHelper.DynamoDbMessages
        };

        var function = new HandlerFunction();

        var response = function.HandlerUsingAttribute(request);

        Assert.Equal(2, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);

        return Task.CompletedTask;
    }
    
    [Fact]
    public Task DynamoDb_Handler_All_Fail_Using_Attribute_Should_Throw_BatchProcessingException()
    {
        var request = new DynamoDBEvent()
        {
            Records = TestHelper.DynamoDbMessages
        };
        
        var function = new HandlerFunction();
        
        // Assert
        var exception = Assert.Throws<BatchProcessingException>(() => function.HandlerUsingAttributeAllFail(request));
        
        Assert.Equal(5, exception.InnerExceptions.Count);
        Assert.Equal("Failed processing record: '1'. See inner exception for details.", exception.InnerExceptions[0].Message);
        Assert.Equal("Failed processing record: '2'. See inner exception for details.", exception.InnerExceptions[1].Message);
        Assert.Equal("Failed processing record: '3'. See inner exception for details.", exception.InnerExceptions[2].Message);
        Assert.Equal("Failed processing record: '4'. See inner exception for details.", exception.InnerExceptions[3].Message);
        Assert.Equal("Failed processing record: '5'. See inner exception for details.", exception.InnerExceptions[4].Message);
        
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task DynamoDb_Handler_Using_Attribute_Async()
    {
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };

        var function = new HandlerFunction();

        var response = await function.HandlerUsingAttributeAsync(request);

        Assert.Equal(2, response.BatchItemFailures.Count);
    }

    [Fact]
    public async Task DynamoDb_Handler_Using_Attribute_Async_Parallel()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_PARALLEL_ENABLED", "true");
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };
        
        var function = new HandlerFunction();

        var response = await function.HandlerUsingAttributeAsync(request);

        Assert.Equal(2, response.BatchItemFailures.Count);
        Assert.Contains(response.BatchItemFailures, x => x.ItemIdentifier == "2");
        Assert.Contains(response.BatchItemFailures, x => x.ItemIdentifier == "4");
    }
    
    [Fact]
    public async Task DynamoDb_Handler_Using_Utility()
    {
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };
        
        var function = new HandlerFunction();
    
        var response = await function.HandlerUsingUtility(request);
    
        Assert.Equal(2, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
    }
    
    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_Error_Policy_Env_StopOnFirstBatchItemFailure()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_ERROR_HANDLING_POLICY", "StopOnFirstBatchItemFailure");
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };
        
        var function = new HandlerFunction();
    
        var response = function.HandlerUsingAttribute(request);
    
        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[3].ItemIdentifier);
    
        return Task.CompletedTask;
    }
    
    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_Error_Policy_Attribute_StopOnFirstBatchItemFailure()
    {
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };
        
        var function = new HandlerFunction();
    
        var response = function.HandlerUsingAttributeErrorPolicy(request);
    
        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[3].ItemIdentifier);
    
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_PARALLEL_ENABLED", "false");
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_ERROR_HANDLING_POLICY", null);
    }
}