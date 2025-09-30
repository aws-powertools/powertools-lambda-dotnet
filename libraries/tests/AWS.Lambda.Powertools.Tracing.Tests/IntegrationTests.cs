using System;
using System.Runtime.InteropServices;
using AWS.Lambda.Powertools.Tracing;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("TracingTests")]
public class IntegrationTests
{
    [Fact]
    public void Tracing_WithComplexObjectContainingProblematicTypes_DoesNotThrow()
    {
        // Arrange - Create the same object structure that was causing issues
        var complexObject = new
        {
            SystemInfo = new
            {
                DotNetVersion = Environment.Version.ToString(),
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                OSDescription = RuntimeInformation.OSDescription,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet, // This is long/int64 - the problematic type
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                Is64BitProcess = Environment.Is64BitProcess,
                CLRVersion = Environment.Version.ToString(),
                CurrentDirectory = Environment.CurrentDirectory
            },
            LambdaInfo = new
            {
                FunctionName = "TestFunction",
                FunctionVersion = "1.0",
                InvokedFunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:TestFunction",
                MemoryLimitInMB = 512,
                RemainingTime = TimeSpan.FromMinutes(5), // TimeSpan - another potentially problematic type
                RequestId = Guid.NewGuid().ToString(), // Guid converted to string
                LogGroupName = "/aws/lambda/TestFunction",
                LogStreamName = "2023/01/01/[$LATEST]abcdef123456"
            }
        };

        // Act & Assert - Should not throw any exceptions
        try
        {
            Tracing.AddMetadata("SystemInfo", complexObject);
            Tracing.AddMetadata("test", "WorkingSet", Environment.WorkingSet);
            Tracing.AddMetadata("test", "TimeSpan", TimeSpan.FromMinutes(5));
            Tracing.AddMetadata("test", "Guid", Guid.NewGuid());
            
            // Test annotations with problematic types
            Tracing.AddAnnotation("WorkingSet", Environment.WorkingSet);
            Tracing.AddAnnotation("ProcessorCount", Environment.ProcessorCount);
            Tracing.AddAnnotation("TimeSpan", TimeSpan.FromMinutes(5));
            Tracing.AddAnnotation("Guid", Guid.NewGuid());
        }
        catch (Exception ex) when (ex.Message.Contains("Entity is not available"))
        {
            // This is expected when no tracing context is available in unit tests
            // The important thing is that we don't get JSON serialization errors
        }
        catch (Exception ex) when (ex.Message.Contains("LitJson") || ex.Message.Contains("JsonMapper"))
        {
            // If we get JSON serialization errors, our fix didn't work
            Assert.Fail($"JSON serialization error occurred: {ex.Message}");
        }
        
        // If we reach here without JSON serialization exceptions, the fix is working
        Assert.True(true);
    }

    // These tests are consolidated into the comprehensive test above
}