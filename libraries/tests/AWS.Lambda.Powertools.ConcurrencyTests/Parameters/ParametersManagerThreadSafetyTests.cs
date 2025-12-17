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
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.AppConfig;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.DynamoDB;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Internal.Transform;
using AWS.Lambda.Powertools.Parameters.SecretsManager;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Parameters;

/// <summary>
/// Tests for validating ParametersManager thread safety under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads access provider singletons and configuration
/// methods simultaneously, all operations complete without exceptions and return consistent results.
/// 
/// **Feature: parameters-thread-safety, Property 1: Provider Singleton Thread Safety**
/// **Feature: parameters-thread-safety, Property 2: Configuration Thread Safety**
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5**
/// </summary>
[Collection("Parameters Manager Tests")]
public class ParametersManagerThreadSafetyTests
{
    #region Property 1: Provider Singleton Thread Safety - SsmProvider

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 1: Provider Singleton Thread Safety**
    /// *For any* number of concurrent threads accessing ParametersManager.SsmProvider simultaneously,
    /// all threads should receive the same provider instance without throwing exceptions.
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ProviderSingletonThreadSafety_ConcurrentSsmProviderAccess_ShouldReturnSameInstance(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ProviderAccessResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var retrievedProviders = new ConcurrentBag<ISsmProvider>();

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ProviderAccessResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ProviderType = "SsmProvider"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple accesses
                    for (int j = 0; j < 100; j++)
                    {
                        var provider = ParametersManager.SsmProvider;
                        if (provider == null)
                        {
                            result.ExceptionThrown = true;
                            result.ExceptionMessage = "Provider was null";
                            results.Add(result);
                            return;
                        }
                        retrievedProviders.Add(provider);
                        result.ProviderHashCode = provider.GetHashCode();
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

        Task.WaitAll(tasks);

        // Assert - no exceptions
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });

