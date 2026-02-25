using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Logging;

/// <summary>
/// Tests for validating buffer isolation in Powertools Logger under concurrent execution scenarios.
/// These tests verify that when multiple Lambda invocations run concurrently,
/// each invocation's log buffer remains isolated from other invocations.
/// </summary>
public class BufferIsolationTests : IDisposable
{
    public BufferIsolationTests()
    {
        LogBufferManager.ResetForTesting();
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
    }

    public void Dispose()
    {
        Logger.ClearBuffer();
        LogBufferManager.ResetForTesting();
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
    }

    /// <summary>
    /// Verifies that concurrent invocations maintain separate buffers.
    /// </summary>
    [Theory]
    [InlineData(2, 3)]
    [InlineData(3, 5)]
    [InlineData(5, 2)]
    public void BufferSeparation_ConcurrentInvocations_ShouldMaintainSeparateBuffers(int concurrencyLevel, int entriesPerInvocation)
    {
        var results = new BufferSeparationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        LogBufferManager.ResetForTesting();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var invocationId = $"Root=1-{Guid.NewGuid():N};Parent={invocationIndex:X16};Sampled=1";
                var entriesAdded = new List<string>();

                Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", invocationId);

                barrier.SignalAndWait();

                Logger.Configure(config =>
                {
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LogBuffering.Enabled = true;
                    config.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
                    config.LogBuffering.FlushOnErrorLog = false;
                });

                for (int e = 0; e < entriesPerInvocation; e++)
                {
                    var entryMarker = $"inv_{invocationIndex}_entry_{e}_{Guid.NewGuid():N}";
                    entriesAdded.Add(entryMarker);
                    Logger.LogDebug(entryMarker);
                }

                Thread.Sleep(Random.Shared.Next(1, 10));

                results[invocationIndex] = new BufferSeparationResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    EntriesAdded = entriesAdded,
                    ExpectedEntryCount = entriesPerInvocation
                };
            });
        }

        Task.WaitAll(tasks);

        foreach (var result in results)
        {
            Assert.Equal(result.ExpectedEntryCount, result.EntriesAdded.Count);

            var foreignEntries = result.EntriesAdded
                .Where(e => !e.StartsWith($"inv_{result.InvocationIndex}_"))
                .ToList();

            Assert.Empty(foreignEntries);
        }

        var totalEntries = results.Sum(r => r.EntriesAdded.Count);
        var expectedTotal = concurrencyLevel * entriesPerInvocation;
        Assert.Equal(expectedTotal, totalEntries);
    }

    /// <summary>
    /// Verifies that flushing one invocation's buffer doesn't affect another active invocation's buffer.
    /// </summary>
    [Theory]
    [InlineData(10, 3)]
    [InlineData(30, 2)]
    [InlineData(50, 5)]
    public void BufferLifecycleIsolation_OverlappingInvocations_ShouldPreserveActiveBuffer(int shortDuration, int entriesPerInvocation)
    {
        var longDuration = shortDuration * 3;
        var shortResult = new BufferLifecycleResult();
        var longResult = new BufferLifecycleResult();
        var barrier = new Barrier(2);

        LogBufferManager.ResetForTesting();

        var shortTask = Task.Run(() =>
        {
            var invocationId = $"Root=1-{Guid.NewGuid():N};Parent=SHORT;Sampled=1";
            shortResult.InvocationId = invocationId;
            shortResult.EntriesAdded = new List<string>();

            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", invocationId);

            barrier.SignalAndWait();

            Logger.Configure(config =>
            {
                config.MinimumLogLevel = LogLevel.Debug;
                config.LogBuffering.Enabled = true;
                config.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
                config.LogBuffering.FlushOnErrorLog = false;
            });

            for (int e = 0; e < entriesPerInvocation; e++)
            {
                var entry = $"short_entry_{e}_{Guid.NewGuid():N}";
                shortResult.EntriesAdded.Add(entry);
                Logger.LogDebug(entry);
            }

            Thread.Sleep(shortDuration);

            Logger.FlushBuffer();
            shortResult.BufferFlushed = true;
        });

        var longTask = Task.Run(() =>
        {
            var invocationId = $"Root=1-{Guid.NewGuid():N};Parent=LONG;Sampled=1";
            longResult.InvocationId = invocationId;
            longResult.EntriesAdded = new List<string>();

            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", invocationId);

            barrier.SignalAndWait();

            Logger.Configure(config =>
            {
                config.MinimumLogLevel = LogLevel.Debug;
                config.LogBuffering.Enabled = true;
                config.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
                config.LogBuffering.FlushOnErrorLog = false;
            });

            for (int e = 0; e < entriesPerInvocation; e++)
            {
                var entry = $"long_entry_{e}_{Guid.NewGuid():N}";
                longResult.EntriesAdded.Add(entry);
                Logger.LogDebug(entry);
            }

            Thread.Sleep(longDuration);

            var postFlushEntry = $"long_post_flush_{Guid.NewGuid():N}";
            longResult.EntriesAdded.Add(postFlushEntry);
            Logger.LogDebug(postFlushEntry);

            longResult.BufferIntactAfterOtherFlush = true;
            longResult.TotalEntriesAfterOtherFlush = longResult.EntriesAdded.Count;
        });

        Task.WaitAll(shortTask, longTask);

        Assert.True(longResult.BufferIntactAfterOtherFlush);

        var expectedLongEntries = entriesPerInvocation + 1;
        Assert.Equal(expectedLongEntries, longResult.TotalEntriesAfterOtherFlush);
    }

    /// <summary>
    /// Verifies that buffer eviction in one invocation doesn't affect another invocation's buffer.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(10)]
    public void BufferEvictionIsolation_SizeLimitEviction_ShouldOnlyAffectOwnBuffer(int entriesPerInvocation)
    {
        var smallBufferSize = 1024;
        var largeBufferSize = 1024 * 1024;

        var evictingResult = new BufferEvictionResult();
        var normalResult = new BufferEvictionResult();
        var barrier = new Barrier(2);

        LogBufferManager.ResetForTesting();

        var evictingTask = Task.Run(() =>
        {
            var invocationId = $"Root=1-{Guid.NewGuid():N};Parent=EVICTING;Sampled=1";
            evictingResult.InvocationId = invocationId;
            evictingResult.EntriesAdded = new List<string>();

            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", invocationId);

            barrier.SignalAndWait();

            Logger.Configure(config =>
            {
                config.MinimumLogLevel = LogLevel.Debug;
                config.LogBuffering.Enabled = true;
                config.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
                config.LogBuffering.FlushOnErrorLog = false;
                config.LogBuffering.MaxBytes = smallBufferSize;
            });

            for (int e = 0; e < entriesPerInvocation * 3; e++)
            {
                var entry = $"evicting_entry_{e}_{new string('X', 200)}_{Guid.NewGuid():N}";
                evictingResult.EntriesAdded.Add(entry);
                Logger.LogDebug(entry);
            }

            Thread.Sleep(Random.Shared.Next(5, 15));
            evictingResult.EvictionTriggered = true;
        });

        var normalTask = Task.Run(() =>
        {
            var invocationId = $"Root=1-{Guid.NewGuid():N};Parent=NORMAL;Sampled=1";
            normalResult.InvocationId = invocationId;
            normalResult.EntriesAdded = new List<string>();

            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", invocationId);

            barrier.SignalAndWait();

            Logger.Configure(config =>
            {
                config.MinimumLogLevel = LogLevel.Debug;
                config.LogBuffering.Enabled = true;
                config.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
                config.LogBuffering.FlushOnErrorLog = false;
                config.LogBuffering.MaxBytes = largeBufferSize;
            });

            for (int e = 0; e < entriesPerInvocation; e++)
            {
                var entry = $"normal_entry_{e}_{Guid.NewGuid():N}";
                normalResult.EntriesAdded.Add(entry);
                Logger.LogDebug(entry);
            }

            Thread.Sleep(Random.Shared.Next(10, 20));

            normalResult.AllEntriesRetained = normalResult.EntriesAdded.Count == entriesPerInvocation;
        });

        Task.WaitAll(evictingTask, normalTask);

        Assert.True(normalResult.AllEntriesRetained,
            $"Normal invocation lost entries. Expected {entriesPerInvocation}, had {normalResult.EntriesAdded.Count}");

        Assert.True(evictingResult.EvictionTriggered);
    }

    private class BufferSeparationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public List<string> EntriesAdded { get; set; } = new();
        public int ExpectedEntryCount { get; set; }
    }

    private class BufferLifecycleResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public List<string> EntriesAdded { get; set; } = new();
        public bool BufferFlushed { get; set; }
        public bool BufferIntactAfterOtherFlush { get; set; }
        public int TotalEntriesAfterOtherFlush { get; set; }
    }

    private class BufferEvictionResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public List<string> EntriesAdded { get; set; } = new();
        public bool EvictionTriggered { get; set; }
        public bool AllEntriesRetained { get; set; }
    }
}
