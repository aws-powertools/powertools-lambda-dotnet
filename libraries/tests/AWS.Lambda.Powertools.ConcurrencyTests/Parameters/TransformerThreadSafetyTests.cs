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
using AWS.Lambda.Powertools.Parameters.Internal.Transform;
using AWS.Lambda.Powertools.Parameters.Transform;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Parameters;

/// <summary>
/// Tests for validating TransformerManager thread safety under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads perform transformer retrieval and registration
/// operations simultaneously, all operations complete without exceptions and without data corruption.
/// 
/// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
/// **Validates: Requirements 3.1, 3.2, 3.3**
/// </summary>
[Collection("Parameters Transformer Tests")]
public class TransformerThreadSafetyTests
{
    #region Helper Classes

    /// <summary>
    /// Simple test transformer implementation for testing custom transformer registration.
    /// </summary>
    private class TestTransformer : ITransformer
    {
        public string Name { get; }
        
        public TestTransformer(string name)
        {
            Name = name;
        }

        public T? Transform<T>(string value)
        {
            // Simple implementation that returns the value as-is for string type
            if (typeof(T) == typeof(string))
            {
                return (T)(object)$"{Name}:{value}";
            }
            return default;
        }
    }

    #endregion


    #region Property 4: Transformer Thread Safety - Concurrent Retrieval (Same Type)

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// *For any* set of concurrent transformer retrieval operations for the same transformation type,
    /// all retrievals should return the correct transformer without throwing exceptions.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void TransformerThreadSafety_ConcurrentRetrievalSameType_ShouldReturnCorrectTransformer(int concurrencyLevel)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var results = new ConcurrentBag<TransformerOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var retrievedTransformers = new ConcurrentBag<ITransformer>();

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new TransformerOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "GetTransformer",
                    TransformerName = "Json"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple retrievals
                    for (int j = 0; j < 100; j++)
                    {
                        var transformer = transformerManager.GetTransformer(Transformation.Json);
                        if (transformer == null)
                        {
                            result.Success = false;
                            result.ExceptionMessage = "Transformer was null";
                            results.Add(result);
                            return;
                        }
                        retrievedTransformers.Add(transformer);
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

        // Assert - no exceptions
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });

        // Assert - all retrieved transformers are the same instance (singleton behavior)
        var distinctTransformers = retrievedTransformers.Distinct().ToList();
        Assert.Single(distinctTransformers);
        Assert.IsType<JsonTransformer>(distinctTransformers[0]);
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// *For any* set of concurrent transformer retrieval operations for Base64 transformation type,
    /// all retrievals should return the correct transformer without throwing exceptions.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void TransformerThreadSafety_ConcurrentRetrievalBase64_ShouldReturnCorrectTransformer(int concurrencyLevel)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var results = new ConcurrentBag<TransformerOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var retrievedTransformers = new ConcurrentBag<ITransformer>();

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new TransformerOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "GetTransformer",
                    TransformerName = "Base64"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple retrievals
                    for (int j = 0; j < 100; j++)
                    {
                        var transformer = transformerManager.GetTransformer(Transformation.Base64);
                        if (transformer == null)
                        {
                            result.Success = false;
                            result.ExceptionMessage = "Transformer was null";
                            results.Add(result);
                            return;
                        }
                        retrievedTransformers.Add(transformer);
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

        // Assert - no exceptions
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });

        // Assert - all retrieved transformers are the same instance
        var distinctTransformers = retrievedTransformers.Distinct().ToList();
        Assert.Single(distinctTransformers);
        Assert.IsType<Base64Transformer>(distinctTransformers[0]);
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// *For any* set of concurrent TryGetTransformer operations with Auto transformation,
    /// all retrievals should return the correct transformer based on key suffix.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void TransformerThreadSafety_ConcurrentTryGetTransformerAuto_ShouldReturnCorrectTransformer(int concurrencyLevel)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var results = new ConcurrentBag<TransformerOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new TransformerOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationType = "TryGetTransformer"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple retrievals with different key suffixes
                    for (int j = 0; j < 100; j++)
                    {
                        // Alternate between json and base64 keys
                        string key = j % 2 == 0 ? "param.json" : "param.base64";
                        result.TransformerName = key;
                        
                        var transformer = transformerManager.TryGetTransformer(Transformation.Auto, key);
                        if (transformer == null)
                        {
                            result.Success = false;
                            result.ExceptionMessage = $"Transformer was null for key '{key}'";
                            results.Add(result);
                            return;
                        }

                        // Verify correct transformer type based on key
                        bool isCorrectType = key.EndsWith(".json") 
                            ? transformer is JsonTransformer 
                            : transformer is Base64Transformer;
                        
                        if (!isCorrectType)
                        {
                            result.Success = false;
                            result.ExceptionMessage = $"Wrong transformer type for key '{key}': {transformer.GetType().Name}";
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

        Task.WaitAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion


    #region Property 4: Transformer Thread Safety - Concurrent Custom Transformer Registration

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// *For any* set of concurrent custom transformer registration operations with different names,
    /// all registrations should complete without data corruption.
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void TransformerThreadSafety_ConcurrentCustomRegistrationDifferentNames_ShouldCompleteWithoutCorruption(int concurrencyLevel)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var registeredTransformers = new ConcurrentDictionary<string, ITransformer>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        int transformersPerThread = 20;

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < transformersPerThread; j++)
                    {
                        string name = $"custom_transformer_{threadIndex}_{j}";
                        var transformer = new TestTransformer(name);
                        transformerManager.AddTransformer(name, transformer);
                        registeredTransformers[name] = transformer;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - no exceptions during registration
        Assert.Empty(exceptions);

        // Assert - all registered transformers can be retrieved correctly
        foreach (var kvp in registeredTransformers)
        {
            var retrievedTransformer = transformerManager.TryGetTransformer(kvp.Key);
            Assert.NotNull(retrievedTransformer);
            Assert.IsType<TestTransformer>(retrievedTransformer);
            Assert.Equal(kvp.Key, ((TestTransformer)retrievedTransformer).Name);
        }
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// *For any* set of concurrent custom transformer registration operations with the same name,
    /// all registrations should complete without exceptions and the final transformer should be one of the registered ones.
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void TransformerThreadSafety_ConcurrentCustomRegistrationSameName_ShouldCompleteWithoutExceptions(int concurrencyLevel)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var registeredTransformers = new ConcurrentBag<TestTransformer>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        const string sharedName = "shared_custom_transformer";

        // Act
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
                        var transformer = new TestTransformer($"{sharedName}_{threadIndex}_{j}");
                        transformerManager.AddTransformer(sharedName, transformer);
                        registeredTransformers.Add(transformer);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - no exceptions during concurrent registration
        Assert.Empty(exceptions);

        // Assert - the final transformer should be one of the registered ones
        var finalTransformer = transformerManager.TryGetTransformer(sharedName);
        Assert.NotNull(finalTransformer);
        Assert.IsType<TestTransformer>(finalTransformer);
        
        // The final transformer's name should match one of the registered transformers
        var finalName = ((TestTransformer)finalTransformer).Name;
        Assert.Contains(registeredTransformers, t => t.Name == finalName);
    }

    #endregion


    #region Property 4: Transformer Thread Safety - Mixed Retrieval/Registration Operations

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// *For any* combination of concurrent transformer retrieval and registration operations,
    /// all operations should complete without throwing exceptions.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(10)]
    public void TransformerThreadSafety_MixedRetrievalAndRegistration_ShouldBeSafe(int concurrencyLevel)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var results = new ConcurrentBag<TransformerOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        int halfConcurrency = concurrencyLevel / 2;

        // Act - half threads register, half threads retrieve
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            bool isRegistrar = threadIndex < halfConcurrency;

            tasks[i] = Task.Run(() =>
            {
                var result = new TransformerOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 100; j++)
                    {
                        if (isRegistrar)
                        {
                            result.OperationType = "AddTransformer";
                            string name = $"mixed_transformer_{threadIndex}_{j}";
                            result.TransformerName = name;
                            var transformer = new TestTransformer(name);
                            transformerManager.AddTransformer(name, transformer);
                        }
                        else
                        {
                            result.OperationType = "GetTransformer";
                            // Retrieve built-in transformers
                            var transformation = j % 2 == 0 ? Transformation.Json : Transformation.Base64;
                            result.TransformerName = transformation.ToString();
                            var transformer = transformerManager.GetTransformer(transformation);
                            
                            if (transformer == null)
                            {
                                throw new InvalidOperationException($"Transformer was null for {transformation}");
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
            $"Thread {r.ThreadIndex} {r.OperationType} on '{r.TransformerName}' failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// *For any* combination of concurrent retrieval and registration on the same transformer name,
    /// all operations should complete without throwing exceptions.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(10)]
    public void TransformerThreadSafety_MixedRetrievalAndRegistrationSameName_ShouldBeSafe(int concurrencyLevel)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var results = new ConcurrentBag<TransformerOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        int halfConcurrency = concurrencyLevel / 2;
        const string sharedName = "shared_transformer";

        // Pre-register a transformer so retrievals have something to find
        transformerManager.AddTransformer(sharedName, new TestTransformer(sharedName));

        // Act - half threads register, half threads retrieve
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            bool isRegistrar = threadIndex < halfConcurrency;

            tasks[i] = Task.Run(() =>
            {
                var result = new TransformerOperationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    TransformerName = sharedName
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 100; j++)
                    {
                        if (isRegistrar)
                        {
                            result.OperationType = "AddTransformer";
                            var transformer = new TestTransformer($"{sharedName}_{threadIndex}_{j}");
                            transformerManager.AddTransformer(sharedName, transformer);
                        }
                        else
                        {
                            result.OperationType = "TryGetTransformer";
                            var transformer = transformerManager.TryGetTransformer(sharedName);
                            
                            // Transformer should always be found since we pre-registered
                            if (transformer == null)
                            {
                                throw new InvalidOperationException($"Transformer '{sharedName}' was null");
                            }
                            
                            // Verify it's a TestTransformer
                            if (!(transformer is TestTransformer))
                            {
                                throw new InvalidOperationException($"Unexpected transformer type: {transformer.GetType().Name}");
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


    #region High Concurrency Stress Tests

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// High concurrency stress test for transformer operations.
    /// **Validates: Requirements 3.1, 3.2, 3.3**
    /// </summary>
    [Theory]
    [InlineData(20, 200)]
    [InlineData(30, 100)]
    public void TransformerThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var results = new ConcurrentBag<TransformerOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

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
                        var result = new TransformerOperationResult
                        {
                            InvocationId = Guid.NewGuid().ToString(),
                            ThreadIndex = threadIndex
                        };

                        try
                        {
                            // Mix of operations
                            int opType = (threadIndex + op) % 4;

                            switch (opType)
                            {
                                case 0: // Get Json transformer
                                    result.OperationType = "GetTransformer";
                                    result.TransformerName = "Json";
                                    var jsonTransformer = transformerManager.GetTransformer(Transformation.Json);
                                    if (jsonTransformer == null)
                                        throw new InvalidOperationException("Json transformer was null");
                                    break;
                                    
                                case 1: // Get Base64 transformer
                                    result.OperationType = "GetTransformer";
                                    result.TransformerName = "Base64";
                                    var base64Transformer = transformerManager.GetTransformer(Transformation.Base64);
                                    if (base64Transformer == null)
                                        throw new InvalidOperationException("Base64 transformer was null");
                                    break;
                                    
                                case 2: // Register unique custom transformer
                                    result.OperationType = "AddTransformer";
                                    string uniqueName = $"stress_transformer_{threadIndex}_{op}";
                                    result.TransformerName = uniqueName;
                                    transformerManager.AddTransformer(uniqueName, new TestTransformer(uniqueName));
                                    break;
                                    
                                case 3: // Register shared custom transformer
                                    result.OperationType = "AddTransformer";
                                    string sharedName = $"shared_stress_{op % 10}";
                                    result.TransformerName = sharedName;
                                    transformerManager.AddTransformer(sharedName, new TestTransformer($"{sharedName}_{threadIndex}"));
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
                    results.Add(new TransformerOperationResult
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
            $"Thread {r.ThreadIndex} {r.OperationType} on '{r.TransformerName}' failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 4: Transformer Thread Safety**
    /// Tests that TryGetTransformer with Auto transformation handles concurrent access correctly.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Theory]
    [InlineData(10, 100)]
    [InlineData(20, 50)]
    public void TransformerThreadSafety_ConcurrentAutoTransformation_ShouldReturnCorrectTransformers(int concurrencyLevel, int operationsPerThread)
    {
        // Arrange
        var transformerManager = new TransformerManager();
        var results = new ConcurrentBag<TransformerOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        
        string[] keys = { "config.json", "secret.base64", "data.binary", "file.json", "encoded.base64" };

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
                        var result = new TransformerOperationResult
                        {
                            InvocationId = Guid.NewGuid().ToString(),
                            ThreadIndex = threadIndex,
                            OperationType = "TryGetTransformer"
                        };

                        try
                        {
                            string key = keys[(threadIndex + op) % keys.Length];
                            result.TransformerName = key;
                            
                            var transformer = transformerManager.TryGetTransformer(Transformation.Auto, key);
                            
                            if (transformer == null)
                            {
                                throw new InvalidOperationException($"Transformer was null for key '{key}'");
                            }

                            // Verify correct transformer type
                            bool isCorrectType = key.EndsWith(".json")
                                ? transformer is JsonTransformer
                                : transformer is Base64Transformer;

                            if (!isCorrectType)
                            {
                                throw new InvalidOperationException($"Wrong transformer type for key '{key}': {transformer.GetType().Name}");
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
                    results.Add(new TransformerOperationResult
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
            $"Thread {r.ThreadIndex} {r.OperationType} on '{r.TransformerName}' failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    #endregion
}
