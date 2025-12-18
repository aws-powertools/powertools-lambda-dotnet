using System.Threading;
using AWS.Lambda.Powertools.Metrics;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Metrics;

/// <summary>
/// Tests for validating metrics behavior in async/await and background task scenarios.
/// Collection attribute ensures tests run sequentially, so no additional locking needed.
/// </summary>
[Collection("Metrics Tests")]
public class MetricsAsyncContextTests : IDisposable
{
    public MetricsAsyncContextTests()
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE", "TestNamespace");
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "TestService");
    }

    public void Dispose()
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
    }

    private class AsyncContextResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public bool MainThreadMetricAdded { get; set; }
        public bool BackgroundTaskMetricAdded { get; set; }
        public bool PostAwaitMetricAdded { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionSource { get; set; }
    }


    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task BackgroundTaskMetrics_ShouldNotThrowException(int backgroundTaskCount)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new AsyncContextResult[backgroundTaskCount];
        var tasks = new Task[backgroundTaskCount];

        for (int i = 0; i < backgroundTaskCount; i++)
        {
            int taskIndex = i;
            var result = new AsyncContextResult { InvocationId = Guid.NewGuid().ToString("N") };
            results[taskIndex] = result;

            try
            {
                Powertools.Metrics.Metrics.AddMetric($"main_metric_{taskIndex}", 1, MetricUnit.Count);
                result.MainThreadMetricAdded = true;
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = ex.Message;
                result.ExceptionSource = "MainThread";
            }

            tasks[taskIndex] = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Random.Shared.Next(10, 50));
                    Powertools.Metrics.Metrics.AddMetric($"background_metric_{taskIndex}", 1, MetricUnit.Count);
                    Powertools.Metrics.Metrics.AddDimension($"background_dim_{taskIndex}", "value");
                    result.BackgroundTaskMetricAdded = true;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = ex.Message;
                    result.ExceptionSource = "BackgroundTask";
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, $"{r.ExceptionSource}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.True(r.MainThreadMetricAdded));
        Assert.All(results, r => Assert.True(r.BackgroundTaskMetricAdded));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task AsyncAwaitMetrics_ShouldNotThrowException(int invocationCount)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new AsyncContextResult[invocationCount];
        var tasks = new Task[invocationCount];

        for (int i = 0; i < invocationCount; i++)
        {
            int invocationIndex = i;
            var result = new AsyncContextResult { InvocationId = Guid.NewGuid().ToString("N") };
            results[invocationIndex] = result;

            tasks[invocationIndex] = Task.Run(async () =>
            {
                try
                {
                    Powertools.Metrics.Metrics.AddMetric($"pre_await_metric_{invocationIndex}", 1, MetricUnit.Count);
                    result.MainThreadMetricAdded = true;
                    await Task.Delay(Random.Shared.Next(10, 50));
                    Powertools.Metrics.Metrics.AddMetric($"post_await_metric_{invocationIndex}", 2, MetricUnit.Count);
                    Powertools.Metrics.Metrics.AddDimension($"async_dim_{invocationIndex}", "value");
                    result.PostAwaitMetricAdded = true;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = ex.Message;
                    result.ExceptionSource = "AsyncHandler";
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, $"{r.ExceptionSource}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.True(r.MainThreadMetricAdded));
        Assert.All(results, r => Assert.True(r.PostAwaitMetricAdded));
    }


    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task FlushDuringBackgroundWork_ShouldNotThrowException(int backgroundTaskCount)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var backgroundTasksStarted = new TaskCompletionSource<bool>();
        var startedCount = 0;
        var flushCompleted = new TaskCompletionSource<bool>();
        var backgroundExceptions = new List<Exception>();
        Exception? flushException = null;

        var backgroundTasks = new Task[backgroundTaskCount];
        for (int i = 0; i < backgroundTaskCount; i++)
        {
            int taskIndex = i;
            backgroundTasks[i] = Task.Run(async () =>
            {
                try
                {
                    if (Interlocked.Increment(ref startedCount) == backgroundTaskCount)
                    {
                        backgroundTasksStarted.TrySetResult(true);
                    }
                    
                    int metricCount = 0;
                    while (!flushCompleted.Task.IsCompleted && metricCount < 100)
                    {
                        Powertools.Metrics.Metrics.AddMetric($"bg_metric_{taskIndex}_{metricCount}", metricCount, MetricUnit.Count);
                        metricCount++;
                        await Task.Delay(1);
                    }
                }
                catch (Exception ex)
                {
                    lock (backgroundExceptions) { backgroundExceptions.Add(ex); }
                }
            });
        }

        var tasksStartedOk = await Task.WhenAny(backgroundTasksStarted.Task, Task.Delay(TimeSpan.FromSeconds(10))) == backgroundTasksStarted.Task;

        try
        {
            Powertools.Metrics.Metrics.AddMetric("main_metric", 1, MetricUnit.Count);
            await Task.Delay(10);
            Powertools.Metrics.Metrics.Flush();
        }
        catch (Exception ex) { flushException = ex; }
        finally { flushCompleted.TrySetResult(true); }

        await Task.WhenAll(backgroundTasks).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(tasksStartedOk, "Background tasks did not start in time");
        Assert.Null(flushException);
        Assert.Empty(backgroundExceptions);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    public void OverlappingInvocationsWithBackgroundTasks_ShouldNotThrowException(int invocationCount, int backgroundTasksPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var allExceptions = new List<(string Source, Exception Ex)>();
        var allTasksCompleted = new CountdownEvent(invocationCount * (1 + backgroundTasksPerInvocation));
        var barrier = new Barrier(invocationCount);

        var invocationTasks = new Task[invocationCount];
        for (int inv = 0; inv < invocationCount; inv++)
        {
            int invocationIndex = inv;
            invocationTasks[inv] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    Powertools.Metrics.Metrics.AddMetric($"inv_{invocationIndex}_main", 1, MetricUnit.Count);
                    Powertools.Metrics.Metrics.AddDimension($"inv_{invocationIndex}_dim", "value");

                    for (int bg = 0; bg < backgroundTasksPerInvocation; bg++)
                    {
                        int bgIndex = bg;
                        Task.Run(() =>
                        {
                            try
                            {
                                Thread.Sleep(Random.Shared.Next(5, 20));
                                Powertools.Metrics.Metrics.AddMetric($"inv_{invocationIndex}_bg_{bgIndex}", 1, MetricUnit.Count);
                            }
                            catch (Exception ex)
                            {
                                lock (allExceptions) { allExceptions.Add(($"Inv{invocationIndex}_Bg{bgIndex}", ex)); }
                            }
                            finally { allTasksCompleted.Signal(); }
                        });
                    }

                    Thread.Sleep(Random.Shared.Next(10, 30));
                    Powertools.Metrics.Metrics.Flush();
                }
                catch (Exception ex)
                {
                    lock (allExceptions) { allExceptions.Add(($"Inv{invocationIndex}_Main", ex)); }
                }
                finally { allTasksCompleted.Signal(); }
            });
        }

        allTasksCompleted.Wait(TimeSpan.FromSeconds(30));
        Task.WaitAll(invocationTasks);

        Assert.Empty(allExceptions);
    }
}
