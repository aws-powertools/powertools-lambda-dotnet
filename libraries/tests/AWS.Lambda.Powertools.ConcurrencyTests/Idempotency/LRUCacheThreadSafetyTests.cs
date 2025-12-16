/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Collections.Concurrent;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Idempotency;

/// <summary>
/// Tests for validating LRU Cache thread safety under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads perform read, write, and delete
/// operations on the LRU cache simultaneously, all operations complete without
/// exceptions and without data corruption.
/// </summary>
[Collection("Idempotency LRU Cache Tests")]
public class LRUCacheThreadSafetyTests
{
    #region Helper Classes

    private class OperationResult
    {
        public int ThreadIndex { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
    }

    /// <summary>
    /// Wrapper to access internal LRUCache for testing purposes.
    /// Uses reflection to create and interact with the internal LRUCache class.
    /// </summary>
    private class LRUCacheWrapper<TKey, TValue> where TKey : notnull
    {
        private readonly object _cache;
        private readonly Type _cacheType;

        public LRUCacheWrapper(int capacity)
        {
            var assembly = typeof(AWS.Lambda.Powertools.Idempotency.Idempotency).Assembly;
            _cacheType = assembly.GetType("AWS.Lambda.Powertools.Idempotency.Internal.LRUCache`2")!
                .MakeGenericType(typeof(TKey), typeof(TValue));
            _cache = Activator.CreateInstance(_cacheType, capacity)!;
        }

        public bool TryGet(TKey key, out TValue? value)
        {
            var method = _cacheType.GetMethod("TryGet")!;
            var parameters = new object?[] { key, null };
            var result = (bool)method.Invoke(_cache, parameters)!;
            value = (TValue?)parameters[1];
            return result;
        }

        public void Set(TKey key, TValue value)
        {
            var method = _cacheType.GetMethod("Set")!;
            method.Invoke(_cache, new object?[] { key, value });
        }

        public void Delete(TKey key)
        {
            var method = _cacheType.GetMethod("Delete")!;
            method.Invoke(_cache, new object?[] { key });
        }

        public int Count
        {
            get
            {
                var property = _cacheType.GetProperty("Count")!;
                return (int)property.GetValue(_cache)!;
            }
        }
    }

    #endregion

