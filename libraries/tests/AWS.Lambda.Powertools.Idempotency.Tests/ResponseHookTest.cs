using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Persistence;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests;

public class ResponseHookTest : IClassFixture<DynamoDbFixture>
{
    private readonly AmazonDynamoDBClient _client;
    private readonly string _tableName;

    public ResponseHookTest(DynamoDbFixture fixture)
    {
        _client = fixture.Client;
        _tableName = fixture.TableName;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResponseHook_ShouldNotExecuteOnFirstCall()
    {
        // Arrange
        var hookExecuted = false;
        
        Idempotency.Configure(builder => builder
            .WithOptions(options => options
                .WithEventKeyJmesPath("powertools_json(body).address")
                .WithResponseHook((responseData, dataRecord) => {
                    hookExecuted = true;
                    if (responseData is APIGatewayProxyResponse proxyResponse)
                    {
                        var headers = new Dictionary<string, string>(proxyResponse.Headers ?? new Dictionary<string, string>());
                        headers["x-idempotency-response"] = "true";
                        headers["x-idempotency-expiration"] = dataRecord.ExpiryTimestamp.ToString();
                        proxyResponse.Headers = headers;
                        return proxyResponse;
                    }
                    return responseData;
                }))
            .WithPersistenceStore(new DynamoDBPersistenceStoreBuilder()
                .WithTableName(_tableName)
                .WithDynamoDBClient(_client)
                .Build()));

        var function = new ResponseHookTestFunction();
        var request = IdempotencySerializer.Deserialize<APIGatewayProxyRequest>(
            await File.ReadAllTextAsync("./resources/apigw_event2.json"));

        // Act - First call
        var response = await function.Handle(request);

        // Assert - Hook should not execute on first call
        hookExecuted.Should().BeFalse();
        response.Headers.Should().NotContainKey("x-idempotency-response");
        function.HandlerExecuted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResponseHook_ShouldExecuteOnIdempotentCall()
    {
        // Arrange
        var hookExecuted = false;
        
        Idempotency.Configure(builder => builder
            .WithOptions(options => options
                .WithEventKeyJmesPath("powertools_json(body).address")
                .WithResponseHook((responseData, dataRecord) => {
                    hookExecuted = true;
                    if (responseData is APIGatewayProxyResponse proxyResponse)
                    {
                        var headers = new Dictionary<string, string>(proxyResponse.Headers ?? new Dictionary<string, string>());
                        headers["x-idempotency-response"] = "true";
                        headers["x-idempotency-expiration"] = dataRecord.ExpiryTimestamp.ToString();
                        proxyResponse.Headers = headers;
                        return proxyResponse;
                    }
                    return responseData;
                }))
            .WithPersistenceStore(new DynamoDBPersistenceStoreBuilder()
                .WithTableName(_tableName)
                .WithDynamoDBClient(_client)
                .Build()));

        var function = new ResponseHookTestFunction();
        var request = IdempotencySerializer.Deserialize<APIGatewayProxyRequest>(
            await File.ReadAllTextAsync("./resources/apigw_event2.json"));

        // Act - First call to populate cache
        await function.Handle(request);
        function.HandlerExecuted = false;
        hookExecuted = false;

        // Act - Second call (idempotent)
        var response = await function.Handle(request);

        // Assert - Hook should execute on idempotent call
        hookExecuted.Should().BeTrue();
        response.Headers.Should().ContainKey("x-idempotency-response");
        response.Headers["x-idempotency-response"].Should().Be("true");
        response.Headers.Should().ContainKey("x-idempotency-expiration");
        function.HandlerExecuted.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResponseHook_ShouldHandleExceptionsGracefully()
    {
        // Arrange
        Idempotency.Configure(builder => builder
            .WithOptions(options => options
                .WithEventKeyJmesPath("powertools_json(body).address")
                .WithResponseHook((responseData, dataRecord) => {
                    throw new InvalidOperationException("Hook failed");
                }))
            .WithPersistenceStore(new DynamoDBPersistenceStoreBuilder()
                .WithTableName(_tableName)
                .WithDynamoDBClient(_client)
                .Build()));

        var function = new ResponseHookTestFunction();
        var request = IdempotencySerializer.Deserialize<APIGatewayProxyRequest>(
            await File.ReadAllTextAsync("./resources/apigw_event2.json"));

        // Act - First call to populate cache
        var firstResponse = await function.Handle(request);
        function.HandlerExecuted = false;

        // Act - Second call (idempotent) - should not throw despite hook exception
        var response = await function.Handle(request);

        // Assert - Should return original response despite hook exception
        response.Should().NotBeNull();
        response.Body.Should().Be(firstResponse.Body);
        function.HandlerExecuted.Should().BeFalse();
    }
}

public class ResponseHookTestFunction
{
    public bool HandlerExecuted { get; set; }

    [Idempotent]
    public async Task<APIGatewayProxyResponse> Handle(APIGatewayProxyRequest request)
    {
        HandlerExecuted = true;
        
        await Task.Delay(100); // Simulate some work
        
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = "Hello World",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };
    }
}