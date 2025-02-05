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
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Handlers;

public class IdempotencyFunctionMethodDecorated
{
    public bool MethodCalled;

    public IdempotencyFunctionMethodDecorated(AmazonDynamoDBClient client)
    {
        Idempotency.Configure(builder =>
            builder
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .UseDynamoDb(storeBuilder =>
                    storeBuilder
                        .WithTableName("idempotency_table")
                        .WithDynamoDBClient(client)
                ));
    }

    
    public async Task<APIGatewayProxyResponse> Handle(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Idempotency.RegisterLambdaContext(context);
        var result= await InternalFunctionHandler(apigProxyEvent);

        return result;
    }
    
    private async Task<APIGatewayProxyResponse> InternalFunctionHandler(APIGatewayProxyRequest apigProxyEvent)
    {
        Dictionary<string, string> headers = new()
        {
            {"Content-Type", "application/json"},
            {"Access-Control-Allow-Origin", "*"},
            {"Access-Control-Allow-Methods", "GET, OPTIONS"},
            {"Access-Control-Allow-Headers", "*"}
        };

        try
        {
            var address = JsonDocument.Parse(apigProxyEvent.Body).RootElement.GetProperty("address").GetString();
            var pageContents = await GetPageContents(address);
            var output = $"{{ \"message\": \"hello world\", \"location\": \"{pageContents}\" }}";

            return new APIGatewayProxyResponse
            {
                Body = output,
                StatusCode = 200,
                Headers = headers
            };

        }
        catch (IOException)
        {
            return new APIGatewayProxyResponse
            {
                Body = "{}",
                StatusCode = 500,
                Headers = headers
            };
        }
    }
    
    [Idempotent]
    private async Task<string> GetPageContents(string address)
    {
        MethodCalled = true;
        
        var client = new HttpClient();
        using var response = await client.GetAsync(address);
        using var content = response.Content;
        var pageContent = await content.ReadAsStringAsync();
        
        return pageContent;
    }
}