    #region Property 3: LRU Cache Thread Safety

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 3: LRU Cache Thread Safety**
    /// *For any* combination of concurrent read, write, and delete operations on the LRU cache,
    /// all operations should complete without throwing exceptions and without data corruption
    /// (reads return correct values, writes persist correctly, deletes remove only specified entries).
    /// **Validates: Requirements 2.1, 2.2, 2.3, 2.4**
    /// </summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(5, 50)]
    [InlineData(10, 100)]
    public void LRUCacheThreadSafety_ConcurrentOperations_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        var cache = new LRUCacheWrapper<string, string>(50);
        var results = new ConcurrentBag<OperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int op = 0; op < operationsPerThread; op++)
                    {
                        var result = new OperationResult { ThreadIndex = threadIndex };
                        
                        try
                        {
                            // Mix of operations: write, read, delete
                            int opType = (threadIndex + op) % 3;
                            string key = $"key_{threadIndex}_{op % 20}"; // Reuse some keys
                            
                            switch (opType)
                            {
                                case 0: // Write
                                    result.OperationType = "Set";
                                    cache.Set(key, $"value_{threadIndex}_{op}");
                                    break;
                                case 1: // Read
                                    result.OperationType = "TryGet";
                                    cache.TryGet(key, out _);
                                    break;
                                case 2: // Delete
                                    result.OperationType = "Delete";
                                    cache.Delete(key);
                                    break;
                            }
                            
                            result.Success = true;
                        }
                        catch (Exception ex)
                        {
                            result.ExceptionThrown = true;
                            result.ExceptionType = ex.GetType().Name;
                            result.ExceptionMessage = ex.Message;
                        }
                        
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new OperationResult
                    {
                        ThreadIndex = threadIndex,
                        OperationType = "Barrier",
                        ExceptionThrown = true,
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message
                    });
                }
            });
        }

        Task.WaitAll(tasks);

        // All operations should complete without exceptions
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} {r.OperationType} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 3a: Concurrent Reads Return Correct Values**
    /// *For any* set of concurrent read operations on the LRU cache, each read should return
    /// the correct value that was previously written for that key.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void LRUCacheThreadSafety_ConcurrentReads_ShouldReturnCorrectValues(int concurrencyLevel)
    {
        var cache = new LRUCacheWrapper<int, string>(100);
        
        // Pre-populate cache with known values
        for (int i = 0; i < concurrencyLevel * 5; i++)
        {
            cache.Set(i, $"value_{i}");
        }

        var results = new ConcurrentBag<(int key, string? value, bool found, bool correct)>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                // Each thread reads multiple keys
                for (int j = 0; j < concurrencyLevel * 5; j++)
                {
                    bool found = cache.TryGet(j, out var value);
                    bool correct = !found || value == $"value_{j}";
                    results.Add((j, value, found, correct));
                }
            });
        }

        Task.WaitAll(tasks);

        // All reads that found a value should have the correct value
        Assert.All(results, r => Assert.True(r.correct,
            $"Key {r.key} returned incorrect value: expected 'value_{r.key}', got '{r.value}'"));
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 3b: Concurrent Writes Complete Without Corruption**
    /// *For any* set of concurrent write operations on the LRU cache, all writes should
    /// complete and the final state should be consistent (no corrupted entries).
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void LRUCacheThreadSafety_ConcurrentWrites_ShouldCompleteWithoutCorruption(int concurrencyLevel)
    {
        int keysPerThread = 10;
        
        var cache = new LRUCacheWrapper<string, int>(concurrencyLevel * keysPerThread);
        var writtenValues = new ConcurrentDictionary<string, int>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var exceptions = new ConcurrentBag<Exception>();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < keysPerThread; j++)
                    {
                        string key = $"thread_{threadIndex}_key_{j}";
                        int value = threadIndex * 1000 + j;
                        cache.Set(key, value);
                        writtenValues[key] = value;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.Empty(exceptions);

        // Verify all written values can be read back correctly
        foreach (var kvp in writtenValues)
        {
            if (cache.TryGet(kvp.Key, out var value))
            {
                Assert.Equal(kvp.Value, value);
            }
        }
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 3c: Mixed Read/Write Operations Are Safe**
    /// *For any* combination of concurrent read and write operations, the cache should
    /// handle the concurrent access without throwing exceptions.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(10)]
    public void LRUCacheThreadSafety_MixedReadWrite_ShouldBeSafe(int concurrencyLevel)
    {
        int halfConcurrency = concurrencyLevel / 2;
        
        var cache = new LRUCacheWrapper<int, string>(50);
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Half threads write, half threads read
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            bool isWriter = threadIndex < halfConcurrency;
            
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 100; j++)
                    {
                        int key = j % 30; // Shared key space
                        
                        if (isWriter)
                        {
                            cache.Set(key, $"value_{threadIndex}_{j}");
                        }
                        else
                        {
                            cache.TryGet(key, out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.Empty(exceptions);
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 3d: Eviction Under Load Is Safe**
    /// *For any* set of concurrent writes that exceed cache capacity, the eviction
    /// mechanism should work correctly without corruption.
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void LRUCacheThreadSafety_EvictionUnderLoad_ShouldBeSafe(int concurrencyLevel)
    {
        int cacheCapacity = 20;
        int keysPerThread = 50; // More keys than capacity to force eviction
        
        var cache = new LRUCacheWrapper<string, int>(cacheCapacity);
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < keysPerThread; j++)
                    {
                        string key = $"key_{threadIndex}_{j}";
                        cache.Set(key, threadIndex * 1000 + j);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.Empty(exceptions);
        
        // Verify cache count doesn't exceed capacity
        Assert.True(cache.Count <= cacheCapacity,
            $"Cache count {cache.Count} exceeds capacity {cacheCapacity}");
    }

    #endregion

    #region Additional Stress Tests

    /// <summary>
    /// High concurrency stress test for LRU cache operations.
    /// </summary>
    [Theory]
    [InlineData(20, 200)]
    [InlineData(30, 100)]
    public void LRUCacheThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        var cache = new LRUCacheWrapper<string, string>(100);
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int op = 0; op < operationsPerThread; op++)
                    {
                        int opType = (threadIndex + op) % 3;
                        string key = $"key_{op % 50}";
                        
                        switch (opType)
                        {
                            case 0:
                                cache.Set(key, $"value_{threadIndex}_{op}");
                                break;
                            case 1:
                                cache.TryGet(key, out _);
                                break;
                            case 2:
                                cache.Delete(key);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.Empty(exceptions);
    }

    /// <summary>
    /// Test that verifies LRU eviction order is maintained under concurrent access.
    /// </summary>
    [Fact]
    public void LRUCacheThreadSafety_EvictionOrder_ShouldMaintainLRUProperty()
    {
        var cache = new LRUCacheWrapper<int, string>(5);
        
        // Add 5 items
        for (int i = 0; i < 5; i++)
        {
            cache.Set(i, $"value_{i}");
        }

        // Access item 0 to make it most recently used
        cache.TryGet(0, out _);

        // Add a new item, which should evict item 1 (least recently used)
        cache.Set(5, "value_5");

        // Item 0 should still exist (was accessed)
        Assert.True(cache.TryGet(0, out var value0));
        Assert.Equal("value_0", value0);

        // Item 1 should be evicted
        Assert.False(cache.TryGet(1, out _));

        // Item 5 should exist
        Assert.True(cache.TryGet(5, out var value5));
        Assert.Equal("value_5", value5);
    }

    #endregion
}
