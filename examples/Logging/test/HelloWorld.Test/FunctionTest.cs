using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Xunit;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Moq;
using Moq.Protected;
using Xunit.Abstractions;

namespace HelloWorld.Tests
{
    public class FunctionTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public FunctionTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task TestHelloWorldFunctionHandler()
        {
            // Arrange
            var requestId = Guid.NewGuid().ToString("D");
            var accountId = Guid.NewGuid().ToString("D");
            var location = "192.158. 1.38";
            
            var dynamoDbContext = new Mock<IDynamoDBContext>();
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(location)
                })
                .Verifiable();

            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    RequestId = requestId,
                    AccountId = accountId
                }
            };
            
            var context = new TestLambdaContext
            {
                FunctionName = "PowertoolsLoggingSample-HelloWorldFunction-Gg8rhPwO7Wa1",
                FunctionVersion = "1",
                MemoryLimitInMB = 215,
                AwsRequestId = Guid.NewGuid().ToString("D")
            };
            
            var body = new Dictionary<string, string>
            {
                { "LookupId", requestId },
                { "Greeting", "Hello AWS Lambda Powertools for .NET" },
                { "IpAddress", location },
            };

            var expectedResponse = new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
            
            // Act
            var function = new Function(dynamoDbContext.Object, new HttpClient(handlerMock.Object));
            var response = await function.FunctionHandler(request, context);

            _testOutputHelper.WriteLine("Lambda Response: \n" + response.Body);
            _testOutputHelper.WriteLine("Expected Response: \n" + expectedResponse.Body);

            // Assert
            Assert.Equal(expectedResponse.Body, response.Body);
            Assert.Equal(expectedResponse.Headers, response.Headers);
            Assert.Equal(expectedResponse.StatusCode, response.StatusCode);
        }
    }
}