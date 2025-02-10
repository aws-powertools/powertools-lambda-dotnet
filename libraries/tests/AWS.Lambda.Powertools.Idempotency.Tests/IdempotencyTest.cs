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
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
using AWS.Lambda.Powertools.Idempotency.Tests.Handlers;
using AWS.Lambda.Powertools.Idempotency.Tests.Persistence;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests;

public class IdempotencyTest : IClassFixture<DynamoDbFixture>
{
    private readonly AmazonDynamoDBClient _client;
    private readonly string _tableName;

    public IdempotencyTest(DynamoDbFixture fixture)
    {
        _client = fixture.Client;
        _tableName = fixture.TableName;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EndToEndTest()
    {
        var function = new IdempotencyFunction(_client);

        var request =
            IdempotencySerializer.Deserialize<APIGatewayProxyRequest>(
                await File.ReadAllTextAsync("./resources/apigw_event2.json"));

        var response = await function.Handle(request);
        function.HandlerExecuted.Should().BeTrue();

        function.HandlerExecuted = false;

        var response2 = await function.Handle(request);
        function.HandlerExecuted.Should().BeFalse();

        IdempotencySerializer.Serialize(response, typeof(APIGatewayProxyResponse)).Should()
            .Be(IdempotencySerializer.Serialize(response2, typeof(APIGatewayProxyResponse)));
        
        response.Body.Should().Contain("hello world");
        response2.Body.Should().Contain("hello world");

        var scanResponse = await _client.ScanAsync(new ScanRequest
        {
            TableName = _tableName
        });
        scanResponse.Count.Should().Be(1);

        // delete row dynamo
        var key = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = "testFunction.GetPageContents#ff323c6f0c5ceb97eed49121babcec0f" }
        };
        await _client.DeleteItemAsync(new DeleteItemRequest { TableName = _tableName, Key = key });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EndToEndTestMethod()
    {
        var function = new IdempotencyFunctionMethodDecorated(_client);

        var context = new TestLambdaContext
        {
            RemainingTime = TimeSpan.FromSeconds(30)
        };

        var request =
            IdempotencySerializer.Deserialize<APIGatewayProxyRequest>(
                await File.ReadAllTextAsync("./resources/apigw_event2.json"));

        var response = await function.Handle(request, context);
        function.MethodCalled.Should().BeTrue();

        function.MethodCalled = false;

        var response2 = await function.Handle(request, context);
        function.MethodCalled.Should().BeFalse();

        // Assert
        IdempotencySerializer.Serialize(response, typeof(APIGatewayProxyResponse)).Should()
            .Be(IdempotencySerializer.Serialize(response2, typeof(APIGatewayProxyResponse)));
        response.Body.Should().Contain("hello world");
        response2.Body.Should().Contain("hello world");

        var scanResponse = await _client.ScanAsync(new ScanRequest
        {
            TableName = _tableName
        });
        scanResponse.Count.Should().Be(1);

        // delete row dynamo
        var key = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = "testFunction.GetPageContents#ff323c6f0c5ceb97eed49121babcec0f" }
        };
        await _client.DeleteItemAsync(new DeleteItemRequest { TableName = _tableName, Key = key });
    }
}