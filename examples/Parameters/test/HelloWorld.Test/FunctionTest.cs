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
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Xunit;
using NSubstitute;
using System.Text.Json;

namespace HelloWorld.Tests
{
    public class FunctionTest
    {
        [Fact]
        public async Task TestHelloWorldFunctionHandler()
        {
            // Arrange
            var requestId = Guid.NewGuid().ToString("D");
            var accountId = Guid.NewGuid().ToString("D");

            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    RequestId = requestId,
                    AccountId = accountId
                }
            };

            var context = new TestLambdaContext()
            {
                FunctionName = Guid.NewGuid().ToString("D"),
                FunctionVersion = "1",
                MemoryLimitInMB = 215,
                AwsRequestId = requestId
            };

            var helper = Substitute.For<IParameterLookupHelper>();

            var singleSsmRecord = new ParameterLookupRecord
            {
                Provider = ParameterProviderType.SsmProvider,
                Method = ParameterLookupMethod.Get,
                Key = Guid.NewGuid().ToString("D"),
                Value = Guid.NewGuid().ToString("D")
            };
            helper.GetSingleParameterWithSsmProvider().Returns(singleSsmRecord);

            var multipleSsmRecord = new ParameterLookupRecord
            {
                Provider = ParameterProviderType.SsmProvider,
                Method = ParameterLookupMethod.GetMultiple,
                Key = Guid.NewGuid().ToString("D"),
                Value = new Dictionary<string, string>()
                {
                    { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") },
                    { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") }
                }
            };
            helper.GetMultipleParametersWithSsmProvider().Returns(multipleSsmRecord);

            var singleSecretRecord = new ParameterLookupRecord
            {
                Provider = ParameterProviderType.SecretsProvider,
                Method = ParameterLookupMethod.Get,
                Key = Guid.NewGuid().ToString("D"),
                Value = new SecretRecord
                {
                    Username = Guid.NewGuid().ToString("D"),
                    Password = Guid.NewGuid().ToString("D")
                }
            };
            helper.GetSingleSecretWithSecretsProvider().Returns(singleSecretRecord);

            var singleDynamoDbRecord = new ParameterLookupRecord
            {
                Provider = ParameterProviderType.DynamoDBProvider,
                Method = ParameterLookupMethod.Get,
                Key = Guid.NewGuid().ToString("D"),
                Value = Guid.NewGuid().ToString("D")
            };
            helper.GetSingleParameterWithDynamoDBProvider().Returns(singleDynamoDbRecord);

            var multipleDynamoDbRecord = new ParameterLookupRecord
            {
                Provider = ParameterProviderType.DynamoDBProvider,
                Method = ParameterLookupMethod.GetMultiple,
                Key = Guid.NewGuid().ToString("D"),
                Value = new Dictionary<string, string>()
                {
                    { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") },
                    { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") }
                }
            };
            helper.GetMultipleParametersWithDynamoDBProvider().Returns(multipleDynamoDbRecord);

            var body = new Dictionary<string, object>
            {
                { "RequestId", requestId },
                { "Greeting", "Hello Powertools for AWS Lambda (.NET)" },
                {
                    "Parameters", new List<ParameterLookupRecord>
                    {
                        singleSsmRecord,
                        multipleSsmRecord,
                        singleSecretRecord,
                        singleDynamoDbRecord,
                        multipleDynamoDbRecord
                    }
                }
            };

            var expectedResponse = new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            // Act
            var function = new Function(helper);
            var response = await function.FunctionHandler(request, context);

            // Assert
            await helper.Received(1).GetSingleParameterWithSsmProvider();
            await helper.Received(1).GetMultipleParametersWithSsmProvider();
            await helper.Received(1).GetSingleSecretWithSecretsProvider();
            await helper.Received(1).GetSingleParameterWithDynamoDBProvider();
            await helper.Received(1).GetMultipleParametersWithDynamoDBProvider();
            Assert.Equal(expectedResponse.Body, response.Body);
            Assert.Equal(expectedResponse.Headers, response.Headers);
            Assert.Equal(expectedResponse.StatusCode, response.StatusCode);
        }
    }
}