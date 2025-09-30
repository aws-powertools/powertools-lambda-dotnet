using System;
using Amazon.XRay.Recorder.Core;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Core;
using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

/// <summary>
/// Collection fixture to ensure proper isolation between test classes
/// </summary>
public class TracingTestCollectionFixture : IDisposable
{
    public TracingTestCollectionFixture()
    {
        // Clean slate for each test collection
        CleanupEnvironment();
    }

    public void Dispose()
    {
        CleanupEnvironment();
    }

    private static void CleanupEnvironment()
    {
        // Clear all tracing-related environment variables
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
        
        // Reset PowertoolsConfigurations singleton
        ResetPowertoolsConfigurations();
        
        // Reset XRayRecorder instance
        XRayRecorder.ResetInstance();
        
        // Clear any existing X-Ray context
        try
        {
            AWSXRayRecorder.Instance.TraceContext.ClearEntity();
        }
        catch
        {
            // Ignore if no entity exists
        }
        
        // Reset lifecycle tracker
        LambdaLifecycleTracker.Reset();
    }

    private static void ResetPowertoolsConfigurations()
    {
        // Use reflection to reset the singleton instance
        var field = typeof(PowertoolsConfigurations).GetField("_instance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field?.SetValue(null, null);
    }
}

/// <summary>
/// Collection definition for tracing tests
/// </summary>
[CollectionDefinition("TracingTests")]
public class TracingTestCollection : ICollectionFixture<TracingTestCollectionFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}