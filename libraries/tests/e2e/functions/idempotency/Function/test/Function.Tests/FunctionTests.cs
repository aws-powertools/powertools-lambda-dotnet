using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.Model;
using Xunit.Abstractions;

namespace Function.Tests;

[Trait("Category", "E2E")]
public class FunctionTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AmazonLambdaClient _lambdaClient;
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private string _tableName = null!;

    public FunctionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _lambdaClient = new AmazonLambdaClient();
        _dynamoDbClient = new AmazonDynamoDBClient();
    }

    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_idempotency_HandlerTest", "IdempotencyTable-AOT-arm64")]
    [InlineData("E2ETestLambda_X64_AOT_NET8_idempotency_HandlerTest", "IdempotencyTable-AOT-x86_64")]
    public async Task IdempotencyHandlerAotTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await IdempotencyHandler(functionName);
    }
    
    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_idempotency_MethodAttributeTest", "IdempotencyTable-AOT-arm64")]
    [InlineData("E2ETestLambda_X64_AOT_NET8_idempotency_MethodAttributeTest", "IdempotencyTable-AOT-x86_64")]
    public async Task IdempotencyAttributeAotTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await IdempotencyAttribute(functionName);
    }
    
    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_idempotency_PayloadSubsetTest", "IdempotencyTable-AOT-arm64")]
    [InlineData("E2ETestLambda_X64_AOT_NET8_idempotency_PayloadSubsetTest", "IdempotencyTable-AOT-x86_64")]
    public async Task IdempotencyPayloadSubsetAotTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await IdempotencyPayloadSubset(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET8_idempotency")]
    [InlineData("E2ETestLambda_X64_NET6_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET6_idempotency")]
    public async Task IdempotencyPayloadSubsetTest(string functionName)
    {
        _tableName = "IdempotencyTable";
        await UpdateFunctionHandler(functionName, "Function::IdempotencyPayloadSubsetTest.Function::FunctionHandler");
        await IdempotencyPayloadSubset(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET8_idempotency")]
    [InlineData("E2ETestLambda_X64_NET6_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET6_idempotency")]
    public async Task IdempotencyAttributeTest(string functionName)
    {
        _tableName = "IdempotencyTable";
        await UpdateFunctionHandler(functionName, "Function::IdempotencyAttributeTest.Function::FunctionHandler");
        await IdempotencyAttribute(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET8_idempotency")]
    [InlineData("E2ETestLambda_X64_NET6_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET6_idempotency")]
    public async Task IdempotencyHandlerTest(string functionName)
    {
        _tableName = "IdempotencyTable";
        await UpdateFunctionHandler(functionName, "Function::Function.Function::FunctionHandler");
        await IdempotencyHandler(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET8_idempotency")]
    [InlineData("E2ETestLambda_X64_NET6_idempotency")]
    [InlineData("E2ETestLambda_ARM_NET6_idempotency")]
    public async Task IdempotencyHandlerCustomKey(string functionName)
    {
        _tableName = "IdempotencyTable";
        await UpdateFunctionHandler(functionName, "Function::CustomKeyPrefixTest.Function::FunctionHandler");
        await IdempotencyHandler(functionName, "MyCustomKeyPrefix");
    }

    private async Task IdempotencyPayloadSubset(string functionName)
    {
        // First unique request
        var firstProductId = Guid.NewGuid().ToString();
        var (firstResponse1, firstGuid1) = await ExecutePayloadSubsetRequest(functionName, "xyz", firstProductId);
        var (firstResponse2, firstGuid2) = await ExecutePayloadSubsetRequest(functionName, "xyz", firstProductId);
    
        // Assert first request pair
        Assert.Equal(200, firstResponse1.StatusCode);
        Assert.Equal(200, firstResponse2.StatusCode);
        Assert.Equal(firstGuid1, firstGuid2); // Idempotency check
        await AssertDynamoDbData(
            $"{functionName}.FunctionHandler#{Helpers.HashRequest($"[\"xyz\",\"{firstProductId}\"]")}", 
            firstGuid1);

        // Second unique request
        var secondProductId = Guid.NewGuid().ToString();
        var (secondResponse1, secondGuid1) = await ExecutePayloadSubsetRequest(functionName, "xyz", secondProductId);
        var (secondResponse2, secondGuid2) = await ExecutePayloadSubsetRequest(functionName, "xyz", secondProductId);

        // Assert second request pair
        Assert.Equal(200, secondResponse1.StatusCode);
        Assert.Equal(200, secondResponse2.StatusCode);
        Assert.Equal(secondGuid1, secondGuid2); // Idempotency check
        Assert.NotEqual(firstGuid1, secondGuid1); // Different requests should have different GUIDs
        await AssertDynamoDbData(
            $"{functionName}.FunctionHandler#{Helpers.HashRequest($"[\"xyz\",\"{secondProductId}\"]")}", 
            secondGuid1);
    }

    private async Task IdempotencyAttribute(string functionName)
    {
        // First unique request
        var requestId1 = Guid.NewGuid().ToString();
        var (firstResponse1, firstGuid1) = await ExecuteAttributeRequest(functionName, requestId1);
        var (firstResponse2, firstGuid2) = await ExecuteAttributeRequest(functionName, requestId1);

        // Assert first request pair
        Assert.Equal(200, firstResponse1.StatusCode);
        Assert.Equal(200, firstResponse2.StatusCode);
        Assert.Equal(firstGuid1, firstGuid2); // Idempotency check
        await AssertDynamoDbData(
            $"{functionName}.MyInternalMethod#{Helpers.HashRequest(requestId1)}", 
            firstGuid1, 
            true);

        // Second unique request
        var requestId2 = Guid.NewGuid().ToString();
        var (secondResponse1, secondGuid1) = await ExecuteAttributeRequest(functionName, requestId2);
        var (secondResponse2, secondGuid2) = await ExecuteAttributeRequest(functionName, requestId2);

        // Assert second request pair
        Assert.Equal(200, secondResponse1.StatusCode);
        Assert.Equal(200, secondResponse2.StatusCode);
        Assert.Equal(secondGuid1, secondGuid2); // Idempotency check
        Assert.NotEqual(firstGuid1, secondGuid1); // Different requests should have different GUIDs
        await AssertDynamoDbData(
            $"{functionName}.MyInternalMethod#{Helpers.HashRequest(requestId2)}", 
            secondGuid1, 
            true);
    }

    private async Task IdempotencyHandler(string functionName, string? keyPrefix = null)
    {
        var payload = await File.ReadAllTextAsync("../../../../../../../payload.json");
        
        // Execute three identical requests
        var (response1, guid1) = await ExecuteHandlerRequest(functionName, payload);
        var (response2, guid2) = await ExecuteHandlerRequest(functionName, payload);
        var (response3, guid3) = await ExecuteHandlerRequest(functionName, payload);

        // Assert all responses
        Assert.Equal(200, response1.StatusCode);
        Assert.Equal(200, response2.StatusCode);
        Assert.Equal(200, response3.StatusCode);

        // Assert idempotency
        Assert.Equal(guid1, guid2);
        Assert.Equal(guid2, guid3);

        var key = keyPrefix ?? $"{functionName}.FunctionHandler";
        
        // Assert DynamoDB
        await AssertDynamoDbData(
            $"{key}#35973cf447e6cc11008d603c791a232f", 
            guid1);
    }

    private async Task UpdateFunctionHandler(string functionName, string handler)
    {
        var updateRequest = new UpdateFunctionConfigurationRequest
        {
            FunctionName = functionName,
            Handler = handler
        };

        var updateResponse = await _lambdaClient.UpdateFunctionConfigurationAsync(updateRequest);
        
        if (updateResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine($"Successfully updated the handler for function {functionName} to {handler}");
        }
        else
        {
            Assert.Fail(
                $"Failed to update the handler for function {functionName}. Status code: {updateResponse.HttpStatusCode}");
        }
        
        //wait a few seconds for the changes to take effect
        await Task.Delay(5000);
    }

    private async Task AssertDynamoDbData(string id, string requestId, bool isSavedDataString = false)
    {
        _testOutputHelper.WriteLine($"Querying DynamoDB with id: {id}");

        var queryRequest = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "id = :v_id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":v_id", new AttributeValue { S = id } }
            }
        };

        _testOutputHelper.WriteLine($"QueryRequest: {JsonSerializer.Serialize(queryRequest)}");

        var queryResponse = await _dynamoDbClient.QueryAsync(queryRequest);

        _testOutputHelper.WriteLine($"QueryResponse: {JsonSerializer.Serialize(queryResponse)}");

        if (queryResponse.Items.Count == 0)
        {
            Assert.Fail("No items found in DynamoDB for the given id.");
        }

        foreach (var item in queryResponse.Items)
        {
            var data = item["data"].S;
            var status = item["status"].S;

            Assert.Equal("COMPLETED", status);

            if (!isSavedDataString)
            {
                var parsedData = JsonSerializer.Deserialize<APIGatewayProxyResponse>(data);

                if (parsedData == null)
                {
                    Assert.Fail("Failed to parse data field.");
                }

                var parsedResponse = JsonSerializer.Deserialize<Response>(parsedData.Body);
                if (parsedResponse == null)
                {
                    Assert.Fail("Failed to parse response.");
                }

                Assert.Equal(requestId, parsedResponse.Guid);
            }
            else
            {
                Assert.Equal(requestId, data.Trim('"'));
            }
        }
    }
    
    // Helper methods for executing requests
    private async Task<(APIGatewayProxyResponse Response, string Guid)> ExecutePayloadSubsetRequest(
        string functionName, string userId, string productId)
    {
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = JsonSerializer.Serialize(new APIGatewayProxyRequest
            {
                Body = $"{{\"user_id\":\"{userId}\",\"product_id\":\"{productId}\"}}"
            }),
            LogType = LogType.Tail
        };

        return await ExecuteRequest(request);
    }

    private async Task<(APIGatewayProxyResponse Response, string Guid)> ExecuteAttributeRequest(
        string functionName, string requestId)
    {
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = JsonSerializer.Serialize(new APIGatewayProxyRequest
            {
                Body = "{\"user_id\":\"***\",\"product_id\":\"123456789\"}",
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    AccountId = "123456789012",
                    RequestId = requestId
                }
            }),
            LogType = LogType.Tail
        };

        return await ExecuteRequest(request);
    }

    private async Task<(APIGatewayProxyResponse Response, string Guid)> ExecuteHandlerRequest(
        string functionName, string payload)
    {
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = payload,
            LogType = LogType.Tail
        };

        return await ExecuteRequest(request);
    }

    private async Task<(APIGatewayProxyResponse Response, string Guid)> ExecuteRequest(InvokeRequest request)
    {
        var response = await _lambdaClient.InvokeAsync(request);
    
        if (string.IsNullOrEmpty(response.LogResult))
            Assert.Fail("No LogResult field returned in the response of Lambda invocation.");

        var responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
        var parsedResponse = JsonSerializer.Deserialize<APIGatewayProxyResponse>(responsePayload) 
                             ?? throw new Exception("Failed to parse payload.");

        string guid;
        try
        {
            // The GUID is inside the Response object
            var parsedBody = JsonSerializer.Deserialize<Response>(parsedResponse.Body);
            guid = parsedBody?.Guid ?? parsedResponse.Body;
        }
        catch (JsonException)
        {
            // For scenarios where the Body is already the GUID
            guid = parsedResponse.Body;
        }

        return (parsedResponse, guid);
    }
}

public record Response(string Greeting, string Guid);