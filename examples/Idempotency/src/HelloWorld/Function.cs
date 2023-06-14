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
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HelloWorld;

public class Function
{
    private static HttpClient? _httpClient;
    private static AmazonDynamoDBClient? _dynamoDbClient;

    /// <summary>
    /// Function constructor
    /// </summary>
    public Function()
    {
        _httpClient = new HttpClient();
        _dynamoDbClient = new AmazonDynamoDBClient();

        Init(_dynamoDbClient, _httpClient);
    }

    /// <summary>
    /// Test constructor
    /// </summary>
    public Function(AmazonDynamoDBClient amazonDynamoDb, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _dynamoDbClient = amazonDynamoDb;
        Init(amazonDynamoDb, httpClient);
    }
    
    private void Init(AmazonDynamoDBClient amazonDynamoDb, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(amazonDynamoDb);
        ArgumentNullException.ThrowIfNull(httpClient);
        var tableName = Environment.GetEnvironmentVariable("TABLE_NAME");
        ArgumentNullException.ThrowIfNull(tableName);
        Idempotency.Config()
            .WithConfig(IdempotencyConfig.Builder()
                .WithEventKeyJmesPath(
                    "powertools_json(Body).address") // will retrieve the address field in the body which is a string transformed to json with `powertools_json`
                .WithExpiration(TimeSpan.FromSeconds(10))
                .Build())
            .WithPersistenceStore(
                DynamoDBPersistenceStore.Builder()
                    .WithTableName(tableName)
                    .WithDynamoDBClient(amazonDynamoDb)
                    .Build())
            .Configure();
    }

    /// <summary>
    /// Lambda Handler
    /// Try with:
    /// <pre>
    /// curl -X POST https://[REST-API-ID].execute-api.[REGION].amazonaws.com/Prod/hello/ -H "Content-Type: application/json" -d '{"address": "https://checkip.amazonaws.com"}'
    /// </pre>
    /// </summary>
    /// <param name="apigwProxyEvent">API Gateway Proxy event</param>
    /// <param name="context">AWS Lambda context</param>
    /// <returns>API Gateway Proxy response</returns>
    [Idempotent]
    [Logging(LogEvent = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        var serializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var request = JsonSerializer.Deserialize<LookupRequest>(apigwProxyEvent.Body, serializationOptions);
        if (request is null)
        {
            return new APIGatewayProxyResponse
            {
                Body = "Invalid request",
                StatusCode = 403,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        var location = await GetCallingIp(request.Address);

        var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
        var response = new
        {
            RequestId = requestContextRequestId,
            Greeting = "Hello Powertools for AWS Lambda (.NET)",
            IpAddress = location  
        };

        try
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(response),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception e)
        {
            return new APIGatewayProxyResponse
            {
                Body = e.Message,
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }

    /// <summary>
    /// Calls location api to return IP address
    /// </summary>
    /// <param name="address">Uri of the service providing the calling IP</param>
    /// <returns>IP address string</returns>
    private static async Task<string?> GetCallingIp(string address)
    {
        if (_httpClient == null) return "0.0.0.0";
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

        var response = await _httpClient.GetStringAsync(address).ConfigureAwait(false);
        var ip = response.Replace("\n", "");

        return ip;
    }
}

/// <summary>
/// Record to represent the data structure of Lookup request
/// </summary>
[Serializable]
public class LookupRequest
{
    public string Address { get; private set; }

    public LookupRequest(string address)
    {
        Address = address;
    }
}