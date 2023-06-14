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
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HelloWorld.Cfn;

public class Function
{
    public async Task FunctionHandler(object cfnRequest, ILambdaContext context)
    {
        var request = JsonSerializer.Deserialize<CfnRequest>(cfnRequest.ToString()!);
        Console.Write(JsonSerializer.Serialize(request));

        var response = new CfnResponse
        {
            StackId = request!.StackId,
            RequestId = request.RequestId,
            LogicalResourceId = request.LogicalResourceId,
            PhysicalResourceId = "parameters_custom_physical_resource_id",
            Status = "SUCCESS",
            Reason = string.Empty,
            NoEcho = false
        };

        try
        {
            switch (request.RequestType!.ToLower())
            {
                case "create":
                    LambdaLogger.Log("Received Create request");
                    await InsertSingleParameterForDynamoDBProvider().ConfigureAwait(false);
                    await InsertMultipleParametersForDynamoDBProvider().ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Error:{ex.Message}");
            response.Status = "FAILED";
            response.Reason = ex.Message;
        }

        LambdaLogger.Log($"Sending response to {request.ResponseURL} ");
        SendResponse(request.ResponseURL!, response);
        LambdaLogger.Log("Finished");
    }

    private static void SendResponse(string url, CfnResponse cfnResponse)
    {
        var json = JsonSerializer.Serialize(cfnResponse);
        var byteArray = Encoding.UTF8.GetBytes(json);

        LambdaLogger.Log($"trying to upload json {json}");

        var httpRequest = WebRequest.Create(url) as HttpWebRequest;
        httpRequest.Method = "PUT";
        //If ContentType is set to anything but "" your upload will fail
        httpRequest.ContentType = "";
        httpRequest.ContentLength = byteArray.Length;

        using (var datastream = httpRequest.GetRequestStream())
            datastream.Write(byteArray, 0, byteArray.Length);
        var result = httpRequest.GetResponse() as HttpWebResponse;

        LambdaLogger.Log($"Result of upload is {result.StatusCode}");
    }

    private async Task InsertSingleParameterForDynamoDBProvider()
    {
        LambdaLogger.Log($"Inserting single parameter for DynamoDBProvider...");

        var client = new AmazonDynamoDBClient();

        // Get DynamoDB partition key  
        var tableName = Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBSingleParameterTableName) ??
                        "";

        // Get DynamoDB partition key  
        var hashKey = Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBSingleParameterId) ?? "";

        var request = new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>()
            {
                { "id", new AttributeValue { S = hashKey } },
                { "value", new AttributeValue { S = "my-value" } }
            }
        };

        await client.PutItemAsync(request).ConfigureAwait(false);
    }

    private async Task InsertMultipleParametersForDynamoDBProvider()
    {
        LambdaLogger.Log($"Inserting multiple parameters for DynamoDBProvider...");

        var client = new AmazonDynamoDBClient();

        // Get DynamoDB partition key  
        var tableName =
            Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBMultipleParametersTableName) ?? "";

        // Get DynamoDB partition key  
        var hashKey =
            Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBMultipleParametersParameterId) ?? "";

        for (var i = 1; i <= 3; i++)
        {
            var request = new PutItemRequest
            {
                TableName = tableName,
                Item = new Dictionary<string, AttributeValue>()
                {
                    { "id", new AttributeValue { S = hashKey } },
                    { "sk", new AttributeValue { S = $"param-{i}" } },
                    { "value", new AttributeValue { S = $"my-value-{i}" } }
                }
            };

            await client.PutItemAsync(request).ConfigureAwait(false);
        }
    }
}