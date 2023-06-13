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
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace HelloWorld;

public class Function
{

    private readonly IParameterLookupHelper _lookupHelper;

    /// <summary>
    /// Function constructor
    /// </summary>
    public Function()
    {
        _lookupHelper = new ParameterLookupHelper();
    }
    
    /// <summary>
    /// Test constructor
    /// </summary>
    public Function(IParameterLookupHelper lookupHelper)
    {
        _lookupHelper = lookupHelper;
    }
    
    /// <summary>
    /// Event handler function
    /// </summary>
    /// <param name="apigwProxyEvent">The API Gateway event payload</param>
    /// <param name="context">The Lambda context</param>
    /// <returns>An API Gateway response</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        try
        {
            var lookupInfo = new Dictionary<string, object>()
            {
                { "RequestId", apigwProxyEvent.RequestContext.RequestId },
                {
                    "Parameters", new List<ParameterLookupRecord>
                    {
                        await _lookupHelper.GetSingleParameterWithSsmProvider(),
                        await _lookupHelper.GetMultipleParametersWithSsmProvider(),
                        await _lookupHelper.GetSingleSecretWithSecretsProvider(),
                        await _lookupHelper.GetSingleParameterWithDynamoDBProvider(),
                        await _lookupHelper.GetMultipleParametersWithDynamoDBProvider()
                    }
                }
            };

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(lookupInfo),
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
}