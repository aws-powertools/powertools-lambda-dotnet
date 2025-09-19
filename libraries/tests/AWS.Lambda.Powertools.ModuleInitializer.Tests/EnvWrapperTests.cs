using System;
using System.Text.RegularExpressions;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Tracing.Internal;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.Idempotency.Internal;
using AWS.Lambda.Powertools.Metrics.Internal;
using AWS.Lambda.Powertools.Parameters.Internal;
using AWS.Lambda.Powertools.EventHandler.Internal;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal;
using AWS.Lambda.Powertools.Kafka.Avro.Internal;
using AWS.Lambda.Powertools.Kafka.Json.Internal;
using AWS.Lambda.Powertools.Kafka.Protobuf.Internal;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.ModuleInitializer.Tests;

public class EnvWrapperTests
{
    private readonly ITestOutputHelper _output;
    private readonly string? _appId;

    public EnvWrapperTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Clear any existing environment variable to start fresh
        Environment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", null);
        
        // Call the method we want to test - this should set the environment variable
        AWS.Lambda.Powertools.Logging.Internal.EnvWrapper.SetExecutionEnvironment();
        
        _appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        
        // Log the current state for debugging
        _output.WriteLine($"AWS_SDK_UA_APP_ID after SetExecutionEnvironment(): '{_appId ?? "null"}'");
    }

    [Fact]
    public void SetExecutionEnvironment_Should_Set_AWS_SDK_UA_APP_ID()
    {
        // Verify that calling SetExecutionEnvironment() sets the environment variable
        Assert.NotNull(_appId);
        Assert.NotEmpty(_appId);
        _output.WriteLine($"Environment variable set: {_appId}");
    }

    [Fact]
    public void Is_PTENV_Set()
    {
        Assert.NotNull(_appId);
        Assert.Contains("PTEnv/", _appId);
        _output.WriteLine(_appId);
        // check that it is last in the string
        Assert.EndsWith("PTEnv/", _appId, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetExecutionEnvironment_Should_Set_Logging_Utility()
    {
        // Since we're calling EnvWrapper from the Logging library, it should set the Logging utility
        Assert.NotNull(_appId);
        Assert.Contains("PT/Logging/", _appId);
        _output.WriteLine($"Environment variable contains Logging utility: {_appId}");
    }

    [Fact]
    public void SetExecutionEnvironment_Should_Have_Correct_Format()
    {
        Assert.NotNull(_appId);
        
        // Should end with PTEnv/
        Assert.EndsWith("PTEnv/", _appId);
        
        // Should contain at least one PT/ entry
        var ptEntries = Regex.Matches(_appId, @"PT/[^/]+/\d+\.\d+\.\d+");
        Assert.True(ptEntries.Count >= 1, $"Expected at least 1 PT/ entry, found {ptEntries.Count}");
        
        _output.WriteLine($"Found {ptEntries.Count} PT/ entries in the environment variable");
    }

    [Fact]
    public void SetExecutionEnvironment_Should_Include_Version_Number()
    {
        Assert.NotNull(_appId);
        
        // Should contain a version number in the format x.y.z
        var versionPattern = @"PT/Logging/\d+\.\d+\.\d+";
        Assert.Matches(versionPattern, _appId);
        
        _output.WriteLine($"Environment variable contains version: {_appId}");
    }

    [Fact]
    public void SetExecutionEnvironment_Should_Only_Include_Logging_Once()
    {
        Assert.NotNull(_appId);
        
        // Check that Logging utility appears exactly once
        var pattern = @"PT/Logging/\d+\.\d+\.\d+";
        var matches = Regex.Matches(_appId, pattern);
        Assert.Single(matches);
        
        _output.WriteLine($"Logging utility appears exactly once: {matches[0].Value}");
    }

    [Fact]
    public void SetExecutionEnvironment_Multiple_Calls_Should_Not_Duplicate()
    {
        // Clear and call multiple times to ensure no duplication
        Environment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", null);
        
        AWS.Lambda.Powertools.Logging.Internal.EnvWrapper.SetExecutionEnvironment();
        var firstCall = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        
        AWS.Lambda.Powertools.Logging.Internal.EnvWrapper.SetExecutionEnvironment();
        var secondCall = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        
        // Both calls should produce the same result (no duplication)
        Assert.Equal(firstCall, secondCall);
        
        _output.WriteLine($"First call: {firstCall}");
        _output.WriteLine($"Second call: {secondCall}");
    }

    [Fact(Skip = "This will be added back when we have bitwise")]
    public void SetExecutionEnvironment_All_Libraries_Should_Set_All_Utilities()
    {
        // Clear environment variable to start fresh
        Environment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", null);
        
        // Call SetExecutionEnvironment from all EnvWrapper classes to simulate
        // what would happen when all libraries are used in a real application
        AWS.Lambda.Powertools.Logging.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Tracing.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.BatchProcessing.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Idempotency.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Metrics.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Parameters.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.EventHandler.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Kafka.Avro.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Kafka.Json.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Kafka.Protobuf.Internal.EnvWrapper.SetExecutionEnvironment();
        
        var appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Assert.NotNull(appId);
        
        _output.WriteLine($"Complete environment variable: {appId}");
        
        // Verify all expected utilities are present
        var expectedUtilities = new[]
        {
            "Logging",
            "Tracing", 
            "BatchProcessing",
            "Idempotency",
            "Metrics",
            "Parameters",
            "EventHandler",
            "BedrockAgentFunction",
            "Kafka.Avro",
            "Kafka.Json",
            "Kafka.Protobuf"
        };

        foreach (var utility in expectedUtilities)
        {
            Assert.Contains($"PT/{utility}/", appId);
            _output.WriteLine($"✓ Found utility: {utility}");
        }
        
        // Verify PTEnv/ is at the end
        Assert.EndsWith("PTEnv/", appId);
        
        // Verify each utility appears exactly once
        foreach (var utility in expectedUtilities)
        {
            var pattern = $@"PT/{Regex.Escape(utility)}/\d+\.\d+\.\d+";
            var matches = Regex.Matches(appId, pattern);
            Assert.Single(matches);
        }
        
        // Count total PT/ entries
        var ptEntries = Regex.Matches(appId, @"PT/[^/]+/\d+\.\d+\.\d+");
        Assert.Equal(expectedUtilities.Length, ptEntries.Count);
        
        _output.WriteLine($"Total utilities found: {ptEntries.Count}");
    }
    
    [Fact]
    public void SetExecutionEnvironment_All_Libraries_Should_Set_Only_One_Utility()
    {
        // Clear environment variable to start fresh
        Environment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", null);
        
        // Call SetExecutionEnvironment from all EnvWrapper classes to simulate
        // what would happen when all libraries are used in a real application
        AWS.Lambda.Powertools.Logging.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Tracing.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.BatchProcessing.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Idempotency.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Metrics.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Parameters.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.EventHandler.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Kafka.Avro.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Kafka.Json.Internal.EnvWrapper.SetExecutionEnvironment();
        AWS.Lambda.Powertools.Kafka.Protobuf.Internal.EnvWrapper.SetExecutionEnvironment();
        
        var appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Assert.NotNull(appId);
        
        _output.WriteLine($"Complete environment variable: {appId}");
        
        // Verify all expected utilities are present
        var utilitiesThatShouldNotBePresent = new[]
        {
            // "Logging",
            "Tracing", 
            "BatchProcessing",
            "Idempotency",
            "Metrics",
            "Parameters",
            "EventHandler",
            "BedrockAgentFunction",
            "Kafka.Avro",
            "Kafka.Json",
            "Kafka.Protobuf"
        };

        foreach (var utility in utilitiesThatShouldNotBePresent)
        {
            Assert.DoesNotContain($"PT/{utility}/", appId);
            _output.WriteLine($"✓ Not Found utility: {utility}");
        }
        
        // Verify PTEnv/ is at the end
        Assert.EndsWith("PTEnv/", appId);
        
        // Count total PT/ entries
        var ptEntries = Regex.Matches(appId, @"PT/[^/]+/\d+\.\d+\.\d+");
        Assert.Single(ptEntries);
        
        _output.WriteLine($"Total utilities found: {ptEntries.Count}");
    }

    [Theory]
    [InlineData("Logging")]
    [InlineData("Tracing")]
    [InlineData("BatchProcessing")]
    [InlineData("Idempotency")]
    [InlineData("Metrics")]
    [InlineData("Parameters")]
    [InlineData("EventHandler")]
    [InlineData("BedrockAgentFunction")]
    [InlineData("Kafka.Avro")]
    [InlineData("Kafka.Json")]
    [InlineData("Kafka.Protobuf")]
    public void SetExecutionEnvironment_Individual_Library_Should_Set_Specific_Utility(string expectedUtility)
    {
        // Clear environment variable
        Environment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", null);
        
        // Call the appropriate EnvWrapper based on the utility name
        switch (expectedUtility)
        {
            case "Logging":
                AWS.Lambda.Powertools.Logging.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "Tracing":
                AWS.Lambda.Powertools.Tracing.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "BatchProcessing":
                AWS.Lambda.Powertools.BatchProcessing.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "Idempotency":
                AWS.Lambda.Powertools.Idempotency.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "Metrics":
                AWS.Lambda.Powertools.Metrics.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "Parameters":
                AWS.Lambda.Powertools.Parameters.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "EventHandler":
                AWS.Lambda.Powertools.EventHandler.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "BedrockAgentFunction":
                AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "Kafka.Avro":
                AWS.Lambda.Powertools.Kafka.Avro.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "Kafka.Json":
                AWS.Lambda.Powertools.Kafka.Json.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
            case "Kafka.Protobuf":
                AWS.Lambda.Powertools.Kafka.Protobuf.Internal.EnvWrapper.SetExecutionEnvironment();
                break;
        }
        
        var appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Assert.NotNull(appId);
        
        // Verify the specific utility is present
        Assert.Contains($"PT/{expectedUtility}/", appId);
        
        // Verify PTEnv/ is at the end
        Assert.EndsWith("PTEnv/", appId);
        
        _output.WriteLine($"Testing {expectedUtility}: {appId}");
    }
}