using System;
using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Handler;
using Xunit;
using TestHelper = AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers.Helpers;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis;

[Collection("Sequential")]
public class HandlerValidationTests
{
    [Fact]
    public Task Kinesis_Handler_Using_Attribute_Bad_Handler()
    {
        // Arrange
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Kinesis_Handler_Using_Attribute_Bad_Processor()
    {
        // Arrange
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadProcessor(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Kinesis_Handler_Using_Attribute_Bad_Handler_Provider()
    {
        // Arrange
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadHandlerProvider(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Kinesis_Handler_Using_Attribute_Bad_Processor_Provider()
    {
        // Arrange
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
        };

        // Act
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeBadProcessorProvider(request));

        return Task.CompletedTask;
    }
    
    [Fact]
    public Task Kinesis_Handler_Using_Attribute_No_Handler()
    {
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
        };

        var function = new HandlerFunction();

        // Assert
        Assert.Throws<InvalidOperationException>(() => function.HandlerUsingAttributeWithoutHandler(request));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Kinesis_Handler_Using_Attribute_No_Event_Parameter()
    {
        var function = new HandlerFunction();

        // Assert
        Assert.Throws<ArgumentException>(() => function.HandlerUsingAttributeWithoutEvent(string.Empty));

        return Task.CompletedTask;
    }
}