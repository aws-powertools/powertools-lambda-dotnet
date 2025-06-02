using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Xunit;
using Xunit.Abstractions;

namespace Function.Tests;

public class DynamoDbTests : TestBase
{
    private readonly string _tableName = "BatchProcessingTable";
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly AmazonLambdaClient _lambdaClient;
    
    public DynamoDbTests(ITestOutputHelper output) : base(output)
    {
        _dynamoDbClient = new AmazonDynamoDBClient();
        _lambdaClient = new AmazonLambdaClient();
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_batchprocessing_DynamoDB")]
    [InlineData("E2ETestLambda_ARM_NET8_batchprocessing_DynamoDB")]
    [InlineData("E2ETestLambda_X64_NET6_batchprocessing_DynamoDB")]
    [InlineData("E2ETestLambda_ARM_NET6_batchprocessing_DynamoDB")]
    public async Task TestSuccessfulProcessing(string functionName)
    {
        await TestSuccessfulBatchProcessing(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_batchprocessing_DynamoDB")]
    [InlineData("E2ETestLambda_ARM_NET8_batchprocessing_DynamoDB")]
    [InlineData("E2ETestLambda_X64_NET6_batchprocessing_DynamoDB")]
    [InlineData("E2ETestLambda_ARM_NET6_batchprocessing_DynamoDB")]
    public async Task TestFailedItems(string functionName)
    {
        await TestFailedItemsProcessing(functionName);
    }
    
    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_batchprocessing_DynamoDB")]
    [InlineData("E2ETestLambda_X64_AOT_NET8_batchprocessing_DynamoDB")]
    public async Task TestAotFunctionProcessing(string functionName)
    {
        await TestAotProcessing(functionName);
    }

    private async Task TestSuccessfulBatchProcessing(string functionName)
    {
        // Arrange - Create 3 items that should process successfully
        for (int i = 1; i <= 3; i++)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = $"success-{Guid.NewGuid()}" },
                ["name"] = new AttributeValue { S = $"Test Item {i}" },
                ["data"] = new AttributeValue { S = $"Some data {i}" },
                ["timestamp"] = new AttributeValue { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            };
            
            await _dynamoDbClient.PutItemAsync(_tableName, item);
        }
        
        // Wait for stream processing
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // Assert - Check CloudWatch logs to see successful processing
        var success = await WaitForSuccessInLogs(
            functionName,
            "Processing record with id: success-", 
            TimeSpan.FromMinutes(2));
        
        Assert.True(success, $"Failed to find successful processing in logs for {functionName}");
    }
    
    private async Task TestFailedItemsProcessing(string functionName)
    {
        // Arrange - Create items that should fail
        var failItem = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = "fail-item" },
            ["name"] = new AttributeValue { S = "Item that should fail" },
            ["data"] = new AttributeValue { S = "This should trigger failure logic" },
            ["timestamp"] = new AttributeValue { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
        };
        
        await _dynamoDbClient.PutItemAsync(_tableName, failItem);
        
        // Create successful item right after to verify partial batch processing
        var successItem = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = $"success-after-fail-{Guid.NewGuid()}" },
            ["name"] = new AttributeValue { S = "Success after failure" },
            ["data"] = new AttributeValue { S = "This should be processed" },
            ["timestamp"] = new AttributeValue { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
        };
        
        await _dynamoDbClient.PutItemAsync(_tableName, successItem);
        
        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // Assert - Verify both error handling and successful processing
        var failureLogged = await WaitForSuccessInLogs(
            functionName,
            "Failed to process record with id: fail-item", 
            TimeSpan.FromMinutes(2));
        
        Assert.True(failureLogged, $"Failed to find error handling in logs for {functionName}");
        
        // Also verify the success item was processed
        var successAfterFailure = await WaitForSuccessInLogs(
            functionName,
            "Processing record with id: success-after-fail", 
            TimeSpan.FromMinutes(2));
        
        Assert.True(successAfterFailure, $"Failed to process items after failure for {functionName}");
    }
    
    private async Task TestAotProcessing(string functionName)
    {
        // Arrange - Create item for AOT function
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = $"aot-test-{Guid.NewGuid()}" },
            ["name"] = new AttributeValue { S = "AOT Test Item" },
            ["data"] = new AttributeValue { S = "Testing AOT compiled function" },
            ["timestamp"] = new AttributeValue { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
        };
        
        await _dynamoDbClient.PutItemAsync(_tableName, item);
        
        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // Assert - Check logs for AOT function
        var aotSuccess = await WaitForSuccessInLogs(
            functionName,
            "Processing record with id: aot-test-", 
            TimeSpan.FromMinutes(2));
        
        Assert.True(aotSuccess, $"Failed to find AOT function processing in logs for {functionName}");
    }
}