using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Xunit.Abstractions;

namespace HelloWorld.Tests
{
    public class FunctionTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private static readonly HttpClient Client = new HttpClient();

        public FunctionTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private static async Task<string> GetCallingIP()
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var stringTask = Client.GetStringAsync("http://checkip.amazonaws.com/")
                .ConfigureAwait(continueOnCapturedContext: false);

            var msg = await stringTask;
            return msg.Replace("\n", "");
        }

        [Fact]
        public async Task TestHelloWorldFunctionHandler()
        {
            var requestId = Guid.NewGuid().ToString("D");
            var request = new APIGatewayProxyRequest()
                { RequestContext = new APIGatewayProxyRequest.ProxyRequestContext() { RequestId = requestId } };
            var context = new TestLambdaContext()
            {
                FunctionName = "PowertoolsLoggingSample-HelloWorldFunction-Gg8rhPwO7Wa1",
                FunctionVersion = "1",
                MemoryLimitInMB = 215,
                AwsRequestId = Guid.NewGuid().ToString("D")
            };
            string location = GetCallingIP().Result;
            Dictionary<string, string> body = new Dictionary<string, string>
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

            var function = new Function();
            var response = await function.FunctionHandler(request, context);

            _testOutputHelper.WriteLine("Lambda Response: \n" + response.Body);
            _testOutputHelper.WriteLine("Expected Response: \n" + expectedResponse.Body);

            Assert.Equal(expectedResponse.Body, response.Body);
            Assert.Equal(expectedResponse.Headers, response.Headers);
            Assert.Equal(expectedResponse.StatusCode, response.StatusCode);
        }
    }
}