using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Metrics.Tests.Handlers;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

/// <summary>
/// Metrics must belong to the invocation's async flow, not to the thread it happens to run on
/// (issue #1368). Every test forces the thread switch it depends on, so none rely on luck.
/// </summary>
[Collection("Sequential")]
public class AsyncContextTests : IDisposable
{
    private readonly CustomConsoleWriter _consoleOut;

    public AsyncContextTests()
    {
        Metrics.ResetForTest();
        MetricsAspect.ResetForTest();
        _consoleOut = new CustomConsoleWriter();
        ConsoleWrapper.SetOut(_consoleOut);
    }

    public void Dispose()
    {
        Metrics.ResetForTest();
        MetricsAspect.ResetForTest();
        ConsoleWrapper.ResetForTest();
    }

    [Fact]
    public async Task MetricAddedBeforeAwait_IsFlushed_WhenHandlerResumesOnAnotherThread()
    {
        var handler = new AsyncContextFunctionHandler();

        // Run like the Lambda runtime does: on the thread pool, no synchronization context
        await Task.Run(() => handler.AddMetricsAcrossThreadSwitch());

        Assert.NotEqual(handler.ThreadBeforeAwait, handler.ThreadAfterAwait);
        var blob = Assert.Single(EmfBlobs());
        Assert.Equal(new[] { "AfterAwait", "BeforeAwait" }, MetricNames(blob));
    }

    [Fact]
    public async Task MetricsFromChildTasks_AreAllFlushed_IncludingTheSameKeyFromManyTasks()
    {
        var handler = new AsyncContextFunctionHandler();

        await Task.Run(() => handler.AddMetricsFromChildTasks(sameKeyTasks: 100, distinctKeyTasks: 20));

        var blob = Assert.Single(EmfBlobs());
        var expectedNames = Enumerable.Range(0, 20).Select(i => $"Distinct{i:D2}")
            .Append("FromThread").Append("SameKey")
            .OrderBy(n => n, StringComparer.Ordinal);
        Assert.Equal(expectedNames, MetricNames(blob));
        Assert.Equal(100, blob.GetProperty("SameKey").GetArrayLength());
    }

    [Fact]
    public async Task ConcurrentChildTasks_OverflowingMaxMetrics_FlushEveryMetricExactlyOnce()
    {
        var handler = new AsyncContextFunctionHandler();

        // 400 distinct metrics from 8 tasks at once: overflow flushes happen while other tasks add
        await Task.Run(() => handler.AddManyDistinctMetricsConcurrently(tasks: 8, metricsPerTask: 50));

        var blobs = EmfBlobs();
        Assert.All(blobs, b => Assert.InRange(MetricNames(b).Length, 1, PowertoolsConfigurations.MaxMetrics));
        var flushed = blobs.SelectMany(MetricNames).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var expected = Enumerable.Range(0, 8)
            .SelectMany(t => Enumerable.Range(0, 50).Select(i => $"T{t:D2}_M{i:D3}"))
            .OrderBy(n => n, StringComparer.Ordinal);
        Assert.Equal(expected, flushed);
    }

    [Fact]
    public async Task ConcurrentInvocations_OnTheSameThread_DoNotShareMetricsDimensionsOrMetadata()
    {
        var handler = new AsyncContextFunctionHandler();
        using var worker = new DedicatedThread();
        var aAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // A adds, B adds, A flushes, B flushes; every add and flush runs on the same thread
        var a = Task.Run(() => handler.ConcurrentInvocation("A", worker, Task.CompletedTask, aAdded, bAdded.Task));
        var b = Task.Run(() => handler.ConcurrentInvocation("B", worker, aAdded.Task, bAdded, a));
        await Task.WhenAll(a, b);

        AssertOnlyOwnData("A", "B");
        AssertOnlyOwnData("B", "A");
    }

    [Fact]
    public async Task ConcurrentInvocations_WithoutAttribute_DoNotShareMetricsDimensionsOrMetadata()
    {
        // Concurrent invocations in one execution environment only happen in multi-concurrency mode
        using var _ = new MaxConcurrency("4");
        var handler = new AsyncContextFunctionHandler();
        using var worker = new DedicatedThread();
        var aAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // Configuration at startup must not create a context every invocation would inherit
        Metrics.SetNamespace("ns");

        var a = Task.Run(() => handler.ManualConcurrentInvocation("A", worker, Task.CompletedTask, aAdded, bAdded.Task));
        var b = Task.Run(() => handler.ManualConcurrentInvocation("B", worker, aAdded.Task, bAdded, a));
        await Task.WhenAll(a, b);

        AssertOnlyOwnData("A", "B");
        AssertOnlyOwnData("B", "A");
    }

    [Fact]
    public async Task NextInvocation_OnTheThreadAPreviousOneUsed_DoesNotFlushThePreviousInvocationsMetrics()
    {
        var handler = new AsyncContextFunctionHandler();
        using var worker = new DedicatedThread();

        // A adds on the worker but finishes elsewhere; B then runs entirely on the worker
        await Task.Run(() => handler.AddOnThreadThenMoveOn("FromA", worker));
        await Task.Run(() => handler.AddOnThread("FromB", worker));

        var blobs = EmfBlobs();
        Assert.Equal(2, blobs.Count);
        Assert.Equal(new[] { "FromA" }, MetricNames(blobs[0]));
        Assert.Equal(new[] { "FromB" }, MetricNames(blobs[1]));
    }

    [Fact]
    public async Task NextInvocation_AfterAnAsyncInvocationThrew_DoesNotFlushTheFailedInvocationsMetrics()
    {
        var handler = new AsyncContextFunctionHandler();
        using var worker = new DedicatedThread();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.Run(() => handler.AddOnThreadThenThrow("FromFailed", worker)));
        await Task.Run(() => handler.AddOnThread("FromB", worker));

        var blob = Assert.Single(EmfBlobs());
        Assert.Equal(new[] { "FromB" }, MetricNames(blob));
    }

    [Fact]
    public async Task NextInvocation_AfterASyncInvocationThrew_DoesNotFlushTheFailedInvocationsMetrics()
    {
        var handler = new AsyncContextFunctionHandler();
        using var worker = new DedicatedThread();

        await Task.Run(async () =>
        {
            await worker.SwitchTo();
            Assert.Throws<InvalidOperationException>(() => handler.AddThenThrow("FromFailed"));
        });
        await Task.Run(() => handler.AddOnThread("FromB", worker));

        var blob = Assert.Single(EmfBlobs());
        Assert.Equal(new[] { "FromB" }, MetricNames(blob));
    }

    [Fact]
    public async Task NestedAsyncMetricsMethod_FlushesItsOwnMetrics_AndTheOuterInvocationKeepsItsMetrics()
    {
        var handler = new AsyncContextFunctionHandler();

        await Task.Run(() => handler.OuterWithNestedAsync());

        var blobs = EmfBlobs();
        Assert.Equal(2, blobs.Count);
        Assert.Equal(new[] { "Inner" }, MetricNames(blobs[0]));
        Assert.Equal(new[] { "OuterAfter", "OuterBefore" }, MetricNames(blobs[1]));
    }

    [Fact]
    public async Task ManualFlushAfterAwait_IncludesMetricsAddedBeforeTheAwait()
    {
        Metrics.SetNamespace("ns");

        await Task.Run(async () =>
        {
            Metrics.AddMetric("BeforeAwait", 1, MetricUnit.Count);
            await ThreadHop.ToNewThread();
            Metrics.AddMetric("AfterAwait", 1, MetricUnit.Count);
            Metrics.Flush();
        });

        var blob = Assert.Single(EmfBlobs());
        Assert.Equal(new[] { "AfterAwait", "BeforeAwait" }, MetricNames(blob));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("not-a-number")]
    public async Task ManualFlush_WithSingleConcurrency_SeesMetricFirstAddedInAwaitedHelperOnAnotherThread(string maxConcurrency)
    {
        using var _ = new MaxConcurrency(maxConcurrency);
        Metrics.SetNamespace("ns");

        await Task.Run(async () =>
        {
            await AddFirstMetricInHelper("FromHelper");
            Metrics.Flush();
        });

        var blob = Assert.Single(EmfBlobs());
        Assert.Equal(new[] { "FromHelper" }, MetricNames(blob));
    }

    [Fact]
    public async Task ManualFlush_WithSingleConcurrency_GetsDefaultDimensionUpdates_AndFlushClearsIt()
    {
        using var _ = new MaxConcurrency(null);
        Metrics.SetNamespace("ns");

        await Task.Run(async () =>
        {
            await AddFirstMetricInHelper("FromHelper");
            Metrics.SetDefaultDimensions(new Dictionary<string, string> { { "Environment", "prod" } });
            Metrics.Flush();
            // Nothing left to publish after the flush
            Metrics.Flush();
        });

        var blob = Assert.Single(EmfBlobs());
        Assert.Equal(new[] { "FromHelper" }, MetricNames(blob));
        Assert.Equal("prod", blob.GetProperty("Environment").GetString());
        Assert.Contains(_consoleOut.OutputValues, line => line.Contains("No application metrics to publish"));
    }

    [Fact]
    public async Task ManualFlush_WithSingleConcurrency_FlushesEveryMetricFromConcurrentChildTasksExactlyOnce()
    {
        using var _ = new MaxConcurrency(null);
        Metrics.SetNamespace("ns");

        await Task.Run(async () =>
        {
            // No [Metrics] attribute: the first metrics call happens inside the child tasks
            await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < 50; i++)
                    Metrics.AddMetric($"T{t:D2}_M{i:D3}", 1, MetricUnit.Count);
            })));
            await ThreadHop.ToNewThread();
            Metrics.Flush();
        });

        var flushed = EmfBlobs().SelectMany(MetricNames).OrderBy(n => n, StringComparer.Ordinal);
        var expected = Enumerable.Range(0, 8)
            .SelectMany(t => Enumerable.Range(0, 50).Select(i => $"T{t:D2}_M{i:D3}"))
            .OrderBy(n => n, StringComparer.Ordinal);
        Assert.Equal(expected, flushed);
    }

    [Fact]
    public async Task ManualFlush_WithMultiConcurrency_DoesNotSeeMetricFirstAddedInAwaitedHelper()
    {
        // Documented caveat: with AWS_LAMBDA_MAX_CONCURRENCY > 1 each flow gets its own context,
        // and one first created inside an awaited async method isn't visible to its caller
        using var _ = new MaxConcurrency("4");
        Metrics.SetNamespace("ns");

        await Task.Run(async () =>
        {
            await AddFirstMetricInHelper("FromHelper");
            Metrics.Flush();
        });

        Assert.Empty(EmfBlobs());
    }

    [Fact]
    public async Task MetricsInvocation_WithSingleConcurrency_GetsItsOwnContext_NotTheSharedOne()
    {
        using var _ = new MaxConcurrency(null);
        var handler = new AsyncContextFunctionHandler();
        using var worker = new DedicatedThread();
        Metrics.SetNamespace("ns");

        // Manual code puts a metric in the shared context and leaves it there
        await Task.Run(() => AddFirstMetricInHelper("Manual"));
        await Task.Run(() => handler.AddOnThread("FromInvocation", worker));

        var blob = Assert.Single(EmfBlobs());
        Assert.Equal(new[] { "FromInvocation" }, MetricNames(blob));
    }

    private static async Task AddFirstMetricInHelper(string name)
    {
        await ThreadHop.ToNewThread();
        Metrics.AddMetric(name, 1, MetricUnit.Count);
    }

    /// <summary>
    /// Sets AWS_LAMBDA_MAX_CONCURRENCY for the test and restores the previous value on dispose.
    /// </summary>
    private sealed class MaxConcurrency : IDisposable
    {
        private const string Name = "AWS_LAMBDA_MAX_CONCURRENCY";
        private readonly string _previous = Environment.GetEnvironmentVariable(Name);

        public MaxConcurrency(string value) => Environment.SetEnvironmentVariable(Name, value);

        public void Dispose() => Environment.SetEnvironmentVariable(Name, _previous);
    }

    private void AssertOnlyOwnData(string own, string other)
    {
        var blob = Assert.Single(EmfBlobs(), b => b.TryGetProperty($"{own}_metric", out _));
        Assert.Equal(new[] { $"{own}_metric" }, MetricNames(blob));
        Assert.Equal(own, blob.GetProperty($"{own}_dim").GetString());
        Assert.Equal(own, blob.GetProperty($"{own}_meta").GetString());
        Assert.False(blob.TryGetProperty($"{other}_dim", out _), $"{own}'s output has {other}'s dimension");
        Assert.False(blob.TryGetProperty($"{other}_meta", out _), $"{own}'s output has {other}'s metadata");
    }

    private List<JsonElement> EmfBlobs() =>
        _consoleOut.OutputValues
            .Where(line => line != null && line.TrimStart().StartsWith('{') && line.Contains("\"_aws\""))
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())
            .ToList();

    private static string[] MetricNames(JsonElement blob) =>
        blob.GetProperty("_aws").GetProperty("CloudWatchMetrics")[0].GetProperty("Metrics")
            .EnumerateArray()
            .Select(m => m.GetProperty("Name").GetString())
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
}
