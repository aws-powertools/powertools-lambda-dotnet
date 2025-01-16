using System.Text.Json;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.Model;
using TestUtils;
using Xunit.Abstractions;

namespace Function.Tests;

[Trait("Category", "E2E")]
public class FunctionTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AmazonLambdaClient _lambdaClient;

    public FunctionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _lambdaClient = new AmazonLambdaClient(new AmazonLambdaConfig
        {
            Timeout = TimeSpan.FromSeconds(7)
        });
    }
    
    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_X64_AOT_NET8_tracing")]
    // [InlineData("E2ETestLambda_ARM_AOT_NET8_tracing")]
    public async Task AotFunctionTest(string functionName)
    {
        await TestFunction(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET6_tracing")]
    [InlineData("E2ETestLambda_ARM_NET6_tracing")]
    [InlineData("E2ETestLambda_X64_NET8_tracing")]
    [InlineData("E2ETestLambda_ARM_NET8_tracing")]
    public async Task FunctionTest(string functionName)
    {
        await TestFunction(functionName);
    }

    internal async Task TestFunction(string functionName)
    {
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = await File.ReadAllTextAsync("../../../../../../../payload.json"),
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
            AssertOutputLog(response);
        }
    }

    private void AssertOutputLog(InvokeResponse response)
    {
        // Extract and parse log
        var logResult = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
        _testOutputHelper.WriteLine(logResult);
        var output = OutputLogParser.ParseLogSegments(logResult, out var report);
        var isColdStart = report.initDuration != "N/A";
    }
}