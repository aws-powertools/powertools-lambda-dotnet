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
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Idempotency.Tests.Handlers;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests;

public class IdempotencyTest : IntegrationTestBase
{
    [Fact]
    public async Task EndToEndTest() 
    {
        IdempotencyFunction function = new IdempotencyFunction(client);
        
        //var persistenceStore = new InMemoryPersistenceStore();
        TestLambdaContext context = new TestLambdaContext();
        var request = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(File.ReadAllText("./resources/apigw_event2.json"));
        

        APIGatewayProxyResponse response = await function.Handle(request, context);
        function.HandlerExecuted.Should().BeTrue();

        function.HandlerExecuted = false;

        var response2 = await function.Handle(request, context);
        function.HandlerExecuted.Should().BeFalse();

        JsonConvert.SerializeObject(response).Should().Be(JsonConvert.SerializeObject(response));
        response2.Body.Should().Contain("hello world");

        var scanResponse = await client.ScanAsync(new ScanRequest
        {
            TableName = TABLE_NAME
        });
        scanResponse.Count.Should().Be(1);
    }
}