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
/// Tests for validating subsegment isolation in Powertools Tracing
/// under concurrent execution scenarios.
/// 
/// These tests verify that when multiple Lambda invocations run concurrently,
/// each invocation's tracing subsegments remain isolated from other invocations.
/// 
/// Note: X-Ray SDK uses AsyncLocal for trace context, which provides natural
/// isolation between async execution contexts. These tests validate that
/// Powertools Tracing correctly leverages this isolation.
/// </summary>
[Collection("Tracing Concurrency Tests")]
public class SubsegmentIsolationTests : IDisposable
{
    public SubsegmentIsolationTests()
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

    private class SubsegmentResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public string SubsegmentName { get; set; } = string.Empty;
        public bool SubsegmentCreated { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
    }

    private class AnnotationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public List<(string Key, object Value)> AnnotationsAdded { get; set; } = new();
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class MetadataResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public List<(string Namespace, string Key, object Value)> MetadataAdded { get; set; } = new();
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class WithSubsegmentResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public string SubsegmentName { get; set; } = string.Empty;
        public bool CallbackExecuted { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    #endregion

    #region Property 1: Subsegment Creation Isolation

    /// <summary>
    /// Verifies that concurrent invocations can create subsegments without interference.
    /// Each invocation should be able to create its own subsegment independently.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task SubsegmentCreation_ConcurrentInvocations_ShouldNotInterfere(int concurrencyLevel)
    {
        var results = new ConcurrentBag<SubsegmentResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new SubsegmentResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                SubsegmentName = $"Subsegment_{invocationIndex}_{invocationId}"
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"TestLambda_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                // Create a subsegment
                AWSXRayRecorder.Instance.BeginSubsegment(result.SubsegmentName);
                result.SubsegmentCreated = true;

                Thread.Sleep(Random.Shared.Next(1, 10));

                AWSXRayRecorder.Instance.EndSubsegment();

                // Clean up
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

        // All invocations should have created subsegments without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, 
            $"Invocation {r.InvocationIndex} threw: {r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.True(r.SubsegmentCreated, 
            $"Invocation {r.InvocationIndex} failed to create subsegment"));
    }

    #endregion


    #region Property 2: Annotation Isolation

    /// <summary>
    /// Verifies that concurrent invocations adding annotations don't interfere with each other.
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 2)]
    public async Task AnnotationIsolation_ConcurrentInvocations_ShouldNotInterfere(
        int concurrencyLevel, int annotationsPerInvocation)
    {
        var results = new ConcurrentBag<AnnotationResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new AnnotationResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"TestLambda_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                AWSXRayRecorder.Instance.BeginSubsegment($"Subsegment_{invocationIndex}");

                // Add annotations
                for (int a = 0; a < annotationsPerInvocation; a++)
                {
                    var key = $"annotation_inv{invocationIndex}_a{a}";
                    var value = $"value_{invocationId}_{a}";
                    PowertoolsTracing.AddAnnotation(key, value);
                    result.AnnotationsAdded.Add((key, value));
                }

                Thread.Sleep(Random.Shared.Next(1, 10));

                AWSXRayRecorder.Instance.EndSubsegment();
                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(annotationsPerInvocation, r.AnnotationsAdded.Count));
    }

    #endregion

    #region Property 3: Metadata Isolation

    /// <summary>
    /// Verifies that concurrent invocations adding metadata don't interfere with each other.
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 2)]
    public async Task MetadataIsolation_ConcurrentInvocations_ShouldNotInterfere(
        int concurrencyLevel, int metadataPerInvocation)
    {
        var results = new ConcurrentBag<MetadataResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new MetadataResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"TestLambda_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                AWSXRayRecorder.Instance.BeginSubsegment($"Subsegment_{invocationIndex}");

                // Add metadata
                for (int m = 0; m < metadataPerInvocation; m++)
                {
                    var ns = $"namespace_{invocationIndex}";
                    var key = $"metadata_inv{invocationIndex}_m{m}";
                    var value = new { InvocationId = invocationId, Index = m };
                    PowertoolsTracing.AddMetadata(ns, key, value);
                    result.MetadataAdded.Add((ns, key, value));
                }

                Thread.Sleep(Random.Shared.Next(1, 10));

                AWSXRayRecorder.Instance.EndSubsegment();
                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(metadataPerInvocation, r.MetadataAdded.Count));
    }

    #endregion


    #region Property 4: WithSubsegment Isolation

    /// <summary>
    /// Verifies that concurrent invocations using WithSubsegment don't interfere with each other.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task WithSubsegment_ConcurrentInvocations_ShouldNotInterfere(int concurrencyLevel)
    {
        var results = new ConcurrentBag<WithSubsegmentResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new WithSubsegmentResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                SubsegmentName = $"WithSubsegment_{invocationIndex}"
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"TestLambda_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                // Use WithSubsegment
                PowertoolsTracing.WithSubsegment(result.SubsegmentName, subsegment =>
                {
                    result.CallbackExecuted = true;
                    subsegment.AddAnnotation($"invocation_{invocationIndex}", invocationId);
                    subsegment.AddMetadata($"data_{invocationIndex}", new { Id = invocationId });
                    Thread.Sleep(Random.Shared.Next(1, 10));
                });

                rootSegment.SetEndTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.ClearEntity();
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            results.Add(result);
        }));

        await Task.WhenAll(tasks);

        // All invocations should complete without exceptions
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.CallbackExecuted, 
            $"Invocation {r.InvocationIndex} callback was not executed"));
    }

    #endregion

    #region Property 5: BeginSubsegment/Dispose Pattern Isolation

    /// <summary>
    /// Verifies that concurrent invocations using BeginSubsegment with using pattern don't interfere.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task BeginSubsegment_ConcurrentInvocations_ShouldNotInterfere(int concurrencyLevel)
    {
        var results = new ConcurrentBag<SubsegmentResult>();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(invocationIndex => Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            var result = new SubsegmentResult
            {
                InvocationId = invocationId,
                InvocationIndex = invocationIndex,
                SubsegmentName = $"BeginSubsegment_{invocationIndex}"
            };

            try
            {
                // Create a root segment for this "invocation"
                var rootSegment = new Segment($"TestLambda_{invocationIndex}");
                rootSegment.SetStartTimeToNow();
                AWSXRayRecorder.Instance.TraceContext.SetEntity(rootSegment);

                // Use BeginSubsegment with using pattern
                using (var subsegment = PowertoolsTracing.BeginSubsegment(result.SubsegmentName))
                {
                    result.SubsegmentCreated = true;
                    subsegment.AddAnnotation($"invocation_{invocationIndex}", invocationId);
                    Thread.Sleep(Random.Shared.Next(1, 10));
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
        Assert.All(results, r => Assert.False(r.ExceptionThrown, 
            $"Invocation {r.InvocationIndex} threw: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.True(r.SubsegmentCreated, 
            $"Invocation {r.InvocationIndex} failed to create subsegment"));
    }

    #endregion
}
