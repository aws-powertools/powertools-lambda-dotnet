using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.Model;
using TestUtils;
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
    [InlineData("E2ETestLambda_X64_AOT_NET8_idempotency", "IdempotencyTable-AOT-x86_64")]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_idempotency", "IdempotencyTable-AOT-arm64")]
    public async Task IdempotencyHandlerAotTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await TestIdempotencyHandler(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET6_idempotency", "IdempotencyTable")]
    [InlineData("E2ETestLambda_ARM_NET6_idempotency", "IdempotencyTable")]
    [InlineData("E2ETestLambda_X64_NET8_idempotency", "IdempotencyTable")]
    [InlineData("E2ETestLambda_ARM_NET8_idempotency", "IdempotencyTable")]
    public async Task IdempotencyHandlerTest(string functionName, string tableName)
    {
        _tableName = tableName;
        await TestIdempotencyHandler(functionName);
    }

    internal async Task TestIdempotencyHandler(string functionName)
    {
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = await File.ReadAllTextAsync("../../../../../../../payload.json"),
            LogType = LogType.Tail,
        };

        var initialGuid = string.Empty;
        var initialRequestId = string.Empty;
        
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
            
            if(i == 0)
            {
                initialGuid = parsedResponse.MethodGuid;
                initialRequestId = parsedResponse.RequestId;
            }

            Assert.Equal(initialGuid, parsedResponse.MethodGuid);
            Assert.Equal(initialRequestId, parsedResponse.RequestId);
        }
        
        // Query DynamoDB and assert results
        await AssertDynamoDbData(functionName, initialGuid, initialRequestId);
    }
    
    private async Task AssertDynamoDbData(string functionName, string initialGuid, string initialRequestId)
    {
        var id = $"{functionName}.FunctionHandler#35973cf447e6cc11008d603c791a232f";
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
            
            Assert.Equal(initialGuid, parsedResponse.MethodGuid);
            Assert.Equal(initialRequestId, parsedResponse.RequestId);
        }
    }
}

public record Response(string RequestId, string Greeting, string MethodGuid, string HandlerGuid);