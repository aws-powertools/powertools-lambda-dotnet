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
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Function;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS;

[Collection("Sequential")]
public class SqsHandlerValidationTests
{
    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Handler()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = Helpers.Helpers.SqsMessages
        };

        // Act
        var function = new SQSHandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.SqsHandlerUsingAttributeBadHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Processor()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = Helpers.Helpers.SqsMessages
        };

        // Act
        var function = new SQSHandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.SqsHandlerUsingAttributeBadProcessor(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Handler_Provider()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = Helpers.Helpers.SqsMessages
        };

        // Act
        var function = new SQSHandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.SqsHandlerUsingAttributeBadHandlerProvider(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_Bad_Processor_Provider()
    {
        // Arrange
        var request = new SQSEvent
        {
            Records = Helpers.Helpers.SqsMessages
        };

        // Act
        var function = new SQSHandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.SqsHandlerUsingAttributeBadProcessorProvider(request));

        return Task.CompletedTask;
    }
    
    [Fact]
    public Task Sqs_Handler_Using_Attribute_No_Handler()
    {
        var request = new SQSEvent
        {
            Records = Helpers.Helpers.SqsMessages
        };

        var function = new SQSHandlerFunction();

        // Assert
        Assert.Throws<InvalidOperationException>(() => function.SqsHandlerUsingAttributeWithoutHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Sqs_Handler_Using_Attribute_No_Event_Parameter()
    {
        var function = new SQSHandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.SqsHandlerUsingAttributeWithoutEvent(string.Empty));

        return Task.CompletedTask;
    }
}