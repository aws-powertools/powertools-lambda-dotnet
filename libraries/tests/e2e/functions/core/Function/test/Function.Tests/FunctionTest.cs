using System.Text.Json;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.Model;
using TestUtils;
using Xunit.Abstractions;

namespace Function.Tests;

public class FunctionTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AmazonLambdaClient _lambdaClient;

    public FunctionTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _lambdaClient = new AmazonLambdaClient();
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET6")]
    [InlineData("E2ETestLambda_ARM_NET6")]
    [InlineData("E2ETestLambda_X64_NET8")]
    [InlineData("E2ETestLambda_ARM_NET8")]
    public async Task TestToUpperFunction(string functionName)
    {
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = await File.ReadAllTextAsync("../../../../../../payload.json"),
            LogType = LogType.Tail
        };

        // run twice for cold and warm start
        for (int i = 0; i < 2; i++)
        {
            var response = await _lambdaClient.InvokeAsync(request);

            if (string.IsNullOrEmpty(response.LogResult))
            {
                Assert.Fail("No LogResult field returned in the response of Lambda invocation.");
            }

            var payload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
            var parsedPayload = JsonSerializer.Deserialize<APIGatewayProxyResponse>(payload);

            if (parsedPayload == null)
            {
                Assert.Fail("Failed to parse payload.");
            }

            Assert.Equal(200, parsedPayload.StatusCode);
            Assert.Equal("HELLO WORLD", parsedPayload.Body);

            // Assert Output log from Lambda execution
            AssertOutputLog(functionName, response);
        }
    }

    private void AssertOutputLog(string functionName, InvokeResponse response)
    {
        // Extract and parse log
        var logResult = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
        _testOutputHelper.WriteLine(logResult);
        var output = OutputLogParser.ParseLogSegments(logResult, out var report);
        var isColdStart = report.initDuration != "N/A";
        
        // Assert Logging utility
        AssertEventLog(functionName, isColdStart, output[0]);
        AssertInformationLog(functionName, isColdStart, output[1]);
        AssertWarningLog(functionName, isColdStart, output[2]);
        AssertExceptionLog(functionName, isColdStart, output[3]);
    }

    private void AssertEventLog(string functionName, bool isColdStart, string output)
    {
        using JsonDocument doc = JsonDocument.Parse(output);
        JsonElement root = doc.RootElement;
        
        AssertDefaultLoggingProperties.ArePresent(functionName, isColdStart, output);
        
        if (!isColdStart)
        {
            Assert.True(root.TryGetProperty("LookupInfo", out JsonElement lookupInfoElement));
            Assert.True(lookupInfoElement.TryGetProperty("LookupId", out JsonElement lookupIdElement));
            Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", lookupIdElement.GetString());
        }

        Assert.True(root.TryGetProperty("Level", out JsonElement levelElement));
        Assert.Equal("Information", levelElement.GetString());

        Assert.True(root.TryGetProperty("Message", out JsonElement messageElement));
        Assert.True(messageElement.TryGetProperty("Resource", out JsonElement resourceElement));
        Assert.Equal("/{proxy+}", resourceElement.GetString());

        Assert.True(messageElement.TryGetProperty("Path", out JsonElement pathElement));
        Assert.Equal("/path/to/resource", pathElement.GetString());

        Assert.True(messageElement.TryGetProperty("HttpMethod", out JsonElement httpMethodElement));
        Assert.Equal("POST", httpMethodElement.GetString());

        Assert.True(messageElement.TryGetProperty("Headers", out JsonElement headersElement));
        Assert.True(headersElement.TryGetProperty("Accept-Encoding", out JsonElement acceptEncodingElement));
        Assert.Equal("gzip, deflate, sdch", acceptEncodingElement.GetString());

        Assert.True(headersElement.TryGetProperty("Accept-Language", out JsonElement acceptLanguageElement));
        Assert.Equal("en-US,en;q=0.8", acceptLanguageElement.GetString());

        Assert.True(headersElement.TryGetProperty("Cache-Control", out JsonElement cacheControlElement));
        Assert.Equal("max-age=0", cacheControlElement.GetString());

        Assert.True(headersElement.TryGetProperty("CloudFront-Forwarded-Proto",
            out JsonElement cloudFrontForwardedProtoElement));
        Assert.Equal("https", cloudFrontForwardedProtoElement.GetString());

        Assert.True(headersElement.TryGetProperty("CloudFront-Viewer-Country",
            out JsonElement cloudFrontViewerCountryElement));
        Assert.Equal("US", cloudFrontViewerCountryElement.GetString());

        Assert.True(headersElement.TryGetProperty("Upgrade-Insecure-Requests",
            out JsonElement upgradeInsecureRequestsElement));
        Assert.Equal("1", upgradeInsecureRequestsElement.GetString());

        Assert.True(headersElement.TryGetProperty("User-Agent", out JsonElement userAgentElement));
        Assert.Equal("Custom User Agent String", userAgentElement.GetString());

        Assert.True(headersElement.TryGetProperty("X-Forwarded-For", out JsonElement xForwardedForElement));
        Assert.Equal("127.0.0.1, 127.0.0.2", xForwardedForElement.GetString());

        Assert.True(headersElement.TryGetProperty("X-Forwarded-Port", out JsonElement xForwardedPortElement));
        Assert.Equal("443", xForwardedPortElement.GetString());

        Assert.True(headersElement.TryGetProperty("X-Forwarded-Proto", out JsonElement xForwardedProtoElement));
        Assert.Equal("https", xForwardedProtoElement.GetString());

        Assert.True(
            messageElement.TryGetProperty("QueryStringParameters", out JsonElement queryStringParametersElement));
        Assert.True(queryStringParametersElement.TryGetProperty("Foo", out JsonElement fooElement));
        Assert.Equal("bar", fooElement.GetString());

        Assert.True(messageElement.TryGetProperty("RequestContext", out JsonElement requestContextElement));
        Assert.True(requestContextElement.TryGetProperty("Path", out JsonElement requestContextPathElement));
        Assert.Equal("/prod/path/to/resource", requestContextPathElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("AccountId", out JsonElement accountIdElement));
        Assert.Equal("123456789012", accountIdElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("ResourceId", out JsonElement resourceIdElement));
        Assert.Equal("123456", resourceIdElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("Stage", out JsonElement stageElement));
        Assert.Equal("prod", stageElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("RequestId", out JsonElement requestIdElement));
        Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", requestIdElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("ResourcePath", out JsonElement resourcePathElement));
        Assert.Equal("/{proxy+}", resourcePathElement.GetString());

        Assert.True(
            requestContextElement.TryGetProperty("HttpMethod", out JsonElement requestContextHttpMethodElement));
        Assert.Equal("POST", requestContextHttpMethodElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("ApiId", out JsonElement apiIdElement));
        Assert.Equal("1234567890", apiIdElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("RequestTime", out JsonElement requestTimeElement));
        Assert.Equal("09/Apr/2015:12:34:56 +0000", requestTimeElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("RequestTimeEpoch", out JsonElement requestTimeEpochElement));
        Assert.Equal(1428582896000, requestTimeEpochElement.GetInt64());

        Assert.True(messageElement.TryGetProperty("Body", out JsonElement bodyElement));
        Assert.Equal("hello world", bodyElement.GetString());

        Assert.True(messageElement.TryGetProperty("IsBase64Encoded", out JsonElement isBase64EncodedElement));
        Assert.False(isBase64EncodedElement.GetBoolean());
    }

    private void AssertInformationLog(string functionName, bool isColdStart, string output)
    {
        using JsonDocument doc = JsonDocument.Parse(output);
        JsonElement root = doc.RootElement;

        AssertDefaultLoggingProperties.ArePresent(functionName, isColdStart, output);
        
        if (!isColdStart)
        {
            Assert.True(root.TryGetProperty("LookupInfo", out JsonElement lookupInfoElement));
            Assert.True(lookupInfoElement.TryGetProperty("LookupId", out JsonElement lookupIdElement));
            Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", lookupIdElement.GetString());
        }

        Assert.True(root.TryGetProperty("Level", out JsonElement levelElement));
        Assert.Equal("Information", levelElement.GetString());

        Assert.True(root.TryGetProperty("Message", out JsonElement messageElement));
        Assert.Equal("Processing request started", messageElement.GetString());
    }

    private static void AssertWarningLog(string functionName, bool isColdStart, string output)
    {
        using JsonDocument doc = JsonDocument.Parse(output);
        JsonElement root = doc.RootElement;
        
        AssertDefaultLoggingProperties.ArePresent(functionName, isColdStart, output);

        Assert.True(root.TryGetProperty("LookupInfo", out JsonElement lookupInfoElement));
        Assert.True(lookupInfoElement.TryGetProperty("LookupId", out JsonElement lookupIdElement));
        Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", lookupIdElement.GetString());

        Assert.True(root.TryGetProperty("Level", out JsonElement levelElement));
        Assert.Equal("Warning", levelElement.GetString());

        Assert.True(root.TryGetProperty("Test1", out JsonElement test1Element));
        Assert.Equal("value1", test1Element.GetString());
        
        Assert.True(root.TryGetProperty("Test2", out JsonElement test2Element));
        Assert.Equal("value2", test2Element.GetString());
        
        Assert.True(root.TryGetProperty("Message", out JsonElement messageElement));
        Assert.Equal("Warn with additional keys", messageElement.GetString());
    }

    private void AssertExceptionLog(string functionName, bool isColdStart, string output)
    {
        using JsonDocument doc = JsonDocument.Parse(output);
        JsonElement root = doc.RootElement;
        
        AssertDefaultLoggingProperties.ArePresent(functionName, isColdStart, output);

        Assert.True(root.TryGetProperty("LookupInfo", out JsonElement lookupInfoElement));
        Assert.True(lookupInfoElement.TryGetProperty("LookupId", out JsonElement lookupIdElement));
        Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", lookupIdElement.GetString());

        Assert.True(root.TryGetProperty("Level", out JsonElement levelElement));
        Assert.Equal("Error", levelElement.GetString());

        Assert.True(root.TryGetProperty("Message", out JsonElement messageElement));
        Assert.Equal("Oops something went wrong", messageElement.GetString());

        Assert.True(root.TryGetProperty("Exception", out JsonElement exceptionElement));
        Assert.True(exceptionElement.TryGetProperty("Type", out JsonElement exceptionTypeElement));
        Assert.Equal("System.InvalidOperationException", exceptionTypeElement.GetString());

        Assert.True(exceptionElement.TryGetProperty("Message", out JsonElement exceptionMessageElement));
        Assert.Equal("Parent exception message", exceptionMessageElement.GetString());
        
        Assert.False(root.TryGetProperty("Test1", out JsonElement _));
        Assert.False(root.TryGetProperty("Test2", out JsonElement _));
    }
}