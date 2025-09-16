using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.SourceGenerator.Tests;

public class UASetter
{
    private readonly ITestOutputHelper _output;
    private readonly string? _appId;

    public UASetter(ITestOutputHelper output)
    {
        _output = output;
        _appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
    }
    
    [Fact]
    public void Is_PTENV_Set()
    {
        Assert.NotNull(_appId);
        Assert.Contains("PTENV/", _appId);
        CheckUtilityOnlyAppearsOnce(_appId);
        _output.WriteLine(_appId);
        // check that it is last in the string
        Assert.EndsWith("PTENV/", _appId, StringComparison.OrdinalIgnoreCase);
    }
    
    [Theory]
    [InlineData("Logging")]
    [InlineData("Tracing")]
    [InlineData("BatchProcessing")]
    [InlineData("Idempotency")]
    [InlineData("Metrics")]
    [InlineData("EventHandler")]
    [InlineData("BedrockAgentFunction")]
    [InlineData("Kafka.Avro")]
    [InlineData("Kafka.Json")]
    [InlineData("Kafka.Protobuf")]
    [InlineData("Parameters")]
    public void Is_Utility_Set(string utility)
    {
        var appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Assert.NotNull(appId);
        Assert.Contains($"PT/{utility}/1.0.0", appId);
        
        CheckUtilityOnlyAppearsOnce(appId);
        _output.WriteLine(_appId);
    }
    
    private static void CheckUtilityOnlyAppearsOnce(string appId)
    {
        // only appears once
        var matches = Regex.Matches(appId, Regex.Escape(appId));
        Assert.Single(matches);
    }
}
