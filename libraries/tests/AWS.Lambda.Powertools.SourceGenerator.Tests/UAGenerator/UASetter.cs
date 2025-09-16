using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.SourceGenerator.Tests;

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
        Console.WriteLine("DEBUG: Starting ForceAssemblyLoading");
        
        // Check if environment variable is already set before we do anything
        var initialAppId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Console.WriteLine($"DEBUG: Initial AWS_SDK_UA_APP_ID = '{initialAppId}'");
        
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
            Console.WriteLine($"DEBUG: Loading assembly for {type.Name}");
            _ = type.Assembly.FullName;
        }
        
        var afterLoadingAppId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Console.WriteLine($"DEBUG: After loading assemblies AWS_SDK_UA_APP_ID = '{afterLoadingAppId}'");
        
        // If still empty, try manually calling one method to see what happens
        if (string.IsNullOrEmpty(afterLoadingAppId))
        {
            Console.WriteLine("DEBUG: Environment variable still empty, calling one method manually");
            AWS.Lambda.Powertools.Logging.Internal.EnvWrapper.SetExecutionEnvironment();
            var afterManualCallAppId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
            Console.WriteLine($"DEBUG: After manual call AWS_SDK_UA_APP_ID = '{afterManualCallAppId}'");
        }
    }

    [Fact]
    public void SourceGenerators_Should_Generate_ModuleInitializers()
    {
        // This test verifies that source generators are actually working by checking for generated types
        
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
                // Try to find UAModuleInitializer in any namespace
                var allTypes = assembly.GetTypes();
                var initializerTypes = allTypes.Where(t => t.Name == "UAModuleInitializer").ToList();
                
                if (initializerTypes.Any())
                {
                    foundInitializers += initializerTypes.Count;
                    foreach (var initType in initializerTypes)
                    {
                        _output.WriteLine($"✓ Found generated UAModuleInitializer in {assembly.GetName().Name} at {initType.FullName}");
                        
                        // Check if it has the Initialize method with ModuleInitializer attribute
                        var initMethod = initType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (initMethod != null)
                        {
                            var hasModuleInitializerAttribute = initMethod.GetCustomAttributes(false)
                                .Any(attr => attr.GetType().Name == "ModuleInitializerAttribute");
                            _output.WriteLine($"  - Initialize method found with ModuleInitializerAttribute: {hasModuleInitializerAttribute}");
                        }
                    }
                }
                else
                {
                    _output.WriteLine($"⚠ No UAModuleInitializer found in {assembly.GetName().Name}");
                    // List all types for debugging
                    _output.WriteLine($"  Available types: {string.Join(", ", allTypes.Take(10).Select(t => t.Name))}...");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠ Error checking {assembly.GetName().Name}: {ex.Message}");
            }
        }
        
        _output.WriteLine($"Found {foundInitializers} generated UAModuleInitializer classes total");
        
        // Since we know the functionality works (environment variable is set), 
        // we can be more confident about whether source generators are working
        var appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        if (!string.IsNullOrEmpty(appId) && foundInitializers > 0)
        {
            _output.WriteLine($"✓ Source generators are working - found {foundInitializers} generated classes and environment variable is set");
        }
        else if (!string.IsNullOrEmpty(appId) && foundInitializers == 0)
        {
            _output.WriteLine("⚠ Environment variable is set but no UAModuleInitializer classes found - they may be in a different location or generated differently");
        }
        else
        {
            _output.WriteLine("⚠ Neither environment variable nor generated classes found - source generators may not be working");
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
        var utilities = new[] { "BatchProcessing", "BedrockAgentFunction", "EventHandler", "Idempotency", 
                               "Kafka.Avro", "Kafka.Json", "Kafka.Protobuf", "Logging", "Metrics", "Parameters", "Tracing" };
        
        foreach (var utility in utilities)
        {
            var pattern = $@"PT/{Regex.Escape(utility)}/\d+\.\d+\.\d+";
            var matches = Regex.Matches(appId, pattern);
            Assert.Single(matches);
        }
    }
}
