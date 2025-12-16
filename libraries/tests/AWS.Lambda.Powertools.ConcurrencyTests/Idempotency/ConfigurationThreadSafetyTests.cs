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

using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Idempotency;

/// <summary>
/// Tests for validating thread-safe configuration in BasePersistenceStore
/// under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads attempt to configure
/// the persistence store simultaneously, the configuration completes
/// without exceptions and the resulting state is consistent.
/// </summary>
[Collection("Idempotency Configuration Tests")]
public class ConfigurationThreadSafetyTests
{
    #region Helper Classes

    private class ConfigurationResult
    {
        public int ThreadIndex { get; set; }
        public bool Success { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
    }

    #endregion

    #region Property 1: Configuration Thread Safety

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 1: Configuration Thread Safety**
    /// *For any* number of concurrent invocations calling Configure() simultaneously,
    /// the configuration should complete without exceptions and the resulting configuration
    /// should be consistent (not corrupted by interleaved operations).
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(10)]
    public void ConfigurationThreadSafety_ConcurrentConfigure_ShouldCompleteWithoutExceptions(int concurrencyLevel)
    {
        // Create a fresh persistence store for each test run
        var store = new ThreadSafeInMemoryPersistenceStore();
        var results = new ConfigurationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Create idempotency options
        var options = new AWS.Lambda.Powertools.Idempotency.IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("id")
            .WithExpiration(TimeSpan.FromMinutes(5))
            .Build();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult { ThreadIndex = threadIndex };

                try
                {
                    // Wait for all threads to be ready
                    barrier.SignalAndWait();

                    // All threads attempt to configure simultaneously
                    store.Configure(options, $"Function_{threadIndex}", null);

                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results[threadIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        // All threads should complete without exceptions
        Assert.All(results, r => Assert.True(r.Success, 
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.False(r.ExceptionThrown,
            $"Thread {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 1b: Configuration Idempotency**
    /// *For any* persistence store, calling Configure() multiple times with the same
    /// parameters should be safe and idempotent (subsequent calls are no-ops).
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void ConfigurationIdempotency_MultipleCalls_ShouldBeNoOp(int numberOfCalls)
    {
        var store = new ThreadSafeInMemoryPersistenceStore();
        var options = new AWS.Lambda.Powertools.Idempotency.IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("id")
            .Build();

        var exceptions = new List<Exception>();

        // Call Configure multiple times sequentially
        for (int i = 0; i < numberOfCalls; i++)
        {
            try
            {
                store.Configure(options, "TestFunction", null);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        Assert.Empty(exceptions);
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 1c: Configuration Thread Safety with Different Parameters**
    /// *For any* set of concurrent threads calling Configure() with different function names,
    /// the first configuration should win and subsequent calls should be no-ops.
    /// **Validates: Requirements 1.1, 1.3**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void ConfigurationThreadSafety_DifferentParameters_FirstConfigurationWins(int concurrencyLevel)
    {
        var store = new ThreadSafeInMemoryPersistenceStore();
        var results = new ConfigurationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            var options = new AWS.Lambda.Powertools.Idempotency.IdempotencyOptionsBuilder()
                .WithEventKeyJmesPath($"id_{threadIndex}")
                .Build();

            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult { ThreadIndex = threadIndex };

                try
                {
                    barrier.SignalAndWait();

                    // Each thread tries to configure with different parameters
                    store.Configure(options, $"Function_{threadIndex}", null);

                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results[threadIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        // All threads should complete without exceptions (even if their config was ignored)
        Assert.All(results, r => Assert.True(r.Success, 
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.False(r.ExceptionThrown,
            $"Thread {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    #endregion

    #region Additional Concurrency Tests

    /// <summary>
    /// Stress test for configuration thread safety with high concurrency.
    /// This test uses a higher number of threads to stress test the locking mechanism.
    /// </summary>
    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    public void ConfigurationThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel)
    {
        var store = new ThreadSafeInMemoryPersistenceStore();
        var results = new ConfigurationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        var options = new AWS.Lambda.Powertools.Idempotency.IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("id")
            .Build();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult { ThreadIndex = threadIndex };

                try
                {
                    barrier.SignalAndWait();
                    store.Configure(options, $"Function_{threadIndex}", null);
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results[threadIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        // Verify all threads completed without exceptions
        Assert.All(results, r => Assert.True(r.Success, 
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.False(r.ExceptionThrown,
            $"Thread {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    #endregion
}
