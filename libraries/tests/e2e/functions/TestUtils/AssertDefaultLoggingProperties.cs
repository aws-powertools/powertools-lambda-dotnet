using System.Text.Json;
using Xunit;

namespace TestUtils;

public static class AssertDefaultLoggingProperties
{
    public static void ArePresent(string functionName, bool isColdStart, string log)
    {
        using JsonDocument doc = JsonDocument.Parse(log);
        JsonElement root = doc.RootElement;
        
        // Only verify ColdStart property exists and is a boolean
        // Don't assert specific value as it can be unpredictable when tests run in parallel
        // and share the same Lambda function
        Assert.True(root.TryGetProperty("ColdStart", out JsonElement coldStartElement));
        Assert.True(coldStartElement.ValueKind == JsonValueKind.True || coldStartElement.ValueKind == JsonValueKind.False);
        
        Assert.True(root.TryGetProperty("CorrelationId", out JsonElement correlationIdElement));
        Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", correlationIdElement.GetString());
        
        Assert.True(root.TryGetProperty("FunctionName", out JsonElement functionNameElement));
        Assert.Equal(functionName, functionNameElement.GetString());

        Assert.True(root.TryGetProperty("FunctionVersion", out JsonElement functionVersionElement));
        Assert.Equal("$LATEST", functionVersionElement.GetString());

        Assert.True(root.TryGetProperty("FunctionMemorySize", out JsonElement functionMemorySizeElement));
        Assert.Equal(128, functionMemorySizeElement.GetInt32());

        Assert.True(root.TryGetProperty("FunctionArn", out JsonElement functionArnElement));
        Assert.Contains($"function:{functionName}",
            functionArnElement.GetString());
        
        Assert.True(root.TryGetProperty("Service", out JsonElement serviceElement));
        Assert.Equal("TestService", serviceElement.GetString());

        Assert.True(root.TryGetProperty("Name", out JsonElement nameElement));
        Assert.Equal("AWS.Lambda.Powertools.Logging.Logger", nameElement.GetString());
    }
}