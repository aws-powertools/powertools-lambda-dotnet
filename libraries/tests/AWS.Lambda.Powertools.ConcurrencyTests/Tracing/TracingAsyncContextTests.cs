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
/// Tests for validating async context behavior in Powertools Tracing
/// under concurrent execution scenarios.
/// 
/// These tests verify that the X-Ray SDK's AsyncLocal-based trace context
/// correctly maintains isolation between async execution contexts,
/// which is essential for Lambda's multi-instance mode.
/// </summary>
[Collection("Tracing Concurrency Tests")]
public class TracingAsyncContextTests : IDisposable
{
    public TracingAsyncContextTests()
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

    private class AsyncContextResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public string ExpectedSegmentName { get; set; } = string.Empty;
        public string? ActualSegmentName { get; set; }
        public bool ContextPreserved { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    #endregion

    #region Property 1: Async Context Preservation

    /// <summary>
    /// Verifies that async context is preserved across await points.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task AsyncContextPreservation_AcrossAwaitPoints_ShouldMaintainContext(int concurrencyLevel)
    {
        var results = new ConcurrentBag<AsyncContextResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(async invocationIndex =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new AsyncContextResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                ExpectedSegmentName = $"AsyncTest_{invocationIndex}"
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment(result.ExpectedSegmentName);
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                // First await point
                await Task.Delay(Random.Shared.Next(1, 10));

                // Verify context is preserved
                var entity1 = PowertoolsTracing.GetEntity();
                if (entity1 == null)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = "Context lost after first await";
                    results.Add(result);
                    return;
                }

                // Create a subsegment
                AWSXRayRecorder.Instance.BeginSubsegment($"async_sub_{invocationIndex}");

                // Second await point
                await Task.Delay(Random.Shared.Next(1, 10));

                // Verify we're still in the subsegment context
                var entity2 = PowertoolsTracing.GetEntity();
                if (entity2 == null)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = "Context lost after second await";
                    results.Add(result);
                    return;
                }

                AWSXRayRecorder.Instance.EndSubsegment();

                // Third await point
                await Task.Delay(Random.Shared.Next(1, 10));

                // Verify we're back to root segment context
                var entity3 = PowertoolsTracing.GetEntity();
                result.ActualSegmentName = entity3?.Name;
                result.ContextPreserved = entity3 != null;

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            results.Add(result);
        }).ToList();

        await Task.WhenAll(tasks);

        // All invocations should preserve context
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ContextPreserved, 
            $"Invocation {r.InvocationIndex} lost context"));
    }

    #endregion

    #region Property 2: ConfigureAwait(false) Behavior

    /// <summary>
    /// Verifies that ConfigureAwait(false) doesn't break trace context.
    /// X-Ray SDK uses AsyncLocal which flows across ConfigureAwait(false).
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConfigureAwaitFalse_ShouldPreserveTraceContext(int concurrencyLevel)
    {
        var results = new ConcurrentBag<AsyncContextResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(async invocationIndex =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new AsyncContextResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                ExpectedSegmentName = $"ConfigureAwaitTest_{invocationIndex}"
            };

            try
            {
                var rootSegment = new Segment(result.ExpectedSegmentName);
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                // Use ConfigureAwait(false)
                await Task.Delay(Random.Shared.Next(1, 10)).ConfigureAwait(false);

                // Verify context is preserved
                var entity1 = PowertoolsTracing.GetEntity();
                if (entity1 == null)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = "Context lost after ConfigureAwait(false)";
                    results.Add(result);
                    return;
                }

                AWSXRayRecorder.Instance.BeginSubsegment($"after_configureawait_{invocationIndex}");

                await Task.Delay(Random.Shared.Next(1, 10)).ConfigureAwait(false);

                var entity2 = PowertoolsTracing.GetEntity();
                if (entity2 == null)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = "Context lost after second ConfigureAwait(false)";
                    results.Add(result);
                    return;
                }

                AWSXRayRecorder.Instance.EndSubsegment();

                await Task.Delay(Random.Shared.Next(1, 10)).ConfigureAwait(false);

                var entity3 = PowertoolsTracing.GetEntity();
                result.ContextPreserved = entity3 != null;
                result.ActualSegmentName = entity3?.Name;

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            results.Add(result);
        }).ToList();

        await Task.WhenAll(tasks);

        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ContextPreserved,
            $"Invocation {r.InvocationIndex} lost context after ConfigureAwait(false)"));
    }

    #endregion

    #region Property 3: Task.Run Context Flow

    /// <summary>
    /// Verifies that trace context flows correctly into Task.Run.
    /// </summary>
    [Theory]
    [InlineData(2, 3)]
    [InlineData(5, 2)]
    [InlineData(10, 2)]
    public async Task TaskRun_ShouldFlowTraceContext(int concurrencyLevel, int taskRunsPerInvocation)
    {
        var results = new ConcurrentBag<AsyncContextResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(async invocationIndex =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new AsyncContextResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                ExpectedSegmentName = $"TaskRunTest_{invocationIndex}"
            };

            try
            {
                var rootSegment = new Segment(result.ExpectedSegmentName);
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                var contextPreservedInAllTasks = true;

                // Run multiple Task.Run operations
                var taskRunTasks = Enumerable.Range(0, taskRunsPerInvocation)
                    .Select(taskIndex => Task.Run(() =>
                    {
                        // Verify context flowed into Task.Run
                        var entity = PowertoolsTracing.GetEntity();
                        if (entity == null)
                        {
                            contextPreservedInAllTasks = false;
                        }
                        Thread.Sleep(Random.Shared.Next(1, 5));
                    }));

                await Task.WhenAll(taskRunTasks);

                result.ContextPreserved = contextPreservedInAllTasks;

                var finalEntity = PowertoolsTracing.GetEntity();
                result.ActualSegmentName = finalEntity?.Name;

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            results.Add(result);
        }).ToList();

        await Task.WhenAll(tasks);

        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ContextPreserved,
            $"Invocation {r.InvocationIndex} lost context in Task.Run"));
    }

    #endregion
}
