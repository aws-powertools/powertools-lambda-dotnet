using System.Text.Json;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.Model;
using TestUtils;
using Xunit.Abstractions;
using Environment = Amazon.Lambda.Model.Environment;

namespace Function.Tests;

[Trait("Category", "E2E")]
public class FunctionTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AmazonLambdaClient _lambdaClient;

    public FunctionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _lambdaClient = new AmazonLambdaClient();
    }
    
    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_X64_AOT_NET8_logging_AOT-Function")]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_logging_AOT-Function")]
    public async Task AotFunctionTest(string functionName)
    {
        // await ResetFunction(functionName);
        await TestFunction(functionName);
    }
    
    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_X64_AOT_NET8_logging_AOT-Function-ILogger")]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_logging_AOT-Function-ILogger")]
    public async Task AotILoggerFunctionTest(string functionName)
    {
        // await ResetFunction(functionName);
        await TestFunction(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_logging")]
    [InlineData("E2ETestLambda_ARM_NET8_logging")]
    public async Task FunctionTest(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::Function.Function::FunctionHandler");
        await TestFunction(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_logging")]
    [InlineData("E2ETestLambda_ARM_NET8_logging")]
    public async Task StaticConfigurationFunctionTest(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::StaticConfiguration.Function::FunctionHandler");
        await TestFunction(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_logging")]
    [InlineData("E2ETestLambda_ARM_NET8_logging")]
    public async Task StaticILoggerConfigurationFunctionTest(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::StaticILoggerConfiguration.Function::FunctionHandler");
        await TestFunction(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_logging")]
    [InlineData("E2ETestLambda_ARM_NET8_logging")]
    public async Task ILoggerConfigurationFunctionTest(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::ILoggerConfiguration.Function::FunctionHandler");
        await TestFunction(functionName);
    }
    
    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_logging")]
    [InlineData("E2ETestLambda_ARM_NET8_logging")]
    public async Task ILoggerBuilderFunctionTest(string functionName)
    {
        await UpdateFunctionHandler(functionName, "Function::ILoggerBuilder.Function::FunctionHandler");
        await TestFunction(functionName);
    }

    internal async Task TestFunction(string functionName)
    {
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = await File.ReadAllTextAsync("../../../../../../../../payload.json"),
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
        
        // LookupInfo is only present on warm starts, but due to race conditions in parallel tests
        // we can't reliably predict cold/warm state. Only validate LookupInfo if it exists.
        if (root.TryGetProperty("LookupInfo", out JsonElement lookupInfoElement))
        {
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
        
        Assert.True(messageElement.TryGetProperty("RequestContext", out JsonElement requestContextElement));
        Assert.True(requestContextElement.TryGetProperty("Path", out JsonElement requestContextPathElement));
        Assert.Equal("/prod/path/to/resource", requestContextPathElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("ResourceId", out JsonElement resourceIdElement));
        Assert.Equal("123456", resourceIdElement.GetString());
        
        Assert.True(requestContextElement.TryGetProperty("RequestId", out JsonElement requestIdElement));
        Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", requestIdElement.GetString());
        
        Assert.True(
            requestContextElement.TryGetProperty("HttpMethod", out JsonElement requestContextHttpMethodElement));
        Assert.Equal("POST", requestContextHttpMethodElement.GetString());

        Assert.True(requestContextElement.TryGetProperty("ApiId", out JsonElement apiIdElement));
        Assert.Equal("1234567890", apiIdElement.GetString());

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
        
        // LookupInfo is only present on warm starts, but due to race conditions in parallel tests
        // we can't reliably predict cold/warm state. Only validate LookupInfo if it exists.
        if (root.TryGetProperty("LookupInfo", out JsonElement lookupInfoElement))
        {
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
    
    private async Task UpdateFunctionHandler(string functionName, string handler)
    {
        var updateRequest = new UpdateFunctionConfigurationRequest
        {
            FunctionName = functionName,
            Handler = handler,
            Environment = new Environment
            {
                Variables = new Dictionary<string, string>
                {
                    { "ForceColdStart", Guid.NewGuid().ToString() }
                }
            }
        };

        var updateResponse = await _lambdaClient.UpdateFunctionConfigurationAsync(updateRequest);
        
        if (updateResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine($"Successfully updated the handler for function {functionName} to {handler}");
        }
        else
        {
            Assert.Fail(
                $"Failed to update the handler for function {functionName}. Status code: {updateResponse.HttpStatusCode}");
        }
        
        //wait for the changes to take effect and force cold start
        await Task.Delay(15000);
    }
    
    private async Task ResetFunction(string functionName)
    {
        var updateRequest = new UpdateFunctionConfigurationRequest
        {
            FunctionName = functionName,
            Environment = new Environment 
            {
                Variables = 
                {
                    {"Updated", DateTime.UtcNow.ToString("G")}
                }
            }
        };

        await _lambdaClient.UpdateFunctionConfigurationAsync(updateRequest);
        
        //wait a few seconds for the changes to take effect
        await Task.Delay(1000);
    }
}