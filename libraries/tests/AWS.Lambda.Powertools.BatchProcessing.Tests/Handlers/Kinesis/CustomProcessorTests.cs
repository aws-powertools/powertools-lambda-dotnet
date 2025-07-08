using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Handler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TestHelper = AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers.Helpers;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis;

// These tests must run last. Injecting the custom processor will change the other tests expectations 
[Collection("X Sequential")]
public class HandlerCustomProcessorTests
{
    [Fact]
    public Task Kinesis_Handler_Using_Attribute_Custom_Processor()
    {
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
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
    public Task Kinesis_Handler_Using_Attribute_Custom_Processor_Provider()
    {
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
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
    public async Task Kinesis_Handler_Using_Utility_IoC_Custom_Providers()
    {
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
        };
        
        var function = new HandlerFunction();
    
        var response = await function.HandlerUsingUtilityFromIoc(request);
    
        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
    }
    
    [Fact]
    public async Task Kinesis_Handler_Using_Utility_IoC_Constructor()
    {
        var request = new KinesisEvent
        {
            Records = TestHelper.KinesisMessages
        };

        var batchProcessor = Services.Provider.GetRequiredService<IKinesisEventBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<IKinesisEventRecordHandler>();
        var function = new HandlerFunction(batchProcessor, recordHandler);

        var response = await function.HandlerUsingUtilityFromIocConstructor(request);

        Assert.Equal(4, response.BatchItemFailures.Count);
        Assert.Equal("2", response.BatchItemFailures[0].ItemIdentifier);
        Assert.Equal("3", response.BatchItemFailures[1].ItemIdentifier);
        Assert.Equal("4", response.BatchItemFailures[2].ItemIdentifier);
    }
}