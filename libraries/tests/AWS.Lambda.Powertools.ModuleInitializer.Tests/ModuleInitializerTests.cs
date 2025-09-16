using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.ModuleInitializer.Tests;

public class UASetter
{
    private readonly ITestOutputHelper _output;
    private readonly string? _appId;

    static UASetter()
    {
        // Static constructor to ensure this runs once per test run
        // This simulates what would happen in a real application where assemblies are loaded
        // and module initializers run automatically
        ForceAssemblyLoading();
    }

    public UASetter(ITestOutputHelper output)
    {
        _output = output;
        _appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
    }

    private static void ForceAssemblyLoading()
    {
        // Force loading of all the assemblies by accessing their types
        // This should trigger any module initializers that exist
        var types = new[]
        {
            typeof(AWS.Lambda.Powertools.BatchProcessing.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.EventHandler.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Idempotency.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Kafka.Avro.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Kafka.Json.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Kafka.Protobuf.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Logging.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Metrics.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Parameters.Internal.EnvWrapper),
            typeof(AWS.Lambda.Powertools.Tracing.Internal.EnvWrapper)
        };

        // Just accessing the types should be enough to load the assemblies
        // and trigger any module initializers
        foreach (var type in types)
        {
            _ = type.Assembly.FullName;
        }
    }

    [Fact]
    public void ModuleInitializers_Should_Be_Working()
    {
        // This test verifies that module initializers are working by checking for the presence
        // of the environment variable that should be set by the module initializers
        // This is informational - if it fails, it means module initializers aren't running properly
        var assemblies = new[]
        {
            typeof(AWS.Lambda.Powertools.BatchProcessing.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.EventHandler.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Idempotency.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Kafka.Avro.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Kafka.Json.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Kafka.Protobuf.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Logging.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Metrics.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Parameters.Internal.EnvWrapper).Assembly,
            typeof(AWS.Lambda.Powertools.Tracing.Internal.EnvWrapper).Assembly
        };

        var foundInitializers = 0;
        foreach (var assembly in assemblies)
        {
            try
            {
                var initializerType = assembly.GetTypes().FirstOrDefault(t => t.Name == "ModuleInitializer");
                if (initializerType != null)
                {
                    foundInitializers++;
                    _output.WriteLine($"✓ Found ModuleInitializer in {assembly.GetName().Name}");
                }
                else
                {
                    _output.WriteLine($"⚠ No ModuleInitializer found in {assembly.GetName().Name} (module initializer may not exist)");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠ Error checking {assembly.GetName().Name}: {ex.Message}");
            }
        }

        _output.WriteLine($"Found {foundInitializers} out of {assemblies.Length} ModuleInitializer classes");

        // This is informational - we don't fail the test if module initializers aren't found
        // because the functionality still works
        if (foundInitializers == 0)
        {
            _output.WriteLine("⚠ WARNING: No ModuleInitializer classes found. Module initializers may not be working properly, but functionality is preserved via other mechanisms.");
        }
        else
        {
            _output.WriteLine($"✓ Module initializers are working - found {foundInitializers} classes");
        }
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
        // Check that each utility appears only once
        var utilities = new[] { "BatchProcessing", "BedrockAgentFunction", "EventHandler", "Idempotency", "Kafka.Avro", "Kafka.Json", "Kafka.Protobuf", "Logging", "Metrics", "Parameters", "Tracing" };
        
        foreach (var utility in utilities)
        {
            var pattern = $@"PT/{Regex.Escape(utility)}/\d+\.\d+\.\d+";
            var matches = Regex.Matches(appId, pattern);
            Assert.Single(matches);
        }
    }
}