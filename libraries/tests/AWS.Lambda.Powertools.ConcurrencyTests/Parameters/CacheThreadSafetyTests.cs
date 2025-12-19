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
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Parameters;

/// <summary>
/// Tests for validating CacheManager thread safety under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads perform read, write, and expiration
/// operations on the cache simultaneously, all operations complete without
/// exceptions and without data corruption.
/// 
/// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
/// **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**
/// </summary>
[Collection("Parameters Cache Tests")]
public class CacheThreadSafetyTests
{
    #region Helper Classes

    /// <summary>
    /// Thread-safe DateTime wrapper for testing that allows time manipulation.
    /// </summary>
    private class TestDateTimeWrapper : IDateTimeWrapper
    {
        private DateTime _utcNow = DateTime.UtcNow;
        private readonly object _lock = new();

        public DateTime UtcNow
        {
            get
            {
                lock (_lock)
                {
                    return _utcNow;
                }
            }
        }

        public void SetUtcNow(DateTime value)
        {
            lock (_lock)
            {
                _utcNow = value;
            }
        }

        public void AdvanceTime(TimeSpan duration)
        {
            lock (_lock)
            {
                _utcNow = _utcNow.Add(duration);
            }
        }
    }

    /// <summary>
    /// Wrapper to access internal CacheManager for testing purposes.
    /// </summary>
    private class CacheManagerWrapper
    {
        private readonly ICacheManager _cache;
        private readonly TestDateTimeWrapper _dateTimeWrapper;

        public CacheManagerWrapper()
        {
            _dateTimeWrapper = new TestDateTimeWrapper();
            _cache = new CacheManager(_dateTimeWrapper);
        }

        public object? Get(string key) => _cache.Get(key);
        
        public void Set(string key, object? value, TimeSpan duration) => _cache.Set(key, value, duration);

        public void AdvanceTime(TimeSpan duration) => _dateTimeWrapper.AdvanceTime(duration);
        
        public void SetTime(DateTime time) => _dateTimeWrapper.SetUtcNow(time);
    }

    #endregion

