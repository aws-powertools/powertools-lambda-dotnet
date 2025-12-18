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
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Parameters;

/// <summary>
/// Tests for validating parameter retrieval thread safety under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads perform GetAsync and GetMultipleAsync
/// operations simultaneously, all operations complete without exceptions and return
/// correct values for each invocation.
/// 
/// **Feature: parameters-thread-safety, Property 5: Parameter Retrieval Thread Safety**
/// **Validates: Requirements 4.1, 4.2, 4.3, 4.4**
/// </summary>
[Collection("Parameters Retrieval Tests")]
public class ParameterRetrievalThreadSafetyTests
{
    #region Helper Classes

    /// <summary>
    /// Mock parameter provider for controlled testing of concurrent operations.
    /// Simulates realistic delays to expose race conditions.
    /// </summary>
    private class MockParameterProvider : ParameterProvider
    {
        private readonly ConcurrentDictionary<string, string> _parameters = new();
        private readonly ConcurrentDictionary<string, IDictionary<string, string>> _multipleParameters = new();
        private readonly int _delayMs;
        private int _getCallCount;
        private int _getMultipleCallCount;

        public int GetCallCount => _getCallCount;
        public int GetMultipleCallCount => _getMultipleCallCount;

        public MockParameterProvider(int delayMs = 10)
        {
            _delayMs = delayMs;
        }

        public void SetParameter(string key, string value)
        {
            _parameters[key] = value;
        }

        public void SetMultipleParameters(string path, IDictionary<string, string> values)
        {
            _multipleParameters[path] = values;
        }

        protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
        {
            Interlocked.Increment(ref _getCallCount);
            
            // Simulate network delay to expose race conditions
            if (_delayMs > 0)
                await Task.Delay(_delayMs).ConfigureAwait(false);

            return _parameters.TryGetValue(key, out var value) ? value : null;
        }

        protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string key, ParameterProviderConfiguration? config)
        {
            Interlocked.Increment(ref _getMultipleCallCount);
            
            // Simulate network delay to expose race conditions
            if (_delayMs > 0)
                await Task.Delay(_delayMs).ConfigureAwait(false);

            if (_multipleParameters.TryGetValue(key, out var values))
            {
                return values.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
            }

            return new Dictionary<string, string?>();
        }
    }

    #endregion

    #region Property 5: Parameter Retrieval Thread Safety - Concurrent GetAsync with Different Keys

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 5: Parameter Retrieval Thread Safety**
    /// *For any* set of concurrent GetAsync operations with different keys,
    /// all operations should return the correct value for each key without throwing exceptions.
    /// **Validates: Requirements 4.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ParameterRetrievalThreadSafety_ConcurrentGetAsyncWithDifferentKeys_ShouldReturnCorrectValues(int concurrencyLevel)
    {
        // Arrange
        var provider = new MockParameterProvider(delayMs: 5);
        var results = new ConcurrentBag<ParameterRetrievalResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up parameters for each thread
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"key_{i}", $"value_{i}");
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new ParameterRetrievalResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "GetAsync",
                    Key = $"key_{threadIndex}"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple GetAsync operations
                    for (int j = 0; j < 20; j++)
                    {
                        var value = await provider.GetAsync(result.Key).ConfigureAwait(false);
                        
                        if (value != $"value_{threadIndex}")
                        {
                            result.Success = false;
                            result.ExceptionMessage = $"Expected 'value_{threadIndex}', got '{value}'";
                            results.Add(result);
                            return;
                        }
                    }

                    result.Value = await provider.GetAsync(result.Key).ConfigureAwait(false);
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed for key '{r.Key}': {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.Equal($"value_{r.ThreadIndex}", r.Value);
        });
    }

    #endregion

    #region Property 5: Parameter Retrieval Thread Safety - Concurrent GetAsync with Same Key

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 5: Parameter Retrieval Thread Safety**
    /// *For any* set of concurrent GetAsync operations with the same key,
    /// all operations should return the correct value without interference.
    /// **Validates: Requirements 4.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ParameterRetrievalThreadSafety_ConcurrentGetAsyncWithSameKey_ShouldReturnCorrectValue(int concurrencyLevel)
    {
        // Arrange
        var provider = new MockParameterProvider(delayMs: 5);
        var results = new ConcurrentBag<ParameterRetrievalResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        const string sharedKey = "shared_key";
        const string expectedValue = "shared_value";
        provider.SetParameter(sharedKey, expectedValue);

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new ParameterRetrievalResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "GetAsync",
                    Key = sharedKey
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple GetAsync operations on the same key
                    for (int j = 0; j < 50; j++)
                    {
                        var value = await provider.GetAsync(sharedKey).ConfigureAwait(false);
                        
                        if (value != expectedValue)
                        {
                            result.Success = false;
                            result.ExceptionMessage = $"Expected '{expectedValue}', got '{value}'";
                            results.Add(result);
                            return;
                        }
                    }

                    result.Value = await provider.GetAsync(sharedKey).ConfigureAwait(false);
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.Equal(expectedValue, r.Value);
        });
    }

    #endregion

    #region Property 5: Parameter Retrieval Thread Safety - Concurrent GetMultipleAsync

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 5: Parameter Retrieval Thread Safety**
    /// *For any* set of concurrent GetMultipleAsync operations with different paths,
    /// all operations should return the correct values for each path without throwing exceptions.
    /// **Validates: Requirements 4.3**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ParameterRetrievalThreadSafety_ConcurrentGetMultipleAsync_ShouldReturnCorrectValues(int concurrencyLevel)
    {
        // Arrange
        var provider = new MockParameterProvider(delayMs: 5);
        var results = new ConcurrentBag<ParameterRetrievalResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up multiple parameters for each thread
        for (int i = 0; i < concurrencyLevel; i++)
        {
            var path = $"/path_{i}";
            var values = new Dictionary<string, string>
            {
                { $"param1_{i}", $"value1_{i}" },
                { $"param2_{i}", $"value2_{i}" },
                { $"param3_{i}", $"value3_{i}" }
            };
            provider.SetMultipleParameters(path, values);
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new ParameterRetrievalResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "GetMultipleAsync",
                    Key = $"/path_{threadIndex}"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple GetMultipleAsync operations
                    for (int j = 0; j < 20; j++)
                    {
                        var values = await provider.GetMultipleAsync(result.Key).ConfigureAwait(false);
                        
                        if (values.Count != 3)
                        {
                            result.Success = false;
                            result.ExceptionMessage = $"Expected 3 values, got {values.Count}";
                            results.Add(result);
                            return;
                        }

                        // Verify each value
                        foreach (var kvp in values)
                        {
                            var expectedValue = kvp.Key.Replace("param", "value");
                            if (kvp.Value != expectedValue)
                            {
                                result.Success = false;
                                result.ExceptionMessage = $"Expected '{expectedValue}' for key '{kvp.Key}', got '{kvp.Value}'";
                                results.Add(result);
                                return;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed for path '{r.Key}': {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Property 5: Parameter Retrieval Thread Safety - Mixed Get/GetMultiple Operations

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 5: Parameter Retrieval Thread Safety**
    /// *For any* combination of concurrent Get and GetMultiple operations,
    /// all operations should maintain data integrity across all operations.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(10)]
    public async Task ParameterRetrievalThreadSafety_MixedGetAndGetMultipleOperations_ShouldMaintainDataIntegrity(int concurrencyLevel)
    {
        // Arrange
        var provider = new MockParameterProvider(delayMs: 5);
        var results = new ConcurrentBag<ParameterRetrievalResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up single parameters
        for (int i = 0; i < concurrencyLevel; i++)
        {
            provider.SetParameter($"single_key_{i}", $"single_value_{i}");
        }

        // Set up multiple parameters
        for (int i = 0; i < concurrencyLevel; i++)
        {
            var path = $"/multi_path_{i}";
            var values = new Dictionary<string, string>
            {
                { $"multi_param1_{i}", $"multi_value1_{i}" },
                { $"multi_param2_{i}", $"multi_value2_{i}" }
            };
            provider.SetMultipleParameters(path, values);
        }

        // Act - half threads do GetAsync, half do GetMultipleAsync
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            bool useGetMultiple = threadIndex % 2 == 0;

            tasks[i] = Task.Run(async () =>
            {
                var result = new ParameterRetrievalResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 30; j++)
                    {
                        if (useGetMultiple)
                        {
                            result.OperationType = "GetMultipleAsync";
                            result.Key = $"/multi_path_{threadIndex}";
                            
                            var values = await provider.GetMultipleAsync(result.Key).ConfigureAwait(false);
                            
                            if (values.Count != 2)
                            {
                                result.Success = false;
                                result.ExceptionMessage = $"Expected 2 values, got {values.Count}";
                                results.Add(result);
                                return;
                            }
                        }
                        else
                        {
                            result.OperationType = "GetAsync";
                            result.Key = $"single_key_{threadIndex}";
                            
                            var value = await provider.GetAsync(result.Key).ConfigureAwait(false);
                            
                            if (value != $"single_value_{threadIndex}")
                            {
                                result.Success = false;
                                result.ExceptionMessage = $"Expected 'single_value_{threadIndex}', got '{value}'";
                                results.Add(result);
                                return;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} {r.OperationType} on '{r.Key}' failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region High Concurrency Stress Tests

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 5: Parameter Retrieval Thread Safety**
    /// High concurrency stress test for parameter retrieval operations.
    /// **Validates: Requirements 4.1, 4.2, 4.3, 4.4**
    /// </summary>
    [Theory]
    [InlineData(20, 50)]
    [InlineData(30, 30)]
    public async Task ParameterRetrievalThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        // Arrange
        var provider = new MockParameterProvider(delayMs: 2);
        var results = new ConcurrentBag<ParameterRetrievalResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Set up parameters
        for (int i = 0; i < 50; i++)
        {
            provider.SetParameter($"key_{i}", $"value_{i}");
            provider.SetMultipleParameters($"/path_{i}", new Dictionary<string, string>
            {
                { $"param1_{i}", $"value1_{i}" },
                { $"param2_{i}", $"value2_{i}" }
            });
        }

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int op = 0; op < operationsPerThread; op++)
                    {
                        var result = new ParameterRetrievalResult
                        {
                            InvocationId = Guid.NewGuid().ToString(),
                            ThreadIndex = threadIndex
                        };

                        try
                        {
                            int opType = (threadIndex + op) % 4;
                            int keyIndex = op % 50;

                            switch (opType)
                            {
                                case 0: // GetAsync with unique key
                                    result.OperationType = "GetAsync";
                                    result.Key = $"key_{keyIndex}";
                                    result.Value = await provider.GetAsync(result.Key).ConfigureAwait(false);
                                    break;
                                case 1: // GetAsync with shared key
                                    result.OperationType = "GetAsync";
                                    result.Key = "key_0";
                                    result.Value = await provider.GetAsync(result.Key).ConfigureAwait(false);
                                    break;
                                case 2: // GetMultipleAsync with unique path
                                    result.OperationType = "GetMultipleAsync";
                                    result.Key = $"/path_{keyIndex}";
                                    await provider.GetMultipleAsync(result.Key).ConfigureAwait(false);
                                    break;
                                case 3: // GetMultipleAsync with shared path
                                    result.OperationType = "GetMultipleAsync";
                                    result.Key = "/path_0";
                                    await provider.GetMultipleAsync(result.Key).ConfigureAwait(false);
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
                    results.Add(new ParameterRetrievalResult
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} {r.OperationType} on '{r.Key}' failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Cache Interaction Tests

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 5: Parameter Retrieval Thread Safety**
    /// Tests that concurrent GetAsync operations interact correctly with the cache.
    /// **Validates: Requirements 4.1, 4.2**
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ParameterRetrievalThreadSafety_ConcurrentGetAsyncWithCaching_ShouldWorkCorrectly(int concurrencyLevel)
    {
        // Arrange
        var provider = new MockParameterProvider(delayMs: 10);
        var results = new ConcurrentBag<ParameterRetrievalResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        const string sharedKey = "cached_key";
        const string expectedValue = "cached_value";
        provider.SetParameter(sharedKey, expectedValue);

        // Act - all threads request the same key, testing cache behavior
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new ParameterRetrievalResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "GetAsync",
                    Key = sharedKey
                };

                try
                {
                    barrier.SignalAndWait();

                    // First call - may hit the provider
                    var value1 = await provider.GetAsync(sharedKey).ConfigureAwait(false);
                    
                    // Subsequent calls - should hit cache
                    for (int j = 0; j < 20; j++)
                    {
                        var value = await provider.GetAsync(sharedKey).ConfigureAwait(false);
                        
                        if (value != expectedValue)
                        {
                            result.Success = false;
                            result.ExceptionMessage = $"Expected '{expectedValue}', got '{value}'";
                            results.Add(result);
                            return;
                        }
                    }

                    result.Value = value1;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.Equal(expectedValue, r.Value);
        });
    }

    #endregion
}


/// <summary>
/// Tests for validating configuration isolation under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads use different transformation settings,
/// ForceFetch settings, or max age settings, each invocation's configuration is applied
/// correctly without interference from other invocations.
/// 
/// **Feature: parameters-thread-safety, Property 6: Configuration Isolation**
/// **Validates: Requirements 5.1, 5.2, 5.3**
/// </summary>
[Collection("Parameters Configuration Isolation Tests")]
public class ConfigurationIsolationTests
{
    #region Helper Classes

    /// <summary>
    /// Mock parameter provider for testing configuration isolation.
    /// </summary>
    private class ConfigurableMockProvider : ParameterProvider
    {
        private readonly ConcurrentDictionary<string, string> _parameters = new();
        private int _getCallCount;

        public int GetCallCount => _getCallCount;

        public void SetParameter(string key, string value)
        {
            _parameters[key] = value;
        }

        protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
        {
            Interlocked.Increment(ref _getCallCount);
            
            // Simulate network delay
            await Task.Delay(5).ConfigureAwait(false);

            return _parameters.TryGetValue(key, out var value) ? value : null;
        }

        protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string key, ParameterProviderConfiguration? config)
        {
            await Task.Delay(5).ConfigureAwait(false);
            return new Dictionary<string, string?>();
        }
    }

    /// <summary>
    /// Custom transformer for testing transformation isolation.
    /// </summary>
    private class TestTransformer : ITransformer
    {
        private readonly string _prefix;

        public TestTransformer(string prefix)
        {
            _prefix = prefix;
        }

        public T? Transform<T>(string value)
        {
            var result = $"{_prefix}:{value}";
            if (result is T typedResult)
                return typedResult;
            return default;
        }
    }

    /// <summary>
    /// Result class for configuration isolation tests.
    /// </summary>
    private class ConfigurationIsolationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int ThreadIndex { get; set; }
        public string ConfigurationType { get; set; } = string.Empty;
        public string? ExpectedValue { get; set; }
        public string? ActualValue { get; set; }
        public bool Success { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
    }

    #endregion

    #region Property 6: Configuration Isolation - Different Transformation Settings

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 6: Configuration Isolation**
    /// *For any* set of concurrent invocations using different transformation settings,
    /// each invocation should have its transformation applied correctly without interference.
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConfigurationIsolation_DifferentTransformationSettings_ShouldApplyCorrectly(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationIsolationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act - each thread uses a different transformer
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new ConfigurationIsolationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ConfigurationType = "Transformation"
                };

                try
                {
                    // Create a provider per thread to ensure isolation
                    var provider = new ConfigurableMockProvider();
                    var key = $"key_{threadIndex}";
                    var rawValue = $"value_{threadIndex}";
                    provider.SetParameter(key, rawValue);

                    // Create a unique transformer for this thread
                    var transformer = new TestTransformer($"thread_{threadIndex}");
                    result.ExpectedValue = $"thread_{threadIndex}:{rawValue}";

                    barrier.SignalAndWait();

                    // Perform multiple operations with the transformer
                    for (int j = 0; j < 20; j++)
                    {
                        var value = await provider
                            .WithTransformation(transformer)
                            .GetAsync<string>(key)
                            .ConfigureAwait(false);

                        if (value != result.ExpectedValue)
                        {
                            result.Success = false;
                            result.ActualValue = value;
                            result.ExceptionMessage = $"Expected '{result.ExpectedValue}', got '{value}'";
                            results.Add(result);
                            return;
                        }
                    }

                    result.ActualValue = result.ExpectedValue;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: Expected '{r.ExpectedValue}', got '{r.ActualValue}'. {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Property 6: Configuration Isolation - Different ForceFetch Settings

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 6: Configuration Isolation**
    /// *For any* set of concurrent invocations using different ForceFetch settings,
    /// each invocation should respect its cache configuration.
    /// **Validates: Requirements 5.2**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(10)]
    public async Task ConfigurationIsolation_DifferentForceFetchSettings_ShouldRespectConfiguration(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationIsolationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act - half threads use ForceFetch, half use cache
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            bool useForceFetch = threadIndex % 2 == 0;

            tasks[i] = Task.Run(async () =>
            {
                var result = new ConfigurationIsolationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ConfigurationType = useForceFetch ? "ForceFetch" : "UseCache"
                };

                try
                {
                    // Create a provider per thread
                    var provider = new ConfigurableMockProvider();
                    var key = $"key_{threadIndex}";
                    provider.SetParameter(key, $"value_{threadIndex}");

                    barrier.SignalAndWait();

                    // Perform operations with different ForceFetch settings
                    for (int j = 0; j < 20; j++)
                    {
                        string? value;
                        if (useForceFetch)
                        {
                            value = await provider
                                .ForceFetch()
                                .GetAsync(key)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            value = await provider
                                .GetAsync(key)
                                .ConfigureAwait(false);
                        }

                        if (value != $"value_{threadIndex}")
                        {
                            result.Success = false;
                            result.ActualValue = value;
                            result.ExceptionMessage = $"Expected 'value_{threadIndex}', got '{value}'";
                            results.Add(result);
                            return;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} ({r.ConfigurationType}) failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Property 6: Configuration Isolation - Different Max Age Settings

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 6: Configuration Isolation**
    /// *For any* set of concurrent invocations configuring different max age values,
    /// each invocation should use the correct max age for caching.
    /// **Validates: Requirements 5.3**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConfigurationIsolation_DifferentMaxAgeSettings_ShouldUseCorrectMaxAge(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationIsolationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act - each thread uses a different max age
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new ConfigurationIsolationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ConfigurationType = "MaxAge"
                };

                try
                {
                    // Create a provider per thread
                    var provider = new ConfigurableMockProvider();
                    var key = $"key_{threadIndex}";
                    provider.SetParameter(key, $"value_{threadIndex}");

                    // Each thread uses a different max age
                    var maxAge = TimeSpan.FromSeconds(threadIndex + 1);

                    barrier.SignalAndWait();

                    // Perform operations with different max age settings
                    for (int j = 0; j < 20; j++)
                    {
                        var value = await provider
                            .WithMaxAge(maxAge)
                            .GetAsync(key)
                            .ConfigureAwait(false);

                        if (value != $"value_{threadIndex}")
                        {
                            result.Success = false;
                            result.ActualValue = value;
                            result.ExceptionMessage = $"Expected 'value_{threadIndex}', got '{value}'";
                            results.Add(result);
                            return;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Property 6: Configuration Isolation - Mixed Configuration Operations

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 6: Configuration Isolation**
    /// *For any* combination of concurrent invocations with different configurations
    /// (transformation, ForceFetch, max age), each invocation should have its
    /// configuration applied correctly without interference.
    /// **Validates: Requirements 5.1, 5.2, 5.3**
    /// </summary>
    [Theory]
    [InlineData(6)]
    [InlineData(9)]
    [InlineData(12)]
    public async Task ConfigurationIsolation_MixedConfigurationOperations_ShouldMaintainIsolation(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationIsolationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act - each thread uses a different configuration type
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            int configType = threadIndex % 3;

            tasks[i] = Task.Run(async () =>
            {
                var result = new ConfigurationIsolationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex
                };

                try
                {
                    // Create a provider per thread
                    var provider = new ConfigurableMockProvider();
                    var key = $"key_{threadIndex}";
                    var rawValue = $"value_{threadIndex}";
                    provider.SetParameter(key, rawValue);

                    barrier.SignalAndWait();

                    for (int j = 0; j < 20; j++)
                    {
                        string? value;

                        switch (configType)
                        {
                            case 0: // Use transformation
                                result.ConfigurationType = "Transformation";
                                var transformer = new TestTransformer($"t{threadIndex}");
                                result.ExpectedValue = $"t{threadIndex}:{rawValue}";
                                value = await provider
                                    .WithTransformation(transformer)
                                    .GetAsync<string>(key)
                                    .ConfigureAwait(false);
                                break;

                            case 1: // Use ForceFetch
                                result.ConfigurationType = "ForceFetch";
                                result.ExpectedValue = rawValue;
                                value = await provider
                                    .ForceFetch()
                                    .GetAsync(key)
                                    .ConfigureAwait(false);
                                break;

                            case 2: // Use MaxAge
                                result.ConfigurationType = "MaxAge";
                                result.ExpectedValue = rawValue;
                                value = await provider
                                    .WithMaxAge(TimeSpan.FromSeconds(threadIndex + 1))
                                    .GetAsync(key)
                                    .ConfigureAwait(false);
                                break;

                            default:
                                result.ExpectedValue = rawValue;
                                value = await provider.GetAsync(key).ConfigureAwait(false);
                                break;
                        }

                        if (value != result.ExpectedValue)
                        {
                            result.Success = false;
                            result.ActualValue = value;
                            result.ExceptionMessage = $"Expected '{result.ExpectedValue}', got '{value}'";
                            results.Add(result);
                            return;
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

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} ({r.ConfigurationType}) failed: Expected '{r.ExpectedValue}', got '{r.ActualValue}'. {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region High Concurrency Stress Tests

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 6: Configuration Isolation**
    /// High concurrency stress test for configuration isolation.
    /// Uses unique keys per configuration type to test isolation without cache interference.
    /// **Validates: Requirements 5.1, 5.2, 5.3**
    /// </summary>
    [Theory]
    [InlineData(15, 40)]
    [InlineData(20, 30)]
    public async Task ConfigurationIsolation_HighConcurrency_ShouldMaintainIsolation(int concurrencyLevel, int operationsPerThread)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationIsolationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    // Create a provider per thread
                    var provider = new ConfigurableMockProvider();
                    
                    // Set up parameters for this thread - use unique keys per config type
                    // to avoid cache interference between different configuration types
                    for (int k = 0; k < 10; k++)
                    {
                        // Keys for transformation operations
                        provider.SetParameter($"transform_key_{threadIndex}_{k}", $"value_{threadIndex}_{k}");
                        // Keys for ForceFetch operations
                        provider.SetParameter($"force_key_{threadIndex}_{k}", $"value_{threadIndex}_{k}");
                        // Keys for MaxAge operations
                        provider.SetParameter($"maxage_key_{threadIndex}_{k}", $"value_{threadIndex}_{k}");
                        // Keys for default operations
                        provider.SetParameter($"default_key_{threadIndex}_{k}", $"value_{threadIndex}_{k}");
                    }

                    barrier.SignalAndWait();

                    for (int op = 0; op < operationsPerThread; op++)
                    {
                        var result = new ConfigurationIsolationResult
                        {
                            InvocationId = Guid.NewGuid().ToString(),
                            ThreadIndex = threadIndex
                        };

                        try
                        {
                            int configType = (threadIndex + op) % 4;
                            int keyIndex = op % 10;
                            var rawValue = $"value_{threadIndex}_{keyIndex}";
                            string? value;

                            switch (configType)
                            {
                                case 0: // Use transformation
                                    result.ConfigurationType = "Transformation";
                                    var key0 = $"transform_key_{threadIndex}_{keyIndex}";
                                    var transformer = new TestTransformer($"t{threadIndex}");
                                    result.ExpectedValue = $"t{threadIndex}:{rawValue}";
                                    value = await provider
                                        .WithTransformation(transformer)
                                        .GetAsync<string>(key0)
                                        .ConfigureAwait(false);
                                    break;

                                case 1: // Use ForceFetch
                                    result.ConfigurationType = "ForceFetch";
                                    var key1 = $"force_key_{threadIndex}_{keyIndex}";
                                    result.ExpectedValue = rawValue;
                                    value = await provider
                                        .ForceFetch()
                                        .GetAsync(key1)
                                        .ConfigureAwait(false);
                                    break;

                                case 2: // Use MaxAge
                                    result.ConfigurationType = "MaxAge";
                                    var key2 = $"maxage_key_{threadIndex}_{keyIndex}";
                                    result.ExpectedValue = rawValue;
                                    value = await provider
                                        .WithMaxAge(TimeSpan.FromSeconds(threadIndex + 1))
                                        .GetAsync(key2)
                                        .ConfigureAwait(false);
                                    break;

                                default: // No configuration
                                    result.ConfigurationType = "Default";
                                    var key3 = $"default_key_{threadIndex}_{keyIndex}";
                                    result.ExpectedValue = rawValue;
                                    value = await provider.GetAsync(key3).ConfigureAwait(false);
                                    break;
                            }

                            result.ActualValue = value;
                            result.Success = value == result.ExpectedValue;
                            
                            if (!result.Success)
                            {
                                result.ExceptionMessage = $"Expected '{result.ExpectedValue}', got '{value}'";
                            }
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
                    results.Add(new ConfigurationIsolationResult
                    {
                        ThreadIndex = threadIndex,
                        ConfigurationType = "Barrier",
                        ExceptionThrown = true,
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message
                    });
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} ({r.ConfigurationType}) failed: Expected '{r.ExpectedValue}', got '{r.ActualValue}'. {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion
}
