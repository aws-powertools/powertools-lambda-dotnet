using System;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Core;
using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

/// <summary>
/// Base class for tracing tests that need proper X-Ray segment setup
/// </summary>
[Collection("TracingTests")]
public abstract class TracingTestBase : IDisposable
{
    protected Segment _rootSegment;
    protected bool _segmentCreated;

    protected TracingTestBase()
    {
        // Ensure clean state before each test
        CleanupBeforeTest();
    }

    /// <summary>
    /// Cleans up environment before test execution
    /// </summary>
    private void CleanupBeforeTest()
    {
        try
        {
            // Clear any existing X-Ray context
            AWSXRayRecorder.Instance.TraceContext.ClearEntity();
        }
        catch
        {
            // Ignore if no entity exists
        }

        // Clear environment variables that might be left from previous tests
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);

        // Reset singletons
        ResetPowertoolsConfigurations();
        XRayRecorder.ResetInstance();
        LambdaLifecycleTracker.Reset();

        // Reset instance variables
        _rootSegment = null;
        _segmentCreated = false;
    }

    /// <summary>
    /// Sets up a root X-Ray segment to simulate Lambda environment
    /// </summary>
    protected virtual void SetupXRaySegment()
    {
        try
        {
            // Create a root segment to simulate Lambda environment
            _rootSegment = new Segment("TestLambdaFunction");
            _rootSegment.SetStartTimeToNow();
            
            // Set it as the current entity in X-Ray context
            AWSXRayRecorder.Instance.TraceContext.SetEntity(_rootSegment);
            _segmentCreated = true;
        }
        catch (Exception ex)
        {
            // If we can't create a segment, tests will run without X-Ray context
            Console.WriteLine($"Warning: Could not create X-Ray segment for test: {ex.Message}");
            _segmentCreated = false;
        }
    }

    /// <summary>
    /// Resets the PowertoolsConfigurations singleton to pick up new environment variables
    /// </summary>
    private static void ResetPowertoolsConfigurations()
    {
        // Use reflection to reset the singleton instance
        var field = typeof(PowertoolsConfigurations).GetField("_instance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field?.SetValue(null, null);
    }

    /// <summary>
    /// Sets up the Lambda environment and ensures PowertoolsConfigurations recognizes it
    /// Call this method at the beginning of each test after setting environment variables
    /// </summary>
    protected void SetupLambdaEnvironment()
    {
        // Reset PowertoolsConfigurations to pick up current environment variables
        ResetPowertoolsConfigurations();
        
        // Setup X-Ray segment
        SetupXRaySegment();
    }

    /// <summary>
    /// Gets the current segment, creating one if it doesn't exist
    /// </summary>
    protected Segment GetOrCreateSegment()
    {
        // Ensure Lambda environment is set up first
        if (Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT") != null && !_segmentCreated)
        {
            SetupLambdaEnvironment();
        }

        if (_rootSegment != null && _segmentCreated)
        {
            return _rootSegment;
        }

        try
        {
            var entity = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            if (entity is Segment segment)
            {
                return segment;
            }
            
            // If we get a subsegment, get its root segment
            if (entity is Subsegment subsegment)
            {
                return subsegment.RootSegment;
            }
        }
        catch
        {
            // Fall through to create new segment
        }

        // Create a new segment if none exists
        if (!_segmentCreated)
        {
            SetupXRaySegment();
        }
        return _rootSegment;
    }

    public virtual void Dispose()
    {
        try
        {
            if (_segmentCreated && _rootSegment != null)
            {
                // End the segment and clear the context
                _rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error disposing X-Ray segment: {ex.Message}");
        }
        finally
        {
            _rootSegment = null;
            _segmentCreated = false;
            
            // Clean up environment variables that might affect other tests
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", null);
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", null);
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", null);
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", null);
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
            
            // Reset PowertoolsConfigurations to pick up cleaned environment variables
            ResetPowertoolsConfigurations();
            
            // Reset the XRayRecorder instance to prevent test pollution
            XRayRecorder.ResetInstance();
        }
    }
}