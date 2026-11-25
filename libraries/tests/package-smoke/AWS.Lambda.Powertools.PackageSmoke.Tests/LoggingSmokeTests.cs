using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Logging;
using Xunit;

namespace PackageSmoke.Tests;

public class LoggingFunction
{
    [Logging(Service = "smoke-logging", LogEvent = true, CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        Logger.AppendKey("lookup_id", request.RequestContext.RequestId);
        Logger.LogInformation("Hello from {Utility}", "Logging");

        return new APIGatewayProxyResponse { StatusCode = 200, Body = "ok" };
    }
}

public class LoggingSmokeTests
{
    [Fact]
    public void Handler_Writes_Structured_Log_With_Lambda_Context_And_Correlation_Id()
    {
        var requestId = Guid.NewGuid().ToString("D");
        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { RequestId = requestId }
        };
        var context = new TestLambdaContext
        {
            FunctionName = "PackageSmoke-Logging",
            FunctionVersion = "1",
            MemoryLimitInMB = 256,
            AwsRequestId = Guid.NewGuid().ToString("D")
        };

        using var console = new ConsoleCapture();
        var response = new LoggingFunction().FunctionHandler(request, context);

        Assert.Equal(200, response.StatusCode);

        var lines = console.JsonLines();
        var entry = Assert.Single(lines, l => l.TryGetProperty("message", out var m)
                                              && m.ValueKind == JsonValueKind.String
                                              && m.GetString() == "Hello from Logging");
        Assert.Equal("Information", entry.GetProperty("level").GetString());
        Assert.Equal("smoke-logging", entry.GetProperty("service").GetString());
        Assert.Equal(context.FunctionName, entry.GetProperty("function_name").GetString());
        Assert.Equal(context.AwsRequestId, entry.GetProperty("function_request_id").GetString());
        Assert.Equal(requestId, entry.GetProperty("correlation_id").GetString());
        Assert.Equal(requestId, entry.GetProperty("lookup_id").GetString());
        Assert.True(entry.TryGetProperty("cold_start", out _));

        // LogEvent = true logs the incoming event as an object.
        Assert.Contains(lines, l => l.TryGetProperty("message", out var m)
                                    && m.ValueKind == JsonValueKind.Object
                                    && m.ToString().Contains(requestId));
    }
}