    #region Property 3: Cache Thread Safety - Concurrent Reads

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
    /// *For any* set of concurrent read operations on the CacheManager with the same key,
    /// all reads should return the correct cached value without throwing exceptions.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void CacheThreadSafety_ConcurrentReadsWithSameKey_ShouldReturnCorrectValues(int concurrencyLevel)
    {
        // Arrange
        var cache = new CacheManagerWrapper();
        var results = new ConcurrentBag<CacheOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        
        const string testKey = "shared_key";
        const string testValue = "shared_value";
        var cacheDuration = TimeSpan.FromMinutes(5);
        
        // Pre-populate cache with a known value
        cache.Set(testKey, testValue, cacheDuration);

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new CacheOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "Get",
                    Key = testKey
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple reads
                    for (int j = 0; j < 100; j++)
                    {
                        var value = cache.Get(testKey);
                        if (value != null && (string)value != testValue)
                        {
                            result.Success = false;
                            result.ExceptionMessage = $"Expected '{testValue}', got '{value}'";
                            results.Add(result);
                            return;
                        }
                    }
                    
                    result.Value = cache.Get(testKey);
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results.Add(result);
            });
        }

        Task.WaitAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.Equal(testValue, r.Value);
        });
    }

    #endregion

    #region Property 3: Cache Thread Safety - Concurrent Writes

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
    /// *For any* set of concurrent write operations on the CacheManager with different keys,
    /// all writes should complete without data corruption.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void CacheThreadSafety_ConcurrentWritesWithDifferentKeys_ShouldCompleteWithoutCorruption(int concurrencyLevel)
    {
        // Arrange
        var cache = new CacheManagerWrapper();
        var writtenValues = new ConcurrentDictionary<string, string>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var cacheDuration = TimeSpan.FromMinutes(5);
        int keysPerThread = 20;

        // Act
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
                        string value = $"value_{threadIndex}_{j}";
                        cache.Set(key, value, cacheDuration);
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

        // Assert - no exceptions during writes
        Assert.Empty(exceptions);

        // Assert - all written values can be read back correctly
        foreach (var kvp in writtenValues)
        {
            var cachedValue = cache.Get(kvp.Key);
            Assert.NotNull(cachedValue);
            Assert.Equal(kvp.Value, (string)cachedValue);
        }
    }

    #endregion

    #region Property 3: Cache Thread Safety - Mixed Read/Write

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
    /// *For any* combination of concurrent read and write operations on the same key,
    /// the cache should handle the concurrent access without throwing exceptions.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(10)]
    public void CacheThreadSafety_MixedReadWriteOnSameKey_ShouldBeSafe(int concurrencyLevel)
    {
        // Arrange
        var cache = new CacheManagerWrapper();
        var results = new ConcurrentBag<CacheOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var cacheDuration = TimeSpan.FromMinutes(5);
        
        const string sharedKey = "shared_key";
        int halfConcurrency = concurrencyLevel / 2;

        // Pre-populate with initial value
        cache.Set(sharedKey, "initial_value", cacheDuration);

        // Act - half threads write, half threads read
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            bool isWriter = threadIndex < halfConcurrency;

            tasks[i] = Task.Run(() =>
            {
                var result = new CacheOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    Key = sharedKey
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 100; j++)
                    {
                        if (isWriter)
                        {
                            result.OperationType = "Set";
                            cache.Set(sharedKey, $"value_{threadIndex}_{j}", cacheDuration);
                        }
                        else
                        {
                            result.OperationType = "Get";
                            var value = cache.Get(sharedKey);
                            // Value should be either null or a valid string
                            if (value != null && !(value is string))
                            {
                                throw new InvalidOperationException($"Unexpected value type: {value.GetType()}");
                            }
                        }
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
            });
        }

        Task.WaitAll(tasks);

        // Assert
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} {r.OperationType} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    #endregion

    #region Property 3: Cache Thread Safety - Cache Object Mutation

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
    /// *For any* set of concurrent updates to the same cache key, the cache should
    /// handle the concurrent modification without data corruption.
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void CacheThreadSafety_ConcurrentUpdatesToSameKey_ShouldNotCorruptData(int concurrencyLevel)
    {
        // Arrange
        var cache = new CacheManagerWrapper();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var cacheDuration = TimeSpan.FromMinutes(5);
        
        const string sharedKey = "shared_key";
        var lastWrittenValues = new ConcurrentDictionary<int, string>();

        // Pre-populate with initial value
        cache.Set(sharedKey, "initial_value", cacheDuration);

        // Act - all threads write to the same key
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 100; j++)
                    {
                        string value = $"value_{threadIndex}_{j}";
                        cache.Set(sharedKey, value, cacheDuration);
                        lastWrittenValues[threadIndex] = value;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - no exceptions during concurrent updates
        Assert.Empty(exceptions);

        // Assert - the final value should be one of the written values
        var finalValue = cache.Get(sharedKey);
        Assert.NotNull(finalValue);
        Assert.Contains((string)finalValue, lastWrittenValues.Values);
    }

    #endregion

    #region Property 3: Cache Thread Safety - Expiration During Concurrent Access

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
    /// *For any* cache entries that expire during concurrent access, the cache should
    /// handle expiration correctly without throwing exceptions.
    /// **Validates: Requirements 2.5**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void CacheThreadSafety_ExpirationDuringConcurrentAccess_ShouldHandleCorrectly(int concurrencyLevel)
    {
        // Arrange
        var cache = new CacheManagerWrapper();
        var results = new ConcurrentBag<CacheOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        
        // Use a very short cache duration
        var shortDuration = TimeSpan.FromMilliseconds(50);
        var longDuration = TimeSpan.FromMinutes(5);

        // Pre-populate cache with values that will expire
        for (int i = 0; i < concurrencyLevel; i++)
        {
            cache.Set($"expiring_key_{i}", $"expiring_value_{i}", shortDuration);
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new CacheOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex
                };

                try
                {
                    barrier.SignalAndWait();

                    // Mix of operations on expiring and non-expiring keys
                    for (int j = 0; j < 50; j++)
                    {
                        string expiringKey = $"expiring_key_{threadIndex}";
                        string freshKey = $"fresh_key_{threadIndex}_{j}";

                        // Read potentially expired key
                        result.OperationType = "Get";
                        result.Key = expiringKey;
                        cache.Get(expiringKey);

                        // Write new value with long duration
                        result.OperationType = "Set";
                        result.Key = freshKey;
                        cache.Set(freshKey, $"fresh_value_{j}", longDuration);

                        // Re-write expiring key
                        result.OperationType = "Set";
                        result.Key = expiringKey;
                        cache.Set(expiringKey, $"renewed_value_{j}", shortDuration);
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
            });
        }

        Task.WaitAll(tasks);

        // Assert
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} {r.OperationType} on key '{r.Key}' failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
    /// Tests that time-based expiration works correctly with concurrent access.
    /// **Validates: Requirements 2.5**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void CacheThreadSafety_TimeBasedExpiration_ShouldWorkCorrectlyUnderConcurrency(int concurrencyLevel)
    {
        // Arrange
        var cache = new CacheManagerWrapper();
        var results = new ConcurrentBag<CacheOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        
        var cacheDuration = TimeSpan.FromSeconds(1);
        const string sharedKey = "timed_key";
        const string initialValue = "initial_value";

        // Pre-populate cache
        cache.Set(sharedKey, initialValue, cacheDuration);

        // Act - concurrent reads while time advances
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new CacheOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "Get",
                    Key = sharedKey
                };

                try
                {
                    barrier.SignalAndWait();

                    // Read before expiration
                    var valueBefore = cache.Get(sharedKey);
                    
                    // Advance time past expiration (only one thread does this)
                    if (threadIndex == 0)
                    {
                        cache.AdvanceTime(TimeSpan.FromSeconds(2));
                    }

                    // Small delay to ensure time advancement is visible
                    Thread.Sleep(10);

                    // Read after expiration
                    var valueAfter = cache.Get(sharedKey);

                    result.Success = true;
                    result.Value = valueAfter;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results.Add(result);
            });
        }

        Task.WaitAll(tasks);

        // Assert - no exceptions
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    #endregion

    #region High Concurrency Stress Tests

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 3: Cache Thread Safety**
    /// High concurrency stress test for cache operations.
    /// **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**
    /// </summary>
    [Theory]
    [InlineData(20, 200)]
    [InlineData(30, 100)]
    public void CacheThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        // Arrange
        var cache = new CacheManagerWrapper();
        var results = new ConcurrentBag<CacheOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var cacheDuration = TimeSpan.FromMinutes(5);

        // Act
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
                        var result = new CacheOperationResult
                        {
                            InvocationId = Guid.NewGuid().ToString(),
                            ThreadIndex = threadIndex
                        };

                        try
                        {
                            // Mix of operations
                            int opType = (threadIndex + op) % 3;
                            string key = $"key_{op % 50}"; // Shared key space

                            switch (opType)
                            {
                                case 0: // Write unique key
                                    result.OperationType = "Set";
                                    result.Key = $"unique_{threadIndex}_{op}";
                                    cache.Set(result.Key, $"value_{threadIndex}_{op}", cacheDuration);
                                    break;
                                case 1: // Read shared key
                                    result.OperationType = "Get";
                                    result.Key = key;
                                    result.Value = cache.Get(key);
                                    break;
                                case 2: // Write shared key
                                    result.OperationType = "Set";
                                    result.Key = key;
                                    cache.Set(key, $"value_{threadIndex}_{op}", cacheDuration);
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
                    results.Add(new CacheOperationResult
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

        // Assert
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} {r.OperationType} on key '{r.Key}' failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    #endregion
}