        // Assert - all retrieved providers are the same instance
        var distinctProviders = retrievedProviders.Distinct().ToList();
        Assert.Single(distinctProviders);
    }

    #endregion

    #region Property 1: Provider Singleton Thread Safety - SecretsProvider

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 1: Provider Singleton Thread Safety**
    /// *For any* number of concurrent threads accessing ParametersManager.SecretsProvider simultaneously,
    /// all threads should receive the same provider instance without throwing exceptions.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ProviderSingletonThreadSafety_ConcurrentSecretsProviderAccess_ShouldReturnSameInstance(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ProviderAccessResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var retrievedProviders = new ConcurrentBag<ISecretsProvider>();

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ProviderAccessResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ProviderType = "SecretsProvider"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple accesses
                    for (int j = 0; j < 100; j++)
                    {
                        var provider = ParametersManager.SecretsProvider;
                        if (provider == null)
                        {
                            result.ExceptionThrown = true;
                            result.ExceptionMessage = "Provider was null";
                            results.Add(result);
                            return;
                        }
                        retrievedProviders.Add(provider);
                        result.ProviderHashCode = provider.GetHashCode();
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

        Task.WaitAll(tasks);

        // Assert - no exceptions
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });

        // Assert - all retrieved providers are the same instance
        var distinctProviders = retrievedProviders.Distinct().ToList();
        Assert.Single(distinctProviders);
    }

    #endregion

    #region Property 1: Provider Singleton Thread Safety - DynamoDBProvider

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 1: Provider Singleton Thread Safety**
    /// *For any* number of concurrent threads accessing ParametersManager.DynamoDBProvider simultaneously,
    /// all threads should receive the same provider instance without throwing exceptions.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ProviderSingletonThreadSafety_ConcurrentDynamoDBProviderAccess_ShouldReturnSameInstance(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ProviderAccessResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var retrievedProviders = new ConcurrentBag<IDynamoDBProvider>();

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ProviderAccessResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ProviderType = "DynamoDBProvider"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple accesses
                    for (int j = 0; j < 100; j++)
                    {
                        var provider = ParametersManager.DynamoDBProvider;
                        if (provider == null)
                        {
                            result.ExceptionThrown = true;
                            result.ExceptionMessage = "Provider was null";
                            results.Add(result);
                            return;
                        }
                        retrievedProviders.Add(provider);
                        result.ProviderHashCode = provider.GetHashCode();
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

        Task.WaitAll(tasks);

        // Assert - no exceptions
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });

        // Assert - all retrieved providers are the same instance
        var distinctProviders = retrievedProviders.Distinct().ToList();
        Assert.Single(distinctProviders);
    }

    #endregion

    #region Property 1: Provider Singleton Thread Safety - AppConfigProvider

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 1: Provider Singleton Thread Safety**
    /// *For any* number of concurrent threads accessing ParametersManager.AppConfigProvider simultaneously,
    /// all threads should receive the same provider instance without throwing exceptions.
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ProviderSingletonThreadSafety_ConcurrentAppConfigProviderAccess_ShouldReturnSameInstance(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ProviderAccessResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var retrievedProviders = new ConcurrentBag<IAppConfigProvider>();

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ProviderAccessResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ProviderType = "AppConfigProvider"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Perform multiple accesses
                    for (int j = 0; j < 100; j++)
                    {
                        var provider = ParametersManager.AppConfigProvider;
                        if (provider == null)
                        {
                            result.ExceptionThrown = true;
                            result.ExceptionMessage = "Provider was null";
                            results.Add(result);
                            return;
                        }
                        retrievedProviders.Add(provider);
                        result.ProviderHashCode = provider.GetHashCode();
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

        Task.WaitAll(tasks);

        // Assert - no exceptions
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });

        // Assert - all retrieved providers are the same instance
        var distinctProviders = retrievedProviders.Distinct().ToList();
        Assert.Single(distinctProviders);
    }

    #endregion

    #region Property 1: Provider Singleton Thread Safety - All Providers Mixed Access

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 1: Provider Singleton Thread Safety**
    /// *For any* number of concurrent threads accessing all provider types simultaneously,
    /// all threads should receive the correct provider instances without throwing exceptions.
    /// **Validates: Requirements 1.1, 1.2, 1.3, 1.4**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    public void ProviderSingletonThreadSafety_ConcurrentMixedProviderAccess_ShouldReturnCorrectInstances(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ProviderAccessResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var ssmProviders = new ConcurrentBag<ISsmProvider>();
        var secretsProviders = new ConcurrentBag<ISecretsProvider>();
        var dynamoDBProviders = new ConcurrentBag<IDynamoDBProvider>();
        var appConfigProviders = new ConcurrentBag<IAppConfigProvider>();

        // Act - each thread accesses a different provider type based on index
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ProviderAccessResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex
                };

                try
                {
                    barrier.SignalAndWait();

                    // Each thread accesses all provider types
                    for (int j = 0; j < 50; j++)
                    {
                        int providerType = (threadIndex + j) % 4;
                        
                        switch (providerType)
                        {
                            case 0:
                                result.ProviderType = "SsmProvider";
                                var ssm = ParametersManager.SsmProvider;
                                if (ssm == null) throw new InvalidOperationException("SsmProvider was null");
                                ssmProviders.Add(ssm);
                                break;
                            case 1:
                                result.ProviderType = "SecretsProvider";
                                var secrets = ParametersManager.SecretsProvider;
                                if (secrets == null) throw new InvalidOperationException("SecretsProvider was null");
                                secretsProviders.Add(secrets);
                                break;
                            case 2:
                                result.ProviderType = "DynamoDBProvider";
                                var dynamo = ParametersManager.DynamoDBProvider;
                                if (dynamo == null) throw new InvalidOperationException("DynamoDBProvider was null");
                                dynamoDBProviders.Add(dynamo);
                                break;
                            case 3:
                                result.ProviderType = "AppConfigProvider";
                                var appConfig = ParametersManager.AppConfigProvider;
                                if (appConfig == null) throw new InvalidOperationException("AppConfigProvider was null");
                                appConfigProviders.Add(appConfig);
                                break;
                        }
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

        Task.WaitAll(tasks);

        // Assert - no exceptions
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown,
                $"Thread {r.ThreadIndex} accessing {r.ProviderType} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });

        // Assert - each provider type returns the same instance
        if (ssmProviders.Any())
            Assert.Single(ssmProviders.Distinct());
        if (secretsProviders.Any())
            Assert.Single(secretsProviders.Distinct());
        if (dynamoDBProviders.Any())
            Assert.Single(dynamoDBProviders.Distinct());
        if (appConfigProviders.Any())
            Assert.Single(appConfigProviders.Distinct());
    }

    #endregion


    #region Property 2: Configuration Thread Safety - DefaultMaxAge

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 2: Configuration Thread Safety**
    /// *For any* number of concurrent threads calling ParametersManager.DefaultMaxAge() simultaneously,
    /// the configuration should complete without exceptions and the resulting configuration should be consistent.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ConfigurationThreadSafety_ConcurrentDefaultMaxAgeCalls_ShouldCompleteWithoutExceptions(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ConfigurationType = "DefaultMaxAge"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Each thread sets a different max age
                    for (int j = 0; j < 50; j++)
                    {
                        var maxAge = TimeSpan.FromSeconds(threadIndex * 10 + j + 1);
                        result.MaxAgeSet = maxAge;
                        ParametersManager.DefaultMaxAge(maxAge);
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

        // Assert - no exceptions during concurrent configuration
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 2: Configuration Thread Safety**
    /// Tests that DefaultMaxAge with invalid values throws ArgumentOutOfRangeException consistently.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ConfigurationThreadSafety_ConcurrentInvalidDefaultMaxAge_ShouldThrowConsistently(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ConfigurationType = "DefaultMaxAge_Invalid"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Try to set invalid max age (zero or negative)
                    var invalidMaxAge = TimeSpan.Zero;
                    result.MaxAgeSet = invalidMaxAge;
                    ParametersManager.DefaultMaxAge(invalidMaxAge);

                    // If we get here, the validation didn't work
                    result.Success = false;
                    result.ExceptionMessage = "Expected ArgumentOutOfRangeException was not thrown";
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Expected exception
                    result.Success = true;
                    result.ExceptionThrown = true;
                    result.ExceptionType = nameof(ArgumentOutOfRangeException);
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

        // Assert - all threads should have received ArgumentOutOfRangeException
        Assert.All(results, r =>
        {
            Assert.True(r.Success,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.Equal(nameof(ArgumentOutOfRangeException), r.ExceptionType);
        });
    }

    #endregion

    #region Property 2: Configuration Thread Safety - UseCacheManager

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 2: Configuration Thread Safety**
    /// *For any* number of concurrent threads calling ParametersManager.UseCacheManager() simultaneously,
    /// the configuration should complete without exceptions.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ConfigurationThreadSafety_ConcurrentUseCacheManagerCalls_ShouldCompleteWithoutExceptions(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ConfigurationType = "UseCacheManager"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Each thread sets a different cache manager
                    for (int j = 0; j < 20; j++)
                    {
                        var cacheManager = new CacheManager(DateTimeWrapper.Instance);
                        ParametersManager.UseCacheManager(cacheManager);
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

        // Assert - no exceptions during concurrent configuration
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Property 2: Configuration Thread Safety - UseTransformerManager

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 2: Configuration Thread Safety**
    /// *For any* number of concurrent threads calling ParametersManager.UseTransformerManager() simultaneously,
    /// the configuration should complete without exceptions.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ConfigurationThreadSafety_ConcurrentUseTransformerManagerCalls_ShouldCompleteWithoutExceptions(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    ConfigurationType = "UseTransformerManager"
                };

                try
                {
                    barrier.SignalAndWait();

                    // Each thread sets a different transformer manager
                    for (int j = 0; j < 20; j++)
                    {
                        var transformerManager = new TransformerManager();
                        ParametersManager.UseTransformerManager(transformerManager);
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

        // Assert - no exceptions during concurrent configuration
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region Property 2: Configuration Thread Safety - Mixed Configuration Operations

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 2: Configuration Thread Safety**
    /// *For any* combination of concurrent configuration operations (DefaultMaxAge, UseCacheManager, UseTransformerManager),
    /// all operations should complete without exceptions.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Theory]
    [InlineData(6)]
    [InlineData(9)]
    [InlineData(12)]
    public void ConfigurationThreadSafety_MixedConfigurationOperations_ShouldCompleteWithoutExceptions(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentBag<ConfigurationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act - each thread performs a different configuration operation based on index
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ConfigurationResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 30; j++)
                    {
                        int opType = (threadIndex + j) % 3;

                        switch (opType)
                        {
                            case 0:
                                result.ConfigurationType = "DefaultMaxAge";
                                var maxAge = TimeSpan.FromSeconds(threadIndex * 10 + j + 1);
                                result.MaxAgeSet = maxAge;
                                ParametersManager.DefaultMaxAge(maxAge);
                                break;
                            case 1:
                                result.ConfigurationType = "UseCacheManager";
                                var cacheManager = new CacheManager(DateTimeWrapper.Instance);
                                ParametersManager.UseCacheManager(cacheManager);
                                break;
                            case 2:
                                result.ConfigurationType = "UseTransformerManager";
                                var transformerManager = new TransformerManager();
                                ParametersManager.UseTransformerManager(transformerManager);
                                break;
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

        // Assert - no exceptions during concurrent configuration
        Assert.All(results, r =>
        {
            Assert.True(r.Success && !r.ExceptionThrown,
                $"Thread {r.ThreadIndex} {r.ConfigurationType} failed: {r.ExceptionType}: {r.ExceptionMessage}");
        });
    }

    #endregion

    #region High Concurrency Stress Tests

    /// <summary>
    /// **Feature: parameters-thread-safety, Property 1: Provider Singleton Thread Safety**
    /// **Feature: parameters-thread-safety, Property 2: Configuration Thread Safety**
    /// High concurrency stress test combining provider access and configuration operations.
    /// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5**
    /// </summary>
    [Theory]
    [InlineData(20, 100)]
    [InlineData(30, 50)]
    public void ParametersManagerThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        // Arrange
        var results = new ConcurrentBag<ThreadSafetyResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Act
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var result = new ThreadSafetyResult
                {
                    InvocationId = Guid.NewGuid().ToString(),
                    ThreadIndex = threadIndex,
                    OperationsAttempted = operationsPerThread
                };

                try
                {
                    barrier.SignalAndWait();

                    for (int op = 0; op < operationsPerThread; op++)
                    {
                        int opType = (threadIndex + op) % 7;

                        switch (opType)
                        {
                            case 0: // Access SsmProvider
                                var ssm = ParametersManager.SsmProvider;
                                if (ssm == null) throw new InvalidOperationException("SsmProvider was null");
                                break;
                            case 1: // Access SecretsProvider
                                var secrets = ParametersManager.SecretsProvider;
                                if (secrets == null) throw new InvalidOperationException("SecretsProvider was null");
                                break;
                            case 2: // Access DynamoDBProvider
                                var dynamo = ParametersManager.DynamoDBProvider;
                                if (dynamo == null) throw new InvalidOperationException("DynamoDBProvider was null");
                                break;
                            case 3: // Access AppConfigProvider
                                var appConfig = ParametersManager.AppConfigProvider;
                                if (appConfig == null) throw new InvalidOperationException("AppConfigProvider was null");
                                break;
                            case 4: // Set DefaultMaxAge
                                var maxAge = TimeSpan.FromSeconds(threadIndex * 10 + op + 1);
                                ParametersManager.DefaultMaxAge(maxAge);
                                break;
                            case 5: // Set CacheManager
                                var cacheManager = new CacheManager(DateTimeWrapper.Instance);
                                ParametersManager.UseCacheManager(cacheManager);
                                break;
                            case 6: // Set TransformerManager
                                var transformerManager = new TransformerManager();
                                ParametersManager.UseTransformerManager(transformerManager);
                                break;
                        }

                        result.OperationsCompleted++;
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

        Task.WaitAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.False(r.ExceptionThrown,
                $"Thread {r.ThreadIndex} failed after {r.OperationsCompleted}/{r.OperationsAttempted} ops: {r.ExceptionType}: {r.ExceptionMessage}");
            Assert.Equal(r.OperationsAttempted, r.OperationsCompleted);
        });
    }

    #endregion
}
