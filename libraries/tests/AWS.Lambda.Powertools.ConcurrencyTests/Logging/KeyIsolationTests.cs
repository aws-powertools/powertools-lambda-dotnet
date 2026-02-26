using AWS.Lambda.Powertools.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Logging;

/// <summary>
/// Tests for validating key isolation in Powertools Logger under concurrent execution scenarios.
/// These tests verify that when multiple Lambda invocations run concurrently (multi-instance mode),
/// each invocation's logging keys remain isolated from other invocations.
/// </summary>
public class KeyIsolationTests
{
    /// <summary>
    /// Demonstrates that a shared static Dictionary (old implementation) would fail under concurrent access.
    /// This proves our tests would catch the thread-safety bug.
    /// </summary>
    [Fact]
    public void DemonstrateOldImplementationWouldFail_SharedStaticDictionary()
    {
        var sharedScope = new Dictionary<string, object>();
        var concurrencyLevel = 5;
        var results = new (string InvocationId, string UniqueKey, Dictionary<string, object> AllKeysAtEnd)[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var lockObj = new object();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString();
                var uniqueKey = $"invocation_{invocationId}";

                barrier.SignalAndWait();

                lock (lockObj)
                {
                    sharedScope[uniqueKey] = invocationId;
                }

                Thread.Sleep(Random.Shared.Next(1, 10));

                Dictionary<string, object> allKeys;
                lock (lockObj)
                {
                    allKeys = new Dictionary<string, object>(sharedScope);
                }

                results[invocationIndex] = (invocationId, uniqueKey, allKeys);
            });
        }

        Task.WaitAll(tasks);

        var allUniqueKeys = results.Select(r => r.UniqueKey).ToHashSet();
        var anyLeakageDetected = results.Any(result =>
            result.AllKeysAtEnd.Keys.Any(k => allUniqueKeys.Contains(k) && k != result.UniqueKey));

        Assert.True(anyLeakageDetected,
            "Expected the OLD shared-dictionary implementation to show key leakage between concurrent invocations.");
    }

    /// <summary>
    /// Verifies that concurrent invocations don't leak keys to each other.
    /// Uses Logger.UseScope() to simulate Lambda multi-threaded mode where each invocation starts fresh.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void KeyIsolation_ConcurrentInvocations_ShouldNotLeakKeys(int concurrencyLevel)
    {
        var results = new ConcurrentInvocationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        ClearAllLoggerState();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(() =>
            {
                // Simulate Lambda invocation starting fresh with isolated scope
                using (Logger.UseScope())
                {
                    var invocationId = Guid.NewGuid().ToString();
                    var uniqueKey = $"invocation_{invocationId}";

                    barrier.SignalAndWait();

                    Logger.AppendKey(uniqueKey, invocationId);

                    Thread.Sleep(Random.Shared.Next(1, 10));

                    var allKeys = Logger.GetAllKeys().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    results[invocationIndex] = new ConcurrentInvocationResult
                    {
                        InvocationId = invocationId,
                        UniqueKey = uniqueKey,
                        AllKeysAtEnd = allKeys
                    };

                    Logger.RemoveKeys(uniqueKey);
                }
            });
        }

        Task.WaitAll(tasks);

        var allUniqueKeys = results.Select(r => r.UniqueKey).ToHashSet();

        foreach (var result in results)
        {
            var foreignKeys = result.AllKeysAtEnd.Keys
                .Where(k => allUniqueKeys.Contains(k) && k != result.UniqueKey)
                .ToList();

            Assert.Empty(foreignKeys);
        }
    }

    /// <summary>
    /// Verifies that GetAllKeys returns only the calling invocation's keys.
    /// Uses Logger.UseScope() to simulate Lambda multi-threaded mode.
    /// </summary>
    [Theory]
    [InlineData(2, 3)]
    [InlineData(5, 2)]
    [InlineData(8, 5)]
    public void GetAllKeys_ConcurrentInvocations_ShouldReturnOnlyOwnKeys(int concurrencyLevel, int keysPerInvocation)
    {
        var results = new GetAllKeysResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        ClearAllLoggerState();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(() =>
            {
                // Simulate Lambda invocation starting fresh with isolated scope
                using (Logger.UseScope())
                {
                    var invocationId = Guid.NewGuid().ToString();
                    var appendedKeys = new Dictionary<string, object>();

                    barrier.SignalAndWait();

                    for (int k = 0; k < keysPerInvocation; k++)
                    {
                        var key = $"inv_{invocationId}_key_{k}";
                        var value = $"value_{k}_{invocationId}";
                        Logger.AppendKey(key, value);
                        appendedKeys[key] = value;
                    }

                    Thread.Sleep(Random.Shared.Next(1, 10));

                    var allKeysResult = Logger.GetAllKeys().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    results[invocationIndex] = new GetAllKeysResult
                    {
                        InvocationId = invocationId,
                        AppendedKeys = appendedKeys,
                        GetAllKeysResult_Keys = allKeysResult
                    };

                    Logger.RemoveKeys(appendedKeys.Keys.ToArray());
                }
            });
        }

        Task.WaitAll(tasks);

        var allAppendedKeysByInvocation = results.ToDictionary(
            r => r.InvocationId,
            r => r.AppendedKeys.Keys.ToHashSet());

        foreach (var result in results)
        {
            var ownKeys = result.AppendedKeys.Keys.ToHashSet();
            var otherInvocationKeys = allAppendedKeysByInvocation
                .Where(kvp => kvp.Key != result.InvocationId)
                .SelectMany(kvp => kvp.Value)
                .ToHashSet();

            var foreignKeysInResult = result.GetAllKeysResult_Keys.Keys
                .Where(k => otherInvocationKeys.Contains(k))
                .ToList();

            Assert.Empty(foreignKeysInResult);

            var missingOwnKeys = ownKeys.Where(k => !result.GetAllKeysResult_Keys.ContainsKey(k)).ToList();
            Assert.Empty(missingOwnKeys);
        }
    }

    /// <summary>
    /// Verifies that concurrent invocations using the same key name maintain separate values.
    /// Uses Logger.UseScope() to simulate Lambda multi-threaded mode.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void SameNameKey_ConcurrentInvocations_ShouldMaintainSeparateValues(int concurrencyLevel)
    {
        var sharedKeyName = "shared_test_key";
        var results = new SameNameKeyResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        ClearAllLoggerState();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(() =>
            {
                // Simulate Lambda invocation starting fresh with isolated scope
                using (Logger.UseScope())
                {
                    var invocationId = Guid.NewGuid().ToString();
                    var uniqueValue = $"unique_value_{invocationId}";

                    barrier.SignalAndWait();

                    Logger.AppendKey(sharedKeyName, uniqueValue);

                    Thread.Sleep(Random.Shared.Next(1, 15));

                    var allKeys = Logger.GetAllKeys().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    var retrievedValue = allKeys.TryGetValue(sharedKeyName, out var val) ? val?.ToString() : null;

                    results[invocationIndex] = new SameNameKeyResult
                    {
                        InvocationId = invocationId,
                        ExpectedValue = uniqueValue,
                        RetrievedValue = retrievedValue
                    };

                    Logger.RemoveKeys(sharedKeyName);
                }
            });
        }

        Task.WaitAll(tasks);

        foreach (var result in results)
        {
            Assert.Equal(result.ExpectedValue, result.RetrievedValue);
        }
    }

    /// <summary>
    /// Verifies that ClearState on one invocation doesn't affect another active invocation.
    /// Uses Logger.UseScope() to simulate Lambda multi-threaded mode.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(50)]
    public void ClearState_OverlappingInvocations_ShouldNotAffectActiveInvocation(int shortDuration)
    {
        var longDuration = shortDuration * 3;
        var shortInvocationResult = new ClearStateResult();
        var longInvocationResult = new ClearStateResult();
        var barrier = new Barrier(2);

        ClearAllLoggerState();

        var shortTask = Task.Run(() =>
        {
            // Simulate Lambda invocation starting fresh with isolated scope
            using (Logger.UseScope())
            {
                var invocationId = Guid.NewGuid().ToString();
                var uniqueKey = $"short_inv_{invocationId}";

                shortInvocationResult.InvocationId = invocationId;
                shortInvocationResult.UniqueKey = uniqueKey;

                barrier.SignalAndWait();

                Logger.AppendKey(uniqueKey, invocationId);
                shortInvocationResult.KeyAppended = true;

                Thread.Sleep(shortDuration);

                var keysToRemove = Logger.GetAllKeys().Select(k => k.Key).ToArray();
                Logger.RemoveKeys(keysToRemove);
                shortInvocationResult.ClearStateCalled = true;
            }
        });

        var longTask = Task.Run(() =>
        {
            // Simulate Lambda invocation starting fresh with isolated scope
            using (Logger.UseScope())
            {
                var invocationId = Guid.NewGuid().ToString();
                var uniqueKey = $"long_inv_{invocationId}";

                longInvocationResult.InvocationId = invocationId;
                longInvocationResult.UniqueKey = uniqueKey;

                barrier.SignalAndWait();

                Logger.AppendKey(uniqueKey, invocationId);
                longInvocationResult.KeyAppended = true;

                Thread.Sleep(longDuration);

                var allKeys = Logger.GetAllKeys().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                longInvocationResult.KeysAfterOtherClear = allKeys;
                longInvocationResult.OwnKeyStillPresent = allKeys.ContainsKey(uniqueKey);

                Logger.RemoveKeys(uniqueKey);
            }
        });

        Task.WaitAll(shortTask, longTask);

        Assert.True(longInvocationResult.OwnKeyStillPresent,
            $"Long invocation's key was removed when short invocation called ClearState");

        Assert.False(longInvocationResult.KeysAfterOtherClear.ContainsKey(shortInvocationResult.UniqueKey),
            $"Short invocation's key leaked into long invocation's scope");
    }

    private static void ClearAllLoggerState()
    {
        var existingKeys = Logger.GetAllKeys()
            .Select(k => k.Key)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToArray();
        if (existingKeys.Length > 0)
        {
            Logger.RemoveKeys(existingKeys);
        }
    }

    private class ConcurrentInvocationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public string UniqueKey { get; set; } = string.Empty;
        public Dictionary<string, object> AllKeysAtEnd { get; set; } = new();
    }

    private class GetAllKeysResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public Dictionary<string, object> AppendedKeys { get; set; } = new();
        public Dictionary<string, object> GetAllKeysResult_Keys { get; set; } = new();
    }

    private class SameNameKeyResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string? RetrievedValue { get; set; }
    }

    private class ClearStateResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public string UniqueKey { get; set; } = string.Empty;
        public bool KeyAppended { get; set; }
        public bool ClearStateCalled { get; set; }
        public Dictionary<string, object> KeysAfterOtherClear { get; set; } = new();
        public bool OwnKeyStillPresent { get; set; }
    }

    #region Regression Tests for Thread-Safety Bug

    /// <summary>
    /// Minimal reproduction test for the thread-safety bug.
    /// 
    /// Before the fix, this test would throw InvalidOperationException:
    /// "Collection was modified; enumeration operation may not execute."
    /// 
    /// Root cause was Logger.Scope being a static Dictionary&lt;string, object&gt;
    /// which is not thread-safe for concurrent read/write operations.
    /// 
    /// The fix uses AsyncLocal&lt;ConcurrentDictionary&gt; for per-execution-context
    /// scope storage, ensuring isolation and async/await safety.
    /// </summary>
    [Fact]
    public async Task ConcurrentAccess_ForeachOnGetAllKeys_ShouldNotThrowException()
    {
        // Clear any existing keys
        Logger.RemoveKeys(Logger.GetAllKeys()?.Select(x => x.Key).ToArray() ?? []);

        var tasks = new List<Task>
        {
            // Thread 1: Enumerate (mimics GetLogEntry line 229)
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    foreach (var kvp in Logger.GetAllKeys())
                    {
                        // Just enumerate
                    }
            }),

            // Thread 2: Log (also enumerates internally)
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    Logger.LogInformation($"Iteration {i}");
            }),

            // Thread 3: Modify keys
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    Logger.AppendKey($"key_{i % 10}", i);
                    Logger.RemoveKey($"key_{(i - 1) % 10}");
                }
            })
        };

        // With the fix, this should complete without throwing InvalidOperationException
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Regression test for the thread-safety bug where concurrent GetAllKeys enumeration
    /// during AppendKey/RemoveKey modifications would throw InvalidOperationException.
    /// 
    /// Root cause (before fix):
    /// Logger.Scope was a static Dictionary&lt;string, object&gt; which is not thread-safe.
    /// When Thread A enumerated via GetAllKeys() while Thread B modified via AppendKey()/RemoveKey(),
    /// it would throw "Collection was modified; enumeration operation may not execute."
    /// 
    /// The fix uses AsyncLocal&lt;ConcurrentDictionary&gt; for per-execution-context
    /// scope storage, ensuring isolation and async/await safety.
    /// </summary>
    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public async Task ConcurrentAccess_GetAllKeysDuringModification_ShouldNotThrowException(int iterations)
    {
        ClearAllLoggerState();
        
        var exceptions = new List<Exception>();
        var exceptionLock = new object();

        var tasks = new List<Task>
        {
            // Thread 1: Enumerate via GetAllKeys (mimics GetLogEntry line 229)
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        foreach (var kvp in Logger.GetAllKeys())
                        {
                            // Just enumerate - this is what GetLogEntry does internally
                            _ = kvp.Key;
                            _ = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            }),
            
            // Thread 2: Log (also enumerates internally via GetLogEntry)
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        Logger.LogInformation($"Iteration {i}");
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            }),
            
            // Thread 3: Modify keys rapidly
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        var keyName = $"key_{i % 10}";
                        Logger.AppendKey(keyName, i);
                        Logger.RemoveKey($"key_{(i - 1) % 10}");
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            })
        };

        await Task.WhenAll(tasks);

        // With the old implementation, this would throw InvalidOperationException or ArgumentException
        // With the fix (per-thread scope), no exceptions should occur
        Assert.Empty(exceptions);
    }

    /// <summary>
    /// Stress test: Multiple threads simultaneously performing all Logger operations.
    /// This validates that the AsyncLocal implementation handles high concurrency.
    /// Uses Logger.UseScope() to simulate Lambda multi-threaded mode.
    /// </summary>
    [Theory]
    [InlineData(3, 100)]
    [InlineData(5, 200)]
    [InlineData(10, 100)]
    public async Task StressTest_MultipleThreadsAllOperations_ShouldNotThrowException(int threadCount, int operationsPerThread)
    {
        ClearAllLoggerState();
        
        var exceptions = new List<Exception>();
        var exceptionLock = new object();
        var barrier = new Barrier(threadCount);

        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex => Task.Run(() =>
        {
            try
            {
                // Simulate Lambda invocation starting fresh with isolated scope
                using (Logger.UseScope())
                {
                    barrier.SignalAndWait();
                    
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        // Mix of all operations
                        var keyName = $"thread_{threadIndex}_key_{i % 5}";
                        
                        // AppendKey
                        Logger.AppendKey(keyName, $"value_{i}");
                        
                        // GetAllKeys with enumeration
                        var keys = Logger.GetAllKeys().ToList();
                        
                        // Log (internally calls GetAllKeys)
                        Logger.LogDebug($"Thread {threadIndex} iteration {i}, keys: {keys.Count}");
                        
                        // RemoveKey
                        if (i % 2 == 0)
                        {
                            Logger.RemoveKey(keyName);
                        }
                        
                        // RemoveKeys (batch)
                        if (i % 10 == 0)
                        {
                            var keysToRemove = Logger.GetAllKeys()
                                .Select(k => k.Key)
                                .Where(k => k.StartsWith($"thread_{threadIndex}_"))
                                .ToArray();
                            Logger.RemoveKeys(keysToRemove);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lock (exceptionLock)
                {
                    exceptions.Add(new Exception($"Thread {threadIndex}: {ex.Message}", ex));
                }
            }
        })).ToList();

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }

    #endregion
}
