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

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json.Linq;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Handlers;

public class IdempotencyFunction
{
    public bool HandlerExecuted = false;

    public IdempotencyFunction(AmazonDynamoDBClient client)
    {
        Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).address"))
                .UseDynamoDb(storeBuilder =>
                    storeBuilder
                        .WithTableName("idempotency_table")
                        .WithDynamoDBClient(client)
                ));

        
        
    }

    [Idempotent]
    public async Task<APIGatewayProxyResponse> Handle(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        HandlerExecuted = true;
        var result= await InternalFunctionHandler(apigProxyEvent,context);

        return result;
    }
    private async Task<APIGatewayProxyResponse> InternalFunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
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
            string address = JToken.Parse(apigProxyEvent.Body)["address"].Value<string>();
            string pageContents = await GetPageContents(address);
            string output = $"{{ \"message\": \"hello world\", \"location\": \"{pageContents}\" }}";

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
    
    // we could actually also put the @Idempotent annotation here
    private async Task<string> GetPageContents(string address)
    {
        HttpClient client = new HttpClient();
        using HttpResponseMessage response = await client.GetAsync(address);
        using HttpContent content = response.Content;
        string pageContent = await content.ReadAsStringAsync();
        
        return pageContent;
    }
}