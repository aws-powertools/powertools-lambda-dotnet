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

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
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
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        //var persistenceStore = new InMemoryPersistenceStore();
        var context = new TestLambdaContext();
        var request = JsonSerializer.Deserialize<APIGatewayProxyRequest>(await File.ReadAllTextAsync("./resources/apigw_event2.json"),options);
        
        var response = await function.Handle(request, context);
        function.HandlerExecuted.Should().BeTrue();

        function.HandlerExecuted = false;

        var response2 = await function.Handle(request, context);
        function.HandlerExecuted.Should().BeFalse();

        JsonSerializer.Serialize(response).Should().Be(JsonSerializer.Serialize(response));
        response2.Body.Should().Contain("hello world");

        var scanResponse = await _client.ScanAsync(new ScanRequest
        {
            TableName = _tableName
        });
        scanResponse.Count.Should().Be(1);
    }
}