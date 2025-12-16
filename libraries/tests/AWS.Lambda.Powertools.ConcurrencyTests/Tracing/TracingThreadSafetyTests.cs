using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common;
using Xunit;
using PowertoolsTracing = AWS.Lambda.Powertools.Tracing.Tracing;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Tracing;

/// <summary>
/// Tests for validating thread safety in Powertools Tracing
/// under concurrent execution scenarios.
/// 
/// These tests verify that when multiple Lambda invocations run concurrently,
/// the tracing operations are thread-safe and don't cause exceptions or data corruption.
/// </summary>
[Collection("Tracing Concurrency Tests")]
public class TracingThreadSafetyTests : IDisposable
{
    public TracingThreadSafetyTests()
    {
        ResetTracingState();
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "/var/task");
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "TestService");
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "false");
    }

    public void Dispose()
    {
        ResetTracingState();
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", null);
    }

    private static void ResetTracingState()
    {
        try
        {
            AWSXRayRecorder.Instance.TraceContext.ClearEntity();
        }
        catch
        {
            // Ignore if no entity exists
        }
    }


    #region Helper Result Classes

    private class ThreadSafetyResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public int OperationsAttempted { get; set; }
        public int OperationsCompleted { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
    }

    private class StressTestResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public int SubsegmentsCreated { get; set; }
        public int AnnotationsAdded { get; set; }
        public int MetadataAdded { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    #endregion

    #region Property 1: Concurrent Operations Thread Safety

    /// <summary>
    /// Verifies that concurrent tracing operations don't throw exceptions.
    /// </summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(5, 20)]
    [InlineData(10, 10)]
    public async Task ConcurrentOperations_SimultaneousTracingCalls_ShouldNotThrowException(
        int concurrencyLevel, int operationsPerInvocation)
    {
        var results = new ConcurrentBag<ThreadSafetyResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new ThreadSafetyResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                OperationsAttempted = operationsPerInvocation
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"TestLambda_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                for (int op = 0; op < operationsPerInvocation; op++)
                {
                    // Mix of operations
                    AWSXRayRecorder.Instance.BeginSubsegment($"Op_{invocationIndex}_{op}");
                    
                    PowertoolsTracing.AddAnnotation($"key_{op}", $"value_{invocationId}_{op}");
                    PowertoolsTracing.AddMetadata($"meta_{op}", new { Id = invocationId, Op = op });
                    
                    AWSXRayRecorder.Instance.EndSubsegment();
                    result.OperationsCompleted++;
                }

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = ex.Message;
                result.ExceptionType = ex.GetType().Name;
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, 
            $"Invocation {r.InvocationIndex} threw {r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    #endregion

    #region Property 2: Nested Subsegments Thread Safety

    /// <summary>
    /// Verifies that concurrent invocations with nested subsegments don't interfere.
    /// </summary>
    [Theory]
    [InlineData(2, 3)]
    [InlineData(5, 2)]
    [InlineData(10, 2)]
    public async Task NestedSubsegments_ConcurrentInvocations_ShouldNotInterfere(
        int concurrencyLevel, int nestingDepth)
    {
        var results = new ConcurrentBag<ThreadSafetyResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new ThreadSafetyResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                OperationsAttempted = nestingDepth
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"TestLambda_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                // Create nested subsegments
                for (int depth = 0; depth < nestingDepth; depth++)
                {
                    AWSXRayRecorder.Instance.BeginSubsegment($"Nested_{invocationIndex}_{depth}");
                    PowertoolsTracing.AddAnnotation($"depth_{depth}", depth);
                    result.OperationsCompleted++;
                }

                // End all nested subsegments
                for (int depth = nestingDepth - 1; depth >= 0; depth--)
                {
                    AWSXRayRecorder.Instance.EndSubsegment();
                }

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                result.ExceptionType = ex.GetType().Name;
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    #endregion


    #region Property 3: Stress Test - All Operations

    /// <summary>
    /// Stress test: Multiple threads simultaneously performing all Tracing operations.
    /// This validates that the implementation handles high concurrency.
    /// </summary>
    [Theory]
    [InlineData(3, 50)]
    [InlineData(5, 100)]
    [InlineData(10, 50)]
    public async Task StressTest_MultipleThreadsAllOperations_ShouldNotThrowException(
        int threadCount, int operationsPerThread)
    {
        var results = new ConcurrentBag<StressTestResult>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new StressTestResult
            {
                InvocationId = invocationId,
                InvocationIndex = threadIndex
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"StressTest_{threadIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                for (int i = 0; i < operationsPerThread; i++)
                {
                    // Mix of all operations
                    var subsegmentName = $"stress_{threadIndex}_{i}";

                    // BeginSubsegment
                    AWSXRayRecorder.Instance.BeginSubsegment(subsegmentName);
                    result.SubsegmentsCreated++;

                    // AddAnnotation
                    PowertoolsTracing.AddAnnotation($"thread_{threadIndex}_op_{i}", i);
                    result.AnnotationsAdded++;

                    // AddMetadata
                    PowertoolsTracing.AddMetadata($"meta_{i}", new { Thread = threadIndex, Op = i });
                    result.MetadataAdded++;

                    // EndSubsegment
                    AWSXRayRecorder.Instance.EndSubsegment();

                    // Occasionally use WithSubsegment
                    if (i % 5 == 0)
                    {
                        PowertoolsTracing.WithSubsegment($"with_{threadIndex}_{i}", subsegment =>
                        {
                            subsegment.AddAnnotation("nested", true);
                        });
                    }
                }

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                exceptions.Add(ex);
            }

            results.Add(result);
        })).ToList();

        await Task.WhenAll(tasks);

        // All threads should complete without exceptions
        Assert.Equal(threadCount, results.Count);
        Assert.Empty(exceptions);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(operationsPerThread, r.SubsegmentsCreated));
    }

    #endregion

    #region Property 4: Rapid Subsegment Creation/Destruction

    /// <summary>
    /// Verifies that rapid creation and destruction of subsegments is thread-safe.
    /// </summary>
    [Theory]
    [InlineData(2, 100)]
    [InlineData(5, 50)]
    [InlineData(10, 30)]
    public async Task RapidSubsegmentLifecycle_ConcurrentInvocations_ShouldNotThrowException(
        int concurrencyLevel, int cyclesPerInvocation)
    {
        var results = new ConcurrentBag<ThreadSafetyResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new ThreadSafetyResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                OperationsAttempted = cyclesPerInvocation
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"RapidTest_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                // Rapid create/destroy cycles
                for (int cycle = 0; cycle < cyclesPerInvocation; cycle++)
                {
                    AWSXRayRecorder.Instance.BeginSubsegment($"rapid_{invocationIndex}_{cycle}");
                    // Minimal work
                    PowertoolsTracing.AddAnnotation("cycle", cycle);
                    AWSXRayRecorder.Instance.EndSubsegment();
                    result.OperationsCompleted++;
                }

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                result.ExceptionType = ex.GetType().Name;
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    #endregion


    #region Property 5: Exception Handling Thread Safety

    /// <summary>
    /// Verifies that concurrent invocations handling exceptions don't interfere.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ExceptionHandling_ConcurrentInvocations_ShouldNotInterfere(int concurrencyLevel)
    {
        var results = new ConcurrentBag<ThreadSafetyResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new ThreadSafetyResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                OperationsAttempted = 1
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"ExceptionTest_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                AWSXRayRecorder.Instance.BeginSubsegment($"exception_{invocationIndex}");

                // Add an exception to the trace
                var testException = new InvalidOperationException($"Test exception from invocation {invocationIndex}");
                PowertoolsTracing.AddException(testException);
                result.OperationsCompleted++;

                AWSXRayRecorder.Instance.EndSubsegment();
                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                result.ExceptionType = ex.GetType().Name;
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without unexpected exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    #endregion

    #region Property 6: GetEntity/SetEntity Thread Safety

    /// <summary>
    /// Verifies that GetEntity and SetEntity operations are thread-safe.
    /// </summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(5, 20)]
    [InlineData(10, 10)]
    public async Task GetSetEntity_ConcurrentInvocations_ShouldMaintainIsolation(
        int concurrencyLevel, int operationsPerInvocation)
    {
        var results = new ConcurrentBag<ThreadSafetyResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new ThreadSafetyResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                OperationsAttempted = operationsPerInvocation
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"EntityTest_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                for (int op = 0; op < operationsPerInvocation; op++)
                {
                    // Get current entity
                    var entity = PowertoolsTracing.GetEntity();
                    
                    // Verify we got an entity (should be our root segment or a subsegment)
                    Assert.NotNull(entity);

                    // Create a subsegment
                    AWSXRayRecorder.Instance.BeginSubsegment($"entity_op_{invocationIndex}_{op}");
                    
                    // Get entity again - should now be the subsegment
                    var subsegmentEntity = PowertoolsTracing.GetEntity();
                    Assert.NotNull(subsegmentEntity);

                    AWSXRayRecorder.Instance.EndSubsegment();
                    result.OperationsCompleted++;
                }

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                result.ExceptionType = ex.GetType().Name;
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    #endregion
}
