using TestHelper = AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers.Helpers;
using Xunit;
using System;
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Handler;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB;

[Collection("Sequential")]
public class HandlerValidationTests
{
    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_Bad_Handler()
    {
        // Arrange
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_Bad_Processor()
    {
        // Arrange
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadProcessor(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_Bad_Handler_Provider()
    {
        // Arrange
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadHandlerProvider(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_Bad_Processor_Provider()
    {
        // Arrange
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadProcessorProvider(request));

        return Task.CompletedTask;
    }
    
    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_No_Handler()
    {
        var request = new DynamoDBEvent
        {
            Records = TestHelper.DynamoDbMessages
        };

        var function = new HandlerFunction();

        // Assert
        Assert.Throws<InvalidOperationException>(() => function.HandlerUsingAttributeWithoutHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task DynamoDb_Handler_Using_Attribute_No_Event_Parameter()
    {
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeWithoutEvent(string.Empty));

        return Task.CompletedTask;
    }
}