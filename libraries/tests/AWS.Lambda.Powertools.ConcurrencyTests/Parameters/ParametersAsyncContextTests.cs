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
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Parameters;

/// <summary>
/// Tests for validating async context preservation in Powertools Parameters.
/// 
/// These tests verify that when async parameter retrieval operations are performed,
/// the correct context is preserved across await points, concurrent async invocations
/// maintain isolation, and ConfigureAwait(false) behavior works correctly.
/// 
/// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
/// **Validates: Requirements 6.1, 6.2, 6.3**
/// </summary>
[Collection("Parameters Async Context Tests")]
public class ParametersAsyncContextTests
{
    #region Helper Classes

    /// <summary>
    /// Mock parameter provider for testing async context preservation.
    /// Simulates realistic async delays to test context preservation across await points.
    /// </summary>
    private class AsyncMockParameterProvider : ParameterProvider
    {
        private readonly ConcurrentDictionary<string, string> _parameters = new();
        private readonly int _delayMs;
        private int _getCallCount;
        private int _getMultipleCallCount;

        public int GetCallCount => _getCallCount;
        public int GetMultipleCallCount => _getMultipleCallCount;

        public AsyncMockParameterProvider(int delayMs = 10)
        {
            _delayMs = delayMs;
        }

        public void SetParameter(string key, string value)
        {
            _parameters[key] = value;
        }

        protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
        {
            Interlocked.Increment(ref _getCallCount);
            
            // Simulate network delay with multiple await points to test context preservation
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs / 2).ConfigureAwait(false);
                await Task.Delay(_delayMs / 2).ConfigureAwait(false);
            }

            return _parameters.TryGetValue(key, out var value) ? value : null;
        }

        protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string key, ParameterProviderConfiguration? config)
        {
            Interlocked.Increment(ref _getMultipleCallCount);
            
            // Simulate network delay with multiple await points
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs / 2).ConfigureAwait(false);
                await Task.Delay(_delayMs / 2).ConfigureAwait(false);
            }

            var result = new Dictionary<string, string?>();
            foreach (var kvp in _parameters.Where(p => p.Key.StartsWith(key)))
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }
    }

    #endregion

    #region Property 7: Async Context Preservation - Across Await Points

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// *For any* async invocation with await points, the parameter retrieval should complete correctly
    /// and the context should be preserved after the await.
    /// **Validates: Requirements 6.1**
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task AsyncContextPreservation_AcrossAwaitPoints_ShouldPreserveContext(int awaitCount)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 10);
        var invocationId = Guid.NewGuid().ToString("N");
        var expectedValue = $"value_{invocationId}";
        provider.SetParameter($"key_{invocationId}", expectedValue);
        
        var valuesAtAwaitPoints = new List<string?>();
        var threadIdsAtAwaitPoints = new List<int>();

        // Act - perform multiple async operations and track context at each await point
        for (int i = 0; i < awaitCount; i++)
        {
            threadIdsAtAwaitPoints.Add(Environment.CurrentManagedThreadId);
            
            var value = await provider.GetAsync($"key_{invocationId}").ConfigureAwait(false);
            valuesAtAwaitPoints.Add(value);
            
            // Small delay between operations
            await Task.Delay(Random.Shared.Next(1, 5)).ConfigureAwait(false);
        }

        // Assert - all values should be correct regardless of thread changes
        Assert.All(valuesAtAwaitPoints, v => Assert.Equal(expectedValue, v));
        Assert.Equal(awaitCount, valuesAtAwaitPoints.Count);
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// *For any* async invocation, the parameter value should be correctly retrieved
    /// even when the operation spans multiple await points internally.
    /// **Validates: Requirements 6.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task AsyncContextPreservation_MultipleAwaitPointsInProvider_ShouldReturnCorrectValue(int operationCount)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 20); // Longer delay to ensure multiple await points
        var results = new ConcurrentBag<AsyncContextResult>();

        // Set up parameters
        for (int i = 0; i < operationCount; i++)
        {
            provider.SetParameter($"param_{i}", $"value_{i}");
        }

        // Act - perform operations sequentially to test context preservation
        for (int i = 0; i < operationCount; i++)
        {
            var result = new AsyncContextResult
            {
                InvocationId = Guid.NewGuid().ToString("N"),
                ThreadIndex = i,
                ThreadIdBeforeAwait = Environment.CurrentManagedThreadId
            };

            try
            {
                var value = await provider.GetAsync($"param_{i}").ConfigureAwait(false);
                
                result.ThreadIdAfterAwait = Environment.CurrentManagedThreadId;
                result.ContextPreserved = value == $"value_{i}";
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionType = ex.GetType().Name;
                result.ExceptionMessage = ex.Message;
            }

            results.Add(result);
        }

        // Assert
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown, $"Operation {r.ThreadIndex} threw exception: {r.ExceptionMessage}");
            Assert.True(r.ContextPreserved, $"Operation {r.ThreadIndex} did not preserve context");
        });
    }

    #endregion

    #region Property 7: Async Context Preservation - Concurrent Async Invocations

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// *For any* set of concurrent async invocations, each invocation should maintain isolation
    /// across await boundaries and return the correct value for its key.
    /// **Validates: Requirements 6.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task AsyncContextPreservation_ConcurrentAsyncInvocations_ShouldMaintainIsolation(int concurrencyLevel)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 15);
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up unique parameters for each invocation
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"concurrent_key_{i}", $"concurrent_value_{i}");
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new AsyncContextResult
                {
                    InvocationId = invocationId,
                    ThreadIndex = invocationIndex,
                    ThreadIdBeforeAwait = Environment.CurrentManagedThreadId
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple async operations to test isolation
                    for (int j = 0; j < 5; j++)
                    {
                        var value = await provider.GetAsync($"concurrent_key_{invocationIndex}").ConfigureAwait(false);
                        
                        if (value != $"concurrent_value_{invocationIndex}")
                        {
                            result.ContextPreserved = false;
                            result.ExceptionMessage = $"Expected 'concurrent_value_{invocationIndex}', got '{value}'";
                            results.Add(result);
                            return;
                        }
                        
                        // Small delay between operations
                        await Task.Delay(Random.Shared.Next(1, 5)).ConfigureAwait(false);
                    }

                    result.ThreadIdAfterAwait = Environment.CurrentManagedThreadId;
                    result.ContextPreserved = true;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown, $"Invocation {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.True(r.ContextPreserved, $"Invocation {r.ThreadIndex} did not maintain isolation: {r.ExceptionMessage}");
        });
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// *For any* set of concurrent async invocations performing mixed operations,
    /// each invocation should maintain isolation and return correct values.
    /// **Validates: Requirements 6.2**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(10)]
    public async Task AsyncContextPreservation_ConcurrentMixedAsyncOperations_ShouldMaintainIsolation(int concurrencyLevel)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 10);
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up parameters
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"mixed_key_{i}", $"mixed_value_{i}");
            provider.SetParameter($"/path_{i}/param1", $"multi_value1_{i}");
            provider.SetParameter($"/path_{i}/param2", $"multi_value2_{i}");
        }

        // Act - half threads do GetAsync, half do GetMultipleAsync
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            bool useGetMultiple = invocationIndex % 2 == 0;

            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new AsyncContextResult
                {
                    InvocationId = invocationId,
                    ThreadIndex = invocationIndex,
                    ThreadIdBeforeAwait = Environment.CurrentManagedThreadId
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 5; j++)
                    {
                        if (useGetMultiple)
                        {
                            var values = await provider.GetMultipleAsync($"/path_{invocationIndex}").ConfigureAwait(false);
                            
                            // Verify we got the expected values
                            if (values.Count < 2)
                            {
                                result.ContextPreserved = false;
                                result.ExceptionMessage = $"Expected at least 2 values, got {values.Count}";
                                results.Add(result);
                                return;
                            }
                        }
                        else
                        {
                            var value = await provider.GetAsync($"mixed_key_{invocationIndex}").ConfigureAwait(false);
                            
                            if (value != $"mixed_value_{invocationIndex}")
                            {
                                result.ContextPreserved = false;
                                result.ExceptionMessage = $"Expected 'mixed_value_{invocationIndex}', got '{value}'";
                                results.Add(result);
                                return;
                            }
                        }

                        await Task.Delay(Random.Shared.Next(1, 5)).ConfigureAwait(false);
                    }

                    result.ThreadIdAfterAwait = Environment.CurrentManagedThreadId;
                    result.ContextPreserved = true;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown, $"Invocation {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.True(r.ContextPreserved, $"Invocation {r.ThreadIndex} did not maintain isolation: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Property 7: Async Context Preservation - ConfigureAwait(false) Behavior

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// *For any* async operation using ConfigureAwait(false), the parameter retrieval
    /// should continue to function correctly and return the expected value.
    /// **Validates: Requirements 6.3**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task AsyncContextPreservation_WithConfigureAwaitFalse_ShouldFunctionCorrectly(int concurrencyLevel)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 15);
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up parameters
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"configawait_key_{i}", $"configawait_value_{i}");
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new AsyncContextResult
                {
                    InvocationId = invocationId,
                    ThreadIndex = invocationIndex,
                    ThreadIdBeforeAwait = Environment.CurrentManagedThreadId
                };

                try
                {
                    barrier.SignalAndWait();

                    // First operation with ConfigureAwait(false)
                    var value1 = await provider.GetAsync($"configawait_key_{invocationIndex}").ConfigureAwait(false);
                    
                    // Delay with ConfigureAwait(false)
                    await Task.Delay(Random.Shared.Next(10, 30)).ConfigureAwait(false);
                    
                    // Second operation with ConfigureAwait(false)
                    var value2 = await provider.GetAsync($"configawait_key_{invocationIndex}").ConfigureAwait(false);
                    
                    // Another delay with ConfigureAwait(false)
                    await Task.Delay(Random.Shared.Next(10, 30)).ConfigureAwait(false);
                    
                    // Third operation with ConfigureAwait(false)
                    var value3 = await provider.GetAsync($"configawait_key_{invocationIndex}").ConfigureAwait(false);

                    result.ThreadIdAfterAwait = Environment.CurrentManagedThreadId;
                    
                    var expectedValue = $"configawait_value_{invocationIndex}";
                    result.ContextPreserved = value1 == expectedValue && 
                                              value2 == expectedValue && 
                                              value3 == expectedValue;
                    
                    if (!result.ContextPreserved)
                    {
                        result.ExceptionMessage = $"Values mismatch: v1='{value1}', v2='{value2}', v3='{value3}', expected='{expectedValue}'";
                    }
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown, $"Invocation {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.True(r.ContextPreserved, $"Invocation {r.ThreadIndex} failed with ConfigureAwait(false): {r.ExceptionMessage}");
        });
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// *For any* nested async operation using ConfigureAwait(false), the parameter retrieval
    /// should continue to function correctly at all nesting levels.
    /// **Validates: Requirements 6.3**
    /// </summary>
    [Theory]
    [InlineData(2, 2)]
    [InlineData(5, 3)]
    [InlineData(10, 2)]
    public async Task AsyncContextPreservation_NestedAsyncWithConfigureAwaitFalse_ShouldFunctionCorrectly(int concurrencyLevel, int nestingDepth)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 10);
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up parameters
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"nested_key_{i}", $"nested_value_{i}");
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new AsyncContextResult
                {
                    InvocationId = invocationId,
                    ThreadIndex = invocationIndex,
                    ThreadIdBeforeAwait = Environment.CurrentManagedThreadId
                };

                try
                {
                    barrier.SignalAndWait();

                    var valuesAtDepths = new List<string?>();
                    var expectedValue = $"nested_value_{invocationIndex}";

                    async Task NestedAsync(int depth)
                    {
                        if (depth <= 0) return;

                        await Task.Delay(Random.Shared.Next(1, 5)).ConfigureAwait(false);
                        
                        var value = await provider.GetAsync($"nested_key_{invocationIndex}").ConfigureAwait(false);
                        lock (valuesAtDepths)
                        {
                            valuesAtDepths.Add(value);
                        }

                        await NestedAsync(depth - 1).ConfigureAwait(false);
                    }

                    await NestedAsync(nestingDepth).ConfigureAwait(false);

                    result.ThreadIdAfterAwait = Environment.CurrentManagedThreadId;
                    result.ContextPreserved = valuesAtDepths.All(v => v == expectedValue);
                    
                    if (!result.ContextPreserved)
                    {
                        result.ExceptionMessage = $"Values at depths: [{string.Join(", ", valuesAtDepths)}], expected: '{expectedValue}'";
                    }
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown, $"Invocation {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.True(r.ContextPreserved, $"Invocation {r.ThreadIndex} failed in nested async: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region High Concurrency Stress Tests

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// High concurrency stress test for async context preservation.
    /// **Validates: Requirements 6.1, 6.2, 6.3**
    /// </summary>
    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    public async Task AsyncContextPreservation_HighConcurrency_ShouldPreserveContext(int concurrencyLevel)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 5);
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up parameters
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"stress_key_{i}", $"stress_value_{i}");
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new AsyncContextResult
                {
                    InvocationId = invocationId,
                    ThreadIndex = invocationIndex,
                    ThreadIdBeforeAwait = Environment.CurrentManagedThreadId
                };

                try
                {
                    barrier.SignalAndWait();

                    var expectedValue = $"stress_value_{invocationIndex}";
                    
                    // Perform multiple async operations with varying patterns
                    for (int j = 0; j < 10; j++)
                    {
                        var value = await provider.GetAsync($"stress_key_{invocationIndex}").ConfigureAwait(false);
                        
                        if (value != expectedValue)
                        {
                            result.ContextPreserved = false;
                            result.ExceptionMessage = $"Iteration {j}: Expected '{expectedValue}', got '{value}'";
                            results.Add(result);
                            return;
                        }

                        await Task.Delay(Random.Shared.Next(1, 5)).ConfigureAwait(false);
                    }

                    result.ThreadIdAfterAwait = Environment.CurrentManagedThreadId;
                    result.ContextPreserved = true;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown, $"Invocation {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.True(r.ContextPreserved, $"Invocation {r.ThreadIndex} did not preserve context under stress: {r.ExceptionMessage}");
        });
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 7: Async Context Preservation**
    /// Tests that Task.Run with async operations preserves correct parameter values.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Theory]
    [InlineData(5, 3)]
    [InlineData(10, 2)]
    public async Task AsyncContextPreservation_TaskRunWithAsyncOperations_ShouldPreserveContext(int concurrencyLevel, int taskRunsPerInvocation)
    {
        // Arrange
        var provider = new AsyncMockParameterProvider(delayMs: 10);
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up parameters
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"taskrun_key_{i}", $"taskrun_value_{i}");
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new AsyncContextResult
                {
                    InvocationId = invocationId,
                    ThreadIndex = invocationIndex,
                    ThreadIdBeforeAwait = Environment.CurrentManagedThreadId
                };

                try
                {
                    barrier.SignalAndWait();

                    var expectedValue = $"taskrun_value_{invocationIndex}";
                    var innerResults = new ConcurrentBag<string?>();

                    // Spawn multiple Task.Run operations
                    var innerTasks = new Task[taskRunsPerInvocation];
                    for (int j = 0; j < taskRunsPerInvocation; j++)
                    {
                        innerTasks[j] = Task.Run(async () =>
                        {
                            var value = await provider.GetAsync($"taskrun_key_{invocationIndex}").ConfigureAwait(false);
                            innerResults.Add(value);
                        });
                    }

                    await Task.WhenAll(innerTasks).ConfigureAwait(false);

                    result.ThreadIdAfterAwait = Environment.CurrentManagedThreadId;
                    result.ContextPreserved = innerResults.All(v => v == expectedValue);
                    
                    if (!result.ContextPreserved)
                    {
                        result.ExceptionMessage = $"Inner results: [{string.Join(", ", innerResults)}], expected: '{expectedValue}'";
                    }
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown, $"Invocation {r.ThreadIndex} threw exception: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.True(r.ContextPreserved, $"Invocation {r.ThreadIndex} failed with Task.Run: {r.ExceptionMessage}");
        });
    }

    #endregion
}
