using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.ModuleInitializer.Tests;

public class MSBuildAutoInitializationTests
{
    private readonly ITestOutputHelper _output;
    private readonly string? _appId;

    public MSBuildAutoInitializationTests(ITestOutputHelper output)
    {
        _output = output;
        _appId = Environment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        
        // Log the current state for debugging
        _output.WriteLine($"AWS_SDK_UA_APP_ID: '{_appId ?? "null"}'");
        
        // Log loaded assemblies to verify which Powertools assemblies are loaded
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("AWS.Lambda.Powertools") == true)
            .Select(a => a.GetName().Name)
            .OrderBy(name => name)
            .ToList();
            
        _output.WriteLine($"Loaded Powertools assemblies: {string.Join(", ", loadedAssemblies)}");
    }

    [Fact]
    public void MSBuildTargets_Should_Exist_In_Library_Projects()
    {
        // This test verifies that the MSBuild targets files exist in the library projects
        // Since this test project uses ProjectReferences, the MSBuild targets won't run here,
        // but we can verify they exist in the source projects
        
        var libraryNames = new[]
        {
            "AWS.Lambda.Powertools.Logging",
            "AWS.Lambda.Powertools.BatchProcessing", 
            "AWS.Lambda.Powertools.EventHandler",
            "AWS.Lambda.Powertools.Idempotency",
            "AWS.Lambda.Powertools.Metrics",
            "AWS.Lambda.Powertools.Parameters",
            "AWS.Lambda.Powertools.Tracing",
            "AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction",
            "AWS.Lambda.Powertools.Kafka.Avro",
            "AWS.Lambda.Powertools.Kafka.Json",
            "AWS.Lambda.Powertools.Kafka.Protobuf"
        };

        var foundTargets = 0;
        foreach (var libraryName in libraryNames)
        {
            // Navigate from bin/Debug/net8.0 back to libraries, then to src
            var targetsPath = $"../../../../../src/{libraryName}/build/{libraryName}.targets";
            var fullPath = System.IO.Path.GetFullPath(targetsPath);
            
            if (System.IO.File.Exists(fullPath))
            {
                foundTargets++;
                _output.WriteLine($"  ✓ Found targets file: {libraryName}.targets");
                
                // Verify the targets file contains the expected content
                var content = System.IO.File.ReadAllText(fullPath);
                Assert.Contains("BeforeTargets=\"BeforeCompile\"", content);
                Assert.Contains("ModuleInitializer", content);
                Assert.Contains("EnvWrapper.SetExecutionEnvironment", content);
            }
            else
            {
                _output.WriteLine($"  ⚠ Missing targets file: {targetsPath}");
            }
        }

        _output.WriteLine($"Found {foundTargets} out of {libraryNames.Length} MSBuild targets files");
        
        // All libraries should have targets files
        Assert.Equal(libraryNames.Length, foundTargets);
    }

    [Fact]
    public void MSBuild_Targets_Generate_Correct_Module_Initializer_Code()
    {
        // This test verifies that the MSBuild targets generate the correct module initializer code
        // that will set the AWS_SDK_UA_APP_ID environment variable when used with NuGet packages
        
        _output.WriteLine("Verifying that MSBuild targets generate correct module initializer code...");
        
        var libraryNames = new[]
        {
            "AWS.Lambda.Powertools.Logging",
            "AWS.Lambda.Powertools.BatchProcessing", 
            "AWS.Lambda.Powertools.EventHandler",
            "AWS.Lambda.Powertools.Idempotency",
            "AWS.Lambda.Powertools.Metrics",
            "AWS.Lambda.Powertools.Parameters",
            "AWS.Lambda.Powertools.Tracing"
        };

        var validTargets = 0;
        foreach (var libraryName in libraryNames)
        {
            var targetsPath = $"../../../../../src/{libraryName}/build/{libraryName}.targets";
            var fullPath = System.IO.Path.GetFullPath(targetsPath);
            
            if (System.IO.File.Exists(fullPath))
            {
                var content = System.IO.File.ReadAllText(fullPath);
                
                // Verify the targets file generates the correct module initializer code
                var hasBeforeCompile = content.Contains("BeforeTargets=\"BeforeCompile\"");
                var hasModuleInitializer = content.Contains("ModuleInitializer");
                var hasEnvWrapperCall = content.Contains("EnvWrapper.SetExecutionEnvironment");
                var hasWriteLinesToFile = content.Contains("WriteLinesToFile");
                var hasIntermediateOutputPath = content.Contains("$(IntermediateOutputPath)");
                
                if (hasBeforeCompile && hasModuleInitializer && hasEnvWrapperCall && hasWriteLinesToFile && hasIntermediateOutputPath)
                {
                    validTargets++;
                    _output.WriteLine($"  ✓ {libraryName}: MSBuild target generates correct module initializer code");
                }
                else
                {
                    _output.WriteLine($"  ⚠ {libraryName}: MSBuild target missing required elements:");
                    if (!hasBeforeCompile) _output.WriteLine($"    - Missing BeforeTargets=\"BeforeCompile\"");
                    if (!hasModuleInitializer) _output.WriteLine($"    - Missing ModuleInitializer");
                    if (!hasEnvWrapperCall) _output.WriteLine($"    - Missing EnvWrapper.SetExecutionEnvironment");
                    if (!hasWriteLinesToFile) _output.WriteLine($"    - Missing WriteLinesToFile");
                    if (!hasIntermediateOutputPath) _output.WriteLine($"    - Missing $(IntermediateOutputPath)");
                }
            }
            else
            {
                _output.WriteLine($"  ⚠ {libraryName}: MSBuild targets file not found");
            }
        }

        _output.WriteLine($"Found {validTargets} out of {libraryNames.Length} libraries with correct MSBuild targets");
        
        // All main libraries should have correct MSBuild targets that generate proper module initializers
        Assert.Equal(libraryNames.Length, validTargets);
    }
}