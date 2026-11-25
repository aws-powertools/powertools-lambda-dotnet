using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

/// <summary>
/// Handlers for the async-flow tests (issue #1368). Each records the threads it ran on so the
/// tests can prove a thread switch actually happened.
/// </summary>
internal class AsyncContextFunctionHandler
{
    public int ThreadBeforeAwait { get; private set; }
    public int ThreadAfterAwait { get; private set; }

    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task AddMetricsAcrossThreadSwitch()
    {
        ThreadBeforeAwait = Environment.CurrentManagedThreadId;
        Metrics.AddMetric("BeforeAwait", 1, MetricUnit.Count);

        await ThreadHop.ToNewThread();

        ThreadAfterAwait = Environment.CurrentManagedThreadId;
        Metrics.AddMetric("AfterAwait", 1, MetricUnit.Count);
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task AddMetricsFromChildTasks(int sameKeyTasks, int distinctKeyTasks)
    {
        var tasks = new List<Task>();
        for (var i = 0; i < sameKeyTasks; i++)
            tasks.Add(Task.Run(() => Metrics.AddMetric("SameKey", 1, MetricUnit.Count)));
        for (var i = 0; i < distinctKeyTasks; i++)
        {
            var key = $"Distinct{i:D2}";
            tasks.Add(Task.Run(() => Metrics.AddMetric(key, 1, MetricUnit.Count)));
        }
        // Also from a dedicated thread, which no other code ever runs on
        var thread = new Thread(() => Metrics.AddMetric("FromThread", 1, MetricUnit.Count));
        thread.Start();
        tasks.Add(Task.Run(thread.Join));

        await Task.WhenAll(tasks);

        // The flush runs on a thread none of the children used
        await ThreadHop.ToNewThread();
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task AddManyDistinctMetricsConcurrently(int tasks, int metricsPerTask)
    {
        await Task.WhenAll(Enumerable.Range(0, tasks).Select(t => Task.Run(() =>
        {
            for (var i = 0; i < metricsPerTask; i++)
                Metrics.AddMetric($"T{t:D2}_M{i:D3}", 1, MetricUnit.Count);
        })));

        await ThreadHop.ToNewThread();
    }

    /// <summary>
    /// One of two concurrent invocations. Adding and flushing both happen on <paramref name="worker"/>,
    /// so the two invocations share a thread the whole time.
    /// </summary>
    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task ConcurrentInvocation(string name, DedicatedThread worker, Task addAfter,
        TaskCompletionSource added, Task flushAfter)
    {
        await addAfter;
        await worker.SwitchTo();
        Metrics.AddMetric($"{name}_metric", 1, MetricUnit.Count);
        Metrics.AddDimension($"{name}_dim", name);
        Metrics.AddMetadata($"{name}_meta", name);
        added.SetResult();

        await flushAfter;
        await worker.SwitchTo();
    }

    /// <summary>
    /// Same as <see cref="ConcurrentInvocation"/> without the attribute: flushes manually.
    /// </summary>
    public async Task ManualConcurrentInvocation(string name, DedicatedThread worker, Task addAfter,
        TaskCompletionSource added, Task flushAfter)
    {
        await addAfter;
        await worker.SwitchTo();
        Metrics.AddMetric($"{name}_metric", 1, MetricUnit.Count);
        Metrics.AddDimension($"{name}_dim", name);
        Metrics.AddMetadata($"{name}_meta", name);
        added.SetResult();

        await flushAfter;
        await worker.SwitchTo();
        Metrics.Flush();
    }

    /// <summary>
    /// Adds a metric on <paramref name="worker"/>, then finishes (and flushes) on a different thread.
    /// </summary>
    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task AddOnThreadThenMoveOn(string key, DedicatedThread worker)
    {
        await worker.SwitchTo();
        Metrics.AddMetric(key, 1, MetricUnit.Count);
        await ThreadHop.ToNewThread();
    }

    /// <summary>
    /// Adds a metric on <paramref name="worker"/>, then fails, so the invocation never flushes.
    /// </summary>
    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task AddOnThreadThenThrow(string key, DedicatedThread worker)
    {
        await worker.SwitchTo();
        Metrics.AddMetric(key, 1, MetricUnit.Count);
        // Conditional so the method keeps a normal completion path, which AspectInjector needs
        if (key.Length > 0)
            throw new InvalidOperationException("invocation failed");
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public void AddThenThrow(string key)
    {
        Metrics.AddMetric(key, 1, MetricUnit.Count);
        throw new InvalidOperationException("invocation failed");
    }

    /// <summary>
    /// Adds a metric and flushes, all on <paramref name="worker"/>.
    /// </summary>
    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task AddOnThread(string key, DedicatedThread worker)
    {
        await worker.SwitchTo();
        Metrics.AddMetric(key, 1, MetricUnit.Count);
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task OuterWithNestedAsync()
    {
        Metrics.AddMetric("OuterBefore", 1, MetricUnit.Count);
        await InnerAsync();
        Metrics.AddMetric("OuterAfter", 1, MetricUnit.Count);
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    private async Task InnerAsync()
    {
        Metrics.AddMetric("Inner", 1, MetricUnit.Count);
        await ThreadHop.ToNewThread();
    }
}
