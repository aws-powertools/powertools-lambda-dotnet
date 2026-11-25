using System.Collections.Concurrent;
using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Runtime;
using AWS.Lambda.Powertools.Idempotency;
using Xunit;

namespace PackageSmoke.Tests;

public class IdempotencyFunction
{
    public int Executions { get; private set; }

    public IdempotencyFunction(AmazonDynamoDBClient dynamoDbClient)
    {
        Idempotency.Configure(builder => builder
            .WithOptions(options => options.WithEventKeyJmesPath("powertools_json(Body).address"))
            .UseDynamoDb(store => store
                .WithTableName("idempotency_table")
                .WithDynamoDBClient(dynamoDbClient)));
    }

    [Idempotent]
    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        Executions++;
        var body = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["address"] = JsonDocument.Parse(request.Body).RootElement.GetProperty("address").GetString(),
            ["executionId"] = Guid.NewGuid().ToString("D")
        });

        return Task.FromResult(new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = body,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        });
    }
}

public class IdempotencySmokeTests
{
    private static APIGatewayProxyRequest Request(string address) => new() { Body = $"{{\"address\":\"{address}\"}}" };

    private static TestLambdaContext Context() => new()
    {
        FunctionName = "PackageSmoke-Idempotency",
        AwsRequestId = Guid.NewGuid().ToString("D"),
        RemainingTime = TimeSpan.FromSeconds(30)
    };

    [Fact]
    public async Task Repeated_Request_Returns_Stored_Response_Without_Running_The_Handler_Again()
    {
        var dynamoDb = new InMemoryDynamoDbClient();
        var function = new IdempotencyFunction(dynamoDb);

        var first = await function.FunctionHandler(Request("Hello World"), Context());
        var second = await function.FunctionHandler(Request("Hello World"), Context());

        Assert.Equal(1, function.Executions);
        Assert.Equal(first.StatusCode, second.StatusCode);
        Assert.Equal(first.Body, second.Body);
        Assert.Equal(first.Headers, second.Headers);

        var record = Assert.Single(dynamoDb.Items.Values);
        Assert.Equal("COMPLETED", record["status"].S);

        await function.FunctionHandler(Request("Another address"), Context());

        Assert.Equal(2, function.Executions);
        Assert.Equal(2, dynamoDb.Items.Count);
    }
}

/// <summary>
/// Stand-in for DynamoDB so the real DynamoDBPersistenceStore can run without a table or Docker.
/// Implements the four calls the store makes, including the conditional put that detects duplicates.
/// </summary>
internal sealed class InMemoryDynamoDbClient()
    : AmazonDynamoDBClient(new BasicAWSCredentials("fake", "fake"), RegionEndpoint.USEast1)
{
    public ConcurrentDictionary<string, Dictionary<string, AttributeValue>> Items { get; } = new();

    private static string KeyOf(Dictionary<string, AttributeValue> key) =>
        string.Join("|", key.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value.S}"));

    public override Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken = default)
    {
        var keyAttr = request.ExpressionAttributeNames["#id"];
        var key = KeyOf(new Dictionary<string, AttributeValue> { [keyAttr] = request.Item[keyAttr] });

        if (Items.TryGetValue(key, out var existing))
        {
            var expiryAttr = request.ExpressionAttributeNames["#expiry"];
            var now = long.Parse(request.ExpressionAttributeValues[":now"].N);
            if (long.Parse(existing[expiryAttr].N) >= now)
            {
                throw new ConditionalCheckFailedException("The conditional request failed") { Item = existing };
            }
        }

        Items[key] = new Dictionary<string, AttributeValue>(request.Item);
        return Task.FromResult(new PutItemResponse());
    }

    public override Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.TryGetValue(KeyOf(request.Key), out var item)
            ? new GetItemResponse { Item = item }
            : new GetItemResponse());

    public override Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        // The store only issues "SET #a = :a, #b = :b, ..." updates.
        var item = Items.GetOrAdd(KeyOf(request.Key), _ => new Dictionary<string, AttributeValue>(request.Key));
        foreach (var (placeholder, attribute) in request.ExpressionAttributeNames)
        {
            item[attribute] = request.ExpressionAttributeValues[":" + placeholder.TrimStart('#')];
        }

        return Task.FromResult(new UpdateItemResponse());
    }

    public override Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default)
    {
        Items.TryRemove(KeyOf(request.Key), out _);
        return Task.FromResult(new DeleteItemResponse());
    }
}
