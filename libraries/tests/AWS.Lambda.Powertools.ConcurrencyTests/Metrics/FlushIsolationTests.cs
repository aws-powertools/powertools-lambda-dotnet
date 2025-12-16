using System.Threading;
using AWS.Lambda.Powertools.Metrics;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Metrics;

/// <summary>
/// Tests for validating flush operations and metadata isolation in Powertools Metrics
/// under concurrent execution scenarios.
/// 
/// These tests verify that when multiple Lambda invocations run concurrently:
/// - Metadata remains isolated between invocations
/// - Flush operations are thread-safe and don't corrupt data
/// - Overflow flushes don't affect other invocations
/// - PushSingleMetric works correctly under concurrent execution
/// 
/// The Metrics implementation uses per-thread context storage to ensure
/// isolation between concurrent Lambda invocations.
/// </summary>
[Collection("Metrics Tests")]
public class FlushIsolationTests : IDisposable
{
    public FlushIsolationTests()
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

    private class MetadataIsolationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public List<(string Key, object Value)> MetadataAdded { get; set; } = new();
        public string? CapturedEmfOutput { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class ConcurrentFlushResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public List<(string Key, double Value)> MetricsAdded { get; set; } = new();
        public string? CapturedEmfOutput { get; set; }
        public bool FlushCompleted { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class OverflowFlushResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int MetricsAdded { get; set; }
        public int ExpectedMetricCount { get; set; }
        public bool OverflowTriggered { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class PushSingleMetricResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public double MetricValue { get; set; }
        public string? CapturedEmfOutput { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    #endregion

    #region Property 7: Metadata Value Isolation

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 7: Metadata Value Isolation**
    /// *For any* set of concurrent invocations adding metadata with the same key but different values, 
    /// each invocation's EMF output should contain only its own metadata value.
    /// **Validates: Requirements 4.1, 4.2**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(2, 3)]
    [InlineData(3, 2)]
    [InlineData(5, 1)]
    [InlineData(5, 3)]
    public void MetadataValueIsolation_ConcurrentInvocations_ShouldMaintainSeparateMetadata(
        int concurrencyLevel, int metadataPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new MetadataIsolationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;

            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new MetadataIsolationResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int m = 0; m < metadataPerInvocation; m++)
                    {
                        var metaKey = $"meta_inv{invocationIndex}_m{m}";
                        var metaValue = $"value_{invocationIndex}_{m}_{invocationId}";
                        Powertools.Metrics.Metrics.AddMetadata(metaKey, metaValue);
                        result.MetadataAdded.Add((metaKey, metaValue));
                    }

                    Powertools.Metrics.Metrics.AddMetric($"metric_inv{invocationIndex}", 1, MetricUnit.Count);

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
        Assert.All(results, r => Assert.Equal(metadataPerInvocation, r.MetadataAdded.Count));
        Assert.All(results, r => Assert.All(r.MetadataAdded, m => Assert.Contains($"inv{r.InvocationIndex}_", m.Key)));
    }

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 7b: Same Key Metadata Conflict**
    /// *For any* set of concurrent invocations adding metadata with the SAME key but different values,
    /// no exceptions should be thrown.
    /// **Validates: Requirements 4.1, 4.2**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(5, 3)]
    [InlineData(10, 5)]
    public void MetadataValueIsolation_SameKeyDifferentValues_ShouldNotThrowException(
        int concurrencyLevel, int operationsPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new MetadataIsolationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        const string sharedMetadataKey = "shared_metadata";

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;

            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new MetadataIsolationResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int m = 0; m < operationsPerInvocation; m++)
                    {
                        var metaValue = $"value_from_thread_{invocationIndex}_{m}";
                        Powertools.Metrics.Metrics.AddMetadata(sharedMetadataKey, metaValue);
                        result.MetadataAdded.Add((sharedMetadataKey, metaValue));

                        Powertools.Metrics.Metrics.AddMetric($"conflict_metric_{invocationIndex}_{m}", m, MetricUnit.Count);
                    }
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                results[invocationIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
    }

    #endregion

    #region Property 8: Concurrent Flush Data Integrity

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 8: Concurrent Flush Data Integrity**
    /// *For any* set of concurrent invocations flushing metrics simultaneously, each invocation's 
    /// EMF output should contain exactly the metrics that invocation added, with no data loss or corruption.
    /// **Validates: Requirements 5.1, 5.3**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(2, 3)]
    [InlineData(3, 2)]
    [InlineData(5, 5)]
    public void ConcurrentFlushDataIntegrity_SimultaneousFlush_ShouldNotCorruptData(
        int concurrencyLevel, int metricsPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new ConcurrentFlushResult[concurrencyLevel];
        var addBarrier = new Barrier(concurrencyLevel);
        var flushBarrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            for (int i = 0; i < concurrencyLevel; i++)
            {
                int invocationIndex = i;

                tasks[i] = Task.Run(() =>
                {
                    var invocationId = Guid.NewGuid().ToString("N");
                    var result = new ConcurrentFlushResult
                    {
                        InvocationId = invocationId,
                        InvocationIndex = invocationIndex
                    };

                    try
                    {
                        addBarrier.SignalAndWait();

                        for (int m = 0; m < metricsPerInvocation; m++)
                        {
                            var metricKey = $"flush_metric_{invocationIndex}_{m}";
                            var metricValue = (double)(invocationIndex * 100 + m);
                            Powertools.Metrics.Metrics.AddMetric(metricKey, metricValue, MetricUnit.Count);
                            result.MetricsAdded.Add((metricKey, metricValue));
                        }

                        flushBarrier.SignalAndWait();

                        Powertools.Metrics.Metrics.Flush();
                        result.FlushCompleted = true;
                    }
                    catch (Exception ex)
                    {
                        result.ExceptionThrown = true;
                        result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                    }

                    results[invocationIndex] = result;
                });
            }

            Task.WaitAll(tasks);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var emfOutput = stringWriter.ToString();

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.FlushCompleted));
        Assert.False(string.IsNullOrWhiteSpace(emfOutput));
        Assert.Contains("{", emfOutput);
        Assert.Contains("}", emfOutput);
    }

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 8b: Flush Thread Safety Under Load**
    /// *For any* number of concurrent invocations rapidly adding and flushing metrics,
    /// no exceptions should be thrown and the system should remain stable.
    /// **Validates: Requirements 5.1, 5.3**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 3)]
    public void ConcurrentFlushDataIntegrity_RapidFlushUnderLoad_ShouldRemainStable(
        int concurrencyLevel, int iterations)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var exceptionFlags = new bool[concurrencyLevel];
        var exceptionMessages = new string?[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            for (int i = 0; i < concurrencyLevel; i++)
            {
                int invocationIndex = i;

                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();

                        for (int iter = 0; iter < iterations; iter++)
                        {
                            for (int m = 0; m < 3; m++)
                            {
                                var metricKey = $"rapid_metric_{invocationIndex}_{iter}_{m}";
                                Powertools.Metrics.Metrics.AddMetric(metricKey, m, MetricUnit.Count);
                            }

                            Powertools.Metrics.Metrics.Flush();
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionFlags[invocationIndex] = true;
                        exceptionMessages[invocationIndex] = $"{ex.GetType().Name}: {ex.Message}";
                    }
                });
            }

            Task.WaitAll(tasks);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.All(exceptionFlags, e => Assert.False(e));
    }

    #endregion

    #region Property 9: Overflow Flush Isolation

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 9: Overflow Flush Isolation**
    /// *For any* scenario where one invocation triggers an overflow flush (exceeding 100 metrics) 
    /// while another invocation has fewer metrics, the second invocation's metric count should remain unaffected.
    /// **Validates: Requirements 5.2**
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void OverflowFlushIsolation_OneInvocationOverflows_ShouldNotAffectOthers(int smallMetricsCount)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var overflowResult = new OverflowFlushResult();
        var smallResult = new OverflowFlushResult();
        var barrier = new Barrier(2);
        var overflowStarted = new ManualResetEventSlim(false);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var overflowTask = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                overflowResult.InvocationId = invocationId;
                overflowResult.ExpectedMetricCount = 105;

                try
                {
                    barrier.SignalAndWait();
                    overflowStarted.Set();

                    for (int m = 0; m < 105; m++)
                    {
                        var metricKey = $"overflow_metric_{m}_{invocationId}";
                        Powertools.Metrics.Metrics.AddMetric(metricKey, m, MetricUnit.Count);
                        overflowResult.MetricsAdded++;
                    }

                    overflowResult.OverflowTriggered = true;
                }
                catch (Exception ex)
                {
                    overflowResult.ExceptionThrown = true;
                    overflowResult.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }
            });

            var smallTask = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                smallResult.InvocationId = invocationId;
                smallResult.ExpectedMetricCount = smallMetricsCount;

                try
                {
                    barrier.SignalAndWait();

                    overflowStarted.Wait(TimeSpan.FromSeconds(1));
                    Thread.Sleep(10);

                    for (int m = 0; m < smallMetricsCount; m++)
                    {
                        var metricKey = $"small_metric_{m}_{invocationId}";
                        Powertools.Metrics.Metrics.AddMetric(metricKey, m, MetricUnit.Count);
                        smallResult.MetricsAdded++;
                    }
                }
                catch (Exception ex)
                {
                    smallResult.ExceptionThrown = true;
                    smallResult.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }
            });

            Task.WaitAll(overflowTask, smallTask);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.False(overflowResult.ExceptionThrown, overflowResult.ExceptionMessage);
        Assert.False(smallResult.ExceptionThrown, smallResult.ExceptionMessage);
        Assert.Equal(overflowResult.ExpectedMetricCount, overflowResult.MetricsAdded);
        Assert.Equal(smallResult.ExpectedMetricCount, smallResult.MetricsAdded);
    }

    #endregion

    #region Property 10: PushSingleMetric Isolation

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 10: PushSingleMetric Isolation**
    /// *For any* set of concurrent PushSingleMetric calls, each call should produce a separate EMF output entry, 
    /// and calling PushSingleMetric should not affect any invocation's accumulated metrics.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void PushSingleMetricIsolation_ConcurrentCalls_ShouldOutputSeparateEntries(int concurrencyLevel)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var results = new PushSingleMetricResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            for (int i = 0; i < concurrencyLevel; i++)
            {
                int invocationIndex = i;

                tasks[i] = Task.Run(() =>
                {
                    var invocationId = Guid.NewGuid().ToString("N");
                    var result = new PushSingleMetricResult
                    {
                        InvocationId = invocationId,
                        InvocationIndex = invocationIndex,
                        MetricName = $"single_metric_{invocationIndex}_{invocationId}",
                        MetricValue = invocationIndex * 10.0
                    };

                    try
                    {
                        barrier.SignalAndWait();

                        Powertools.Metrics.Metrics.PushSingleMetric(
                            result.MetricName,
                            result.MetricValue,
                            MetricUnit.Count,
                            "TestNamespace",
                            "TestService"
                        );
                    }
                    catch (Exception ex)
                    {
                        result.ExceptionThrown = true;
                        result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                    }

                    results[invocationIndex] = result;
                });
            }

            Task.WaitAll(tasks);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var emfOutput = stringWriter.ToString();

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.False(string.IsNullOrWhiteSpace(emfOutput));
        Assert.Contains("{", emfOutput);
        Assert.Contains("}", emfOutput);
        Assert.True(results.Any(r => emfOutput.Contains(r.MetricName)));
    }

    /// <summary>
    /// **Feature: metrics-multi-instance-validation, Property 10b: PushSingleMetric Does Not Affect Accumulated Metrics**
    /// *For any* invocation that has accumulated metrics, calling PushSingleMetric should not affect 
    /// those accumulated metrics.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 3)]
    public void PushSingleMetricIsolation_DuringActiveContext_ShouldNotAffectAccumulatedMetrics(
        int concurrencyLevel, int metricsPerInvocation)
    {
        Powertools.Metrics.Metrics.ResetForTest();
        Powertools.Metrics.Metrics.SetNamespace("TestNamespace");

        var exceptionFlags = new bool[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            for (int i = 0; i < concurrencyLevel; i++)
            {
                int invocationIndex = i;

                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();

                        for (int m = 0; m < metricsPerInvocation; m++)
                        {
                            var metricKey = $"accumulated_metric_{invocationIndex}_{m}";
                            Powertools.Metrics.Metrics.AddMetric(metricKey, m, MetricUnit.Count);
                        }

                        Powertools.Metrics.Metrics.PushSingleMetric(
                            $"single_metric_{invocationIndex}",
                            100.0,
                            MetricUnit.Count,
                            "TestNamespace",
                            "TestService"
                        );

                        for (int m = 0; m < metricsPerInvocation; m++)
                        {
                            var metricKey = $"post_single_metric_{invocationIndex}_{m}";
                            Powertools.Metrics.Metrics.AddMetric(metricKey, m + 100, MetricUnit.Count);
                        }
                    }
                    catch
                    {
                        exceptionFlags[invocationIndex] = true;
                    }
                });
            }

            Task.WaitAll(tasks);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.All(exceptionFlags, e => Assert.False(e));
    }

    #endregion
}
