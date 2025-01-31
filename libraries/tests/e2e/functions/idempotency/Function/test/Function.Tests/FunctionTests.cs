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
    private string _handler;

    public FunctionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _lambdaClient = new AmazonLambdaClient();
        _dynamoDbClient = new AmazonDynamoDBClient();
    }

    // [Trait("Category", "AOT")]
    // [Theory]
    // [MemberData(nameof(TestDataAot.Inline), MemberType = typeof(TestDataAot))]
    // public async Task IdempotencyHandlerAotTest(string functionName, string tableName)
    // {
    //     _tableName = tableName;
    //     await TestIdempotencyHandler(functionName);
    // }

    [Theory]
    [MemberData(nameof(TestData.Inline), MemberType = typeof(TestData))]
    public async Task IdempotencyHandlerTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await TestIdempotencyHandler(functionName);
    }
    
    [Theory]
    [MemberData(nameof(TestData.Inline), MemberType = typeof(TestData))]
    public async Task IdempotencyAttributeTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await TestIdempotencyAttribute(functionName);
    }
    
    [Theory]
    [MemberData(nameof(TestData.Inline), MemberType = typeof(TestData))]
    public async Task IdempotencyPayloadSubsetTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await TestIdempotencyPayloadSubset(functionName);
    }

    private async Task TestIdempotencyPayloadSubset(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::IdempotencyPayloadSubsetTest.Function::FunctionHandler");

        var initialGuid = string.Empty;
        
        for (int i = 0; i < 2; i++)
        {
            var productId = Guid.NewGuid().ToString();
            var apiGatewayRequest = new APIGatewayProxyRequest
            {
                Body = $"{{\"user_id\":\"xyz\",\"product_id\":\"{productId}\"}}"
            };

            var payload = JsonSerializer.Serialize(apiGatewayRequest);

            var request = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload,
                LogType = LogType.Tail,
            };

            // run two times with the same request
            for (int j = 0; j < 2; j++)
            {
                var response = await _lambdaClient.InvokeAsync(request);

                if (string.IsNullOrEmpty(response.LogResult))
                {
                    Assert.Fail("No LogResult field returned in the response of Lambda invocation.");
                }

                var responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                var parsedPayload = JsonSerializer.Deserialize<APIGatewayProxyResponse>(responsePayload);

                if (parsedPayload == null)
                {
                    Assert.Fail("Failed to parse payload.");
                }

                Assert.Equal(200, parsedPayload.StatusCode);
                
                var parsedResponse = JsonSerializer.Deserialize<Response>(parsedPayload.Body);

                if (parsedResponse == null)
                {
                    Assert.Fail("Failed to parse response.");
                }

                if (j == 0)
                {
                    // first call should return a new guid
                    if (parsedResponse.Guid == initialGuid)
                    {
                        Assert.Fail("Idempotency failed to clear cache.");
                    }
                    
                    initialGuid = parsedResponse.Guid;
                }

                Assert.Equal(initialGuid, parsedResponse.Guid);

                // Query DynamoDB and assert results
                var hashRequest = Helpers.HashRequest($"[\"xyz\",\"{productId}\"]");
                
                var id = $"{functionName}.FunctionHandler#{hashRequest}";
                await AssertDynamoDbData(id, initialGuid);
            }
        }
    }

    private async Task TestIdempotencyAttribute(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::IdempotencyAttributeTest.Function::FunctionHandler");

        var initialGuid = string.Empty;

        for (int i = 0; i < 2; i++)
        {
            var requestId = Guid.NewGuid().ToString();
            var apiGatewayRequest = new APIGatewayProxyRequest
            {
                Body = "{\"user_id\":\"xyz\",\"product_id\":\"123456789\"}",
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    AccountId = "123456789012",
                    RequestId = requestId, // requestId is used to invalidate the cache
                }
            };

            var payload = JsonSerializer.Serialize(apiGatewayRequest);

            var request = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload,
                LogType = LogType.Tail,
            };

            // run two times with the same request
            for (int j = 0; j < 2; j++)
            {
                var response = await _lambdaClient.InvokeAsync(request);

                if (string.IsNullOrEmpty(response.LogResult))
                {
                    Assert.Fail("No LogResult field returned in the response of Lambda invocation.");
                }

                var responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                var parsedPayload = JsonSerializer.Deserialize<APIGatewayProxyResponse>(responsePayload);

                if (parsedPayload == null)
                {
                    Assert.Fail("Failed to parse payload.");
                }

                Assert.Equal(200, parsedPayload.StatusCode);

                if (j == 0)
                {
                    // first call should return a new guid
                    if (parsedPayload.Body == initialGuid)
                    {
                        Assert.Fail("Idempotency failed to clear cache.");
                    }

                    initialGuid = parsedPayload.Body;
                }

                Assert.Equal(initialGuid, parsedPayload.Body);

                // Query DynamoDB and assert results
                var hashRequestId = Helpers.HashRequest(requestId);
                var id = $"{functionName}.MyInternalMethod#{hashRequestId}";
                await AssertDynamoDbData(id, initialGuid, true);
            }
        }
    }

    internal async Task TestIdempotencyHandler(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::Function.Function::FunctionHandler");

        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = await File.ReadAllTextAsync("../../../../../../../payload.json"),
            LogType = LogType.Tail,
        };

        var initialGuid = string.Empty;

        // run three times to test idempotency
        for (int i = 0; i < 3; i++)
        {
            var response = await _lambdaClient.InvokeAsync(request);

            if (string.IsNullOrEmpty(response.LogResult))
            {
                Assert.Fail("No LogResult field returned in the response of Lambda invocation.");
            }

            var payload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
            var parsedPayload = JsonSerializer.Deserialize<APIGatewayProxyResponse>(payload);

            if (parsedPayload == null)
            {
                Assert.Fail("Failed to parse payload.");
            }

            Assert.Equal(200, parsedPayload.StatusCode);

            var parsedResponse = JsonSerializer.Deserialize<Response>(parsedPayload.Body);

            if (parsedResponse == null)
            {
                Assert.Fail("Failed to parse response.");
            }

            if (i == 0)
            {
                initialGuid = parsedResponse.Guid;
            }

            Assert.Equal(initialGuid, parsedResponse.Guid);
        }

        // Query DynamoDB and assert results
        var id = $"{functionName}.FunctionHandler#35973cf447e6cc11008d603c791a232f";
        await AssertDynamoDbData(id, initialGuid);
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
}

public record Response(string Greeting, string Guid);