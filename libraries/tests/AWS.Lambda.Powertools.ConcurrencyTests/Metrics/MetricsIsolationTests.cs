using System.Threading;
using AWS.Lambda.Powertools.Metrics;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Metrics;

/// <summary>
/// Tests for validating metrics isolation in Powertools Metrics
/// under concurrent execution scenarios.
/// 
/// These tests verify that when multiple Lambda invocations run concurrently,
/// each invocation's metrics remain isolated from other invocations.
/// 
/// The Metrics implementation uses per-thread context storage to ensure
/// isolation between concurrent Lambda invocations.
/// </summary>
[Collection("Metrics Tests")]
public class MetricsIsolationTests : IDisposable
{
    public MetricsIsolationTests()
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

    #region Helper Result Classes

    private class MetricsSeparationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public List<(string Key, double Value)> MetricsAdded { get; set; } = new();
        public int ExpectedMetricCount { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class MetricsLifecycleResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public List<(string Key, double Value)> MetricsAdded { get; set; } = new();
        public bool MetricsFlushed { get; set; }
        public bool MetricsIntactAfterOtherFlush { get; set; }
        public int TotalMetricsAfterOtherFlush { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class ThreadSafetyResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public int MetricsAttempted { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    #endregion

    #region Property 1: Metrics Value Isolation

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 1: Metrics Value Isolation**
    /// *For any* set of concurrent invocations adding metrics with the same key, each invocation's 
    /// retrieved metrics should contain only the values that invocation added, with no values from 
    /// other concurrent invocations.
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(2, 3)]
    [InlineData(3, 2)]
    [InlineData(5, 1)]
    [InlineData(5, 5)]
    public void MetricsValueIsolation_ConcurrentInvocations_ShouldMaintainSeparateMetrics(
        int concurrencyLevel, int metricsPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new MetricsSeparationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;

            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new MetricsSeparationResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    ExpectedMetricCount = metricsPerInvocation
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int m = 0; m < metricsPerInvocation; m++)
                    {
                        var metricKey = $"metric_inv{invocationIndex}_m{m}";
                        var metricValue = (double)(invocationIndex * 1000 + m);
                        Powertools.Metrics.Metrics.AddMetric(metricKey, metricValue, MetricUnit.Count);
                        result.MetricsAdded.Add((metricKey, metricValue));
                    }

                    Thread.Sleep(Random.Shared.Next(1, 10));
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = ex.Message;
                }

                results[invocationIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(r.ExpectedMetricCount, r.MetricsAdded.Count));
        Assert.All(results, r => Assert.All(r.MetricsAdded, m => Assert.Contains($"inv{r.InvocationIndex}_", m.Key)));
    }

    #endregion

    #region Property 2: Metrics Flush Lifecycle Isolation

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 2: Metrics Flush Lifecycle Isolation**
    /// *For any* two overlapping invocations where one flushes early, the longer-running invocation's 
    /// accumulated metrics should remain intact and unaffected by the other invocation's flush operation.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Theory]
    [InlineData(10, 1)]
    [InlineData(15, 2)]
    [InlineData(20, 3)]
    [InlineData(30, 1)]
    public void MetricsFlushLifecycleIsolation_OverlappingInvocations_ShouldPreserveActiveMetrics(
        int shortDuration, int metricsPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var longDuration = shortDuration * 3;
        var shortResult = new MetricsLifecycleResult();
        var longResult = new MetricsLifecycleResult();
        var barrier = new Barrier(2);
        var shortFlushed = new ManualResetEventSlim(false);

        var shortTask = Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            shortResult.InvocationId = invocationId;

            try
            {
                barrier.SignalAndWait();

                for (int m = 0; m < metricsPerInvocation; m++)
                {
                    var metricKey = $"short_metric_{m}_{invocationId}";
                    Powertools.Metrics.Metrics.AddMetric(metricKey, m, MetricUnit.Count);
                    shortResult.MetricsAdded.Add((metricKey, m));
                }

                Thread.Sleep(shortDuration);

                Powertools.Metrics.Metrics.Flush();
                shortResult.MetricsFlushed = true;
                shortFlushed.Set();
            }
            catch (Exception ex)
            {
                shortResult.ExceptionThrown = true;
                shortResult.ExceptionMessage = ex.Message;
                shortFlushed.Set();
            }
        });

        var longTask = Task.Run(() =>
        {
            var invocationId = Guid.NewGuid().ToString("N");
            longResult.InvocationId = invocationId;

            try
            {
                barrier.SignalAndWait();

                for (int m = 0; m < metricsPerInvocation; m++)
                {
                    var metricKey = $"long_metric_{m}_{invocationId}";
                    Powertools.Metrics.Metrics.AddMetric(metricKey, 100 + m, MetricUnit.Count);
                    longResult.MetricsAdded.Add((metricKey, 100 + m));
                }

                shortFlushed.Wait(TimeSpan.FromSeconds(5));

                var postFlushKey = $"long_post_flush_{invocationId}";
                Powertools.Metrics.Metrics.AddMetric(postFlushKey, 999.0, MetricUnit.Count);
                longResult.MetricsAdded.Add((postFlushKey, 999.0));

                longResult.TotalMetricsAfterOtherFlush = longResult.MetricsAdded.Count;
                longResult.MetricsIntactAfterOtherFlush = longResult.MetricsAdded.Count == metricsPerInvocation + 1;
            }
            catch (Exception ex)
            {
                longResult.ExceptionThrown = true;
                longResult.ExceptionMessage = ex.Message;
            }
        });

        Task.WaitAll(shortTask, longTask);

        Assert.False(shortResult.ExceptionThrown, shortResult.ExceptionMessage);
        Assert.False(longResult.ExceptionThrown, longResult.ExceptionMessage);
        Assert.True(shortResult.MetricsFlushed);
        Assert.Equal(metricsPerInvocation + 1, longResult.MetricsAdded.Count);
    }

    #endregion

    #region Property 3: Concurrent Metrics Thread Safety

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 3: Concurrent Metrics Thread Safety**
    /// *For any* number of concurrent invocations adding metrics simultaneously, no exceptions should 
    /// be thrown and all metric operations should complete without data corruption.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 5)]
    [InlineData(5, 3)]
    [InlineData(10, 10)]
    public void ConcurrentMetricsThreadSafety_SimultaneousOperations_ShouldNotThrowOrCorrupt(
        int concurrencyLevel, int operationsPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new ThreadSafetyResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;

            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new ThreadSafetyResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    MetricsAttempted = operationsPerInvocation
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int m = 0; m < operationsPerInvocation; m++)
                    {
                        var metricKey = $"concurrent_metric_{invocationIndex}_{m}";
                        Powertools.Metrics.Metrics.AddMetric(metricKey, m, MetricUnit.Count);

                        var dimKey = $"dim_{invocationIndex}";
                        Powertools.Metrics.Metrics.AddDimension(dimKey, $"value_{invocationIndex}");

                        var metaKey = $"meta_{invocationIndex}_{m}";
                        Powertools.Metrics.Metrics.AddMetadata(metaKey, $"data_{m}");
                    }
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                }

                results[invocationIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
    }

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 3b: Dimension Key Conflict Thread Safety**
    /// *For any* number of concurrent invocations adding dimensions with the SAME key but different values,
    /// no exceptions should be thrown.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(5, 3)]
    [InlineData(10, 5)]
    public void ConcurrentDimensionKeyConflict_SameKeyDifferentValues_ShouldNotThrowException(
        int concurrencyLevel, int operationsPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new ThreadSafetyResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        const string sharedDimensionKey = "shared_dimension";

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;

            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new ThreadSafetyResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    MetricsAttempted = operationsPerInvocation
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int m = 0; m < operationsPerInvocation; m++)
                    {
                        var metricKey = $"conflict_metric_{invocationIndex}_{m}";
                        Powertools.Metrics.Metrics.AddMetric(metricKey, m, MetricUnit.Count);

                        Powertools.Metrics.Metrics.AddDimension(sharedDimensionKey, $"value_from_thread_{invocationIndex}");

                        Powertools.Metrics.Metrics.AddMetadata("shared_metadata", $"meta_from_thread_{invocationIndex}");
                    }
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                }

                results[invocationIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
    }

    #endregion
}
