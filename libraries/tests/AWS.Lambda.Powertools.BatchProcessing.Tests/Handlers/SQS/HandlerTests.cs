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
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Handler;
using Xunit;
using TestHelper = AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers.Helpers;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS;

[Collection("A Sequential")]
public class HandlerTests : IDisposable
{
    [Fact]
    public Task Sqs_Handler_Using_Attribute()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        
        var function = new HandlerFunction();
    
        var response = function.HandlerUsingAttribute(request);
    
        Assert.Equal(2, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
    
        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_All_Fail_Using_Attribute_Should_Throw_BatchProcessingException()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
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
    public Task Sqs_Handler_Using_Attribute_Fifo()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsFifoMessages
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
    public async Task Sqs_Handler_Using_Attribute_Async()
    {
        // just to make sure
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_PARALLEL_ENABLED", "false");
        
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        
        var function = new HandlerFunction();

        var response = await function.HandlerUsingAttributeAsync(request);

        Assert.Equal(2, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
    }

    [Fact]
    public async Task Sqs_Handler_Using_Attribute_Async_Parallel()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_PARALLEL_ENABLED", "true");
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        
        var function = new HandlerFunction();

        var response = await function.HandlerUsingAttributeAsync(request);

        Assert.Equal(2, response.BatchItemFailures.Count);
        Assert.Contains(response.BatchItemFailures, x => x.ItemIdentifier == "2");
        Assert.Contains(response.BatchItemFailures, x => x.ItemIdentifier == "4");
    }
    
    [Fact]
    public async Task Sqs_Handler_Using_Utility()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        
        var function = new HandlerFunction();
    
        var response = await function.HandlerUsingUtility(request);
    
        Assert.Equal(2, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[1].ItemIdentifier);
    }
    
    [Fact]
    public Task Sqs_Handler_Using_Attribute_Error_Policy_Env_StopOnFirstBatchItemFailure()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_ERROR_HANDLING_POLICY", "StopOnFirstBatchItemFailure");
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
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
    public Task Sqs_Handler_Using_Attribute_Error_Policy_Attribute_StopOnFirstBatchItemFailure()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
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

    [Fact]
    public Task Sqs_Handler_Using_Attribute_All_Fail_Should_Not_Throw_BatchProcessingException_With_Throw_On_Full_Batch_Failure_False_Attribute()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        var function = new HandlerFunction();

        // Act
        var response = function.HandlerUsingAttributeAllFail_ThrowOnFullBatchFailureFalseAttribute(request);

        // Assert
        Assert.Equal(5, response.BatchItemFailures.Count);
        Assert.Equal("1", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("2", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[3].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[4].ItemIdentifier);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_All_Fail_Should_Not_Throw_BatchProcessingException_With_Throw_On_Full_Batch_Failure_False_Env()
    {
        // Arrange
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_THROW_ON_FULL_BATCH_FAILURE", "false");
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        var function = new HandlerFunction();

        // Act
        var response = function.HandlerUsingAttributeAllFail_ThrowOnFullBatchFailureFalseEnv(request);

        // Assert
        Assert.Equal(5, response.BatchItemFailures.Count);
        Assert.Equal("1", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("2", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[3].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[4].ItemIdentifier);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Sqs_Handler_Using_Utility_All_Fail_Should_Not_Throw_BatchProcessingException_With_Throw_On_Full_Batch_Failure_False_Option()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };
        var function = new HandlerFunction();

        // Act
        var response = await function.HandlerUsingUtilityAllFail_ThrowOnFullBatchFailureFalseOption(request);

        // Assert
        Assert.Equal(5, response.BatchItemFailures.Count);
        Assert.Equal("1", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("2", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[3].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[4].ItemIdentifier);
    }

    [Fact]
    public Task Sqs_Fifo_Handler_Using_Attribute_All_Fail_With_Stop_On_First_Error_Attr_Should_Not_Throw_BatchProcessingException_With_Throw_On_Full_Batch_Failure_False_Attribute()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsFifoMessagesWithFirstMessagePoisened
        };
        var function = new HandlerFunction();

        // Act
        var response = function.HandlerUsingAttributeFailAll_StopOnFirstErrorAttr_ThrowOnFullBatchFailureFalseAttr(request);

        // Assert
        Assert.Equal(5, response.BatchItemFailures.Count);
        Assert.Equal("1", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("2", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[3].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[4].ItemIdentifier);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Fifo_Handler_Using_Attribute_All_Fail_With_Stop_On_First_Error_Attr_Should_Not_Throw_BatchProcessingException_With_Throw_On_Full_Batch_Failure_False_Env()
    {
        // Arrange
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_THROW_ON_FULL_BATCH_FAILURE", "false");
        var request = new SQSEvent
        {
            Records = TestHelper.SqsFifoMessagesWithFirstMessagePoisened
        };
        var function = new HandlerFunction();

        // Act
        var response = function.HandlerUsingAttributeFailAll_StopOnFirstErrorAttr_ThrowOnFullBatchFailureFalseEnv(request);

        // Assert
        Assert.Equal(5, response.BatchItemFailures.Count);
        Assert.Equal("1", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("2", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[3].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[4].ItemIdentifier);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Sqs_Fifo_Handler_Using_Utility_All_Fail_With_Stop_On_First_Error_Attr_Should_Not_Throw_BatchProcessingException_With_Throw_On_Full_Batch_Failure_False_Option()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsFifoMessagesWithFirstMessagePoisened
        };
        var function = new HandlerFunction();

        // Act
        var response = await function.HandlerUsingUtility_StopOnFirstErrorOption_ThrowOnFullBatchFailureFalseOption(request);

        // Assert
        Assert.Equal(5, response.BatchItemFailures.Count);
        Assert.Equal("1", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("2", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[2].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[3].ItemIdentifier);
        Assert.Equal("5", response.BatchItemFailures[4].ItemIdentifier);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_THROW_ON_FULL_BATCH_FAILURE", "true");
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_PARALLEL_ENABLED", "false");
        Environment.SetEnvironmentVariable("POWERTOOLS_BATCH_ERROR_HANDLING_POLICY", null);
    }
}