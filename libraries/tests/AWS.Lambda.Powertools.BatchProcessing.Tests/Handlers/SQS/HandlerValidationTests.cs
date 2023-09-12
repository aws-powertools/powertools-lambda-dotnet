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
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Handler;
using Xunit;
using TestHelper = AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers.Helpers;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS;

[Collection("Sequential")]
public class HandlerValidationTests
{
    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Handler()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Processor()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadProcessor(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Handler_Provider()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadHandlerProvider(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Processor_Provider()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadProcessorProvider(request));

        return Task.CompletedTask;
    }
    
    [Fact]
    public Task Sqs_Handler_Using_Attribute_No_Handler()
    {
        var request = new SQSEvent
        {
            Records = TestHelper.SqsMessages
        };

        var function = new HandlerFunction();

        // Assert
        Assert.Throws<InvalidOperationException>(() => function.HandlerUsingAttributeWithoutHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_No_Event_Parameter()
    {
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeWithoutEvent(string.Empty));

        return Task.CompletedTask;
    }
}