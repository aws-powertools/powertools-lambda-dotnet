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
using System.Text.Json;
using System.Text.Json.Nodes;
using AWS.Lambda.Powertools.Idempotency;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Idempotency;

/// <summary>
/// Tests for validating hash generation thread safety under concurrent execution scenarios.
/// **Feature: idempotency-thread-safety, Property 6: Hash Generation Thread Safety**
/// **Validates: Requirements 1.4, 5.1, 5.2, 5.3**
/// </summary>
[Collection("Idempotency Hash Generation Tests")]
public class IdempotencyHashGenerationTests
{
    private class HashResult
    {
        public int ThreadIndex { get; set; }
        public string Payload { get; set; } = string.Empty;
        public string ExpectedHash { get; set; } = string.Empty;
        public string ActualHash { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 6: Hash Generation Thread Safety**
    /// **Validates: Requirements 1.4, 5.1, 5.2, 5.3**
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(5, 25)]
    [InlineData(10, 50)]
    public void HashGenerationThreadSafety_ConcurrentHashGeneration_ShouldProduceCorrectHashes(int concurrencyLevel, int operationsPerThread)
    {
        var persistenceStore = new ThreadSafeInMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        var results = new ConcurrentBag<HashResult>();
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
                        var result = new HashResult { ThreadIndex = threadIndex };

                        try
                        {
                            string payload = $"thread_{threadIndex}_operation_{op}";
                            result.Payload = payload;

                            var jsonValue = JsonValue.Create(payload);
                            var jsonDoc = JsonDocument.Parse(jsonValue!.ToJsonString());
                            var hash = persistenceStore.GenerateHash(jsonDoc.RootElement);

                            var expectedHash = ComputeExpectedMd5Hash(payload);
                            result.ExpectedHash = expectedHash;
                            result.ActualHash = hash;
                            result.Success = hash == expectedHash;
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
                    results.Add(new HashResult
                    {
                        ThreadIndex = threadIndex,
                        ExceptionThrown = true,
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message
                    });
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} failed: expected '{r.ExpectedHash}', got '{r.ActualHash}'. {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void HashGenerationThreadSafety_SamePayload_ShouldProduceSameHash(int concurrencyLevel)
    {
        var persistenceStore = new ThreadSafeInMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        const string sharedPayload = "shared_test_payload";
        var jsonValue = JsonValue.Create(sharedPayload);
        var jsonString = jsonValue!.ToJsonString();

        var hashes = new ConcurrentBag<string>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int j = 0; j < 10; j++)
                    {
                        var jsonDoc = JsonDocument.Parse(jsonString);
                        var hash = persistenceStore.GenerateHash(jsonDoc.RootElement);
                        hashes.Add(hash);
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
        var distinctHashes = hashes.Distinct().ToList();
        Assert.Single(distinctHashes);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void HashGenerationThreadSafety_DifferentPayloads_ShouldProduceDifferentHashes(int concurrencyLevel)
    {
        var persistenceStore = new ThreadSafeInMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        var hashToPayload = new ConcurrentDictionary<string, string>();
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

                    for (int j = 0; j < 10; j++)
                    {
                        string payload = $"unique_payload_thread_{threadIndex}_op_{j}";
                        var jsonValue = JsonValue.Create(payload);
                        var jsonDoc = JsonDocument.Parse(jsonValue!.ToJsonString());
                        var hash = persistenceStore.GenerateHash(jsonDoc.RootElement);

                        hashToPayload.TryAdd(hash, payload);
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
        int expectedUniquePayloads = concurrencyLevel * 10;
        Assert.Equal(expectedUniquePayloads, hashToPayload.Count);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void HashGenerationThreadSafety_ComplexObjects_ShouldProduceCorrectHashes(int concurrencyLevel)
    {
        var persistenceStore = new ThreadSafeInMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        var results = new ConcurrentBag<HashResult>();
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

                    for (int op = 0; op < 10; op++)
                    {
                        var result = new HashResult { ThreadIndex = threadIndex };

                        try
                        {
                            var complexObject = new
                            {
                                Id = threadIndex * 100 + op,
                                Name = $"Item_{threadIndex}_{op}",
                                Price = (threadIndex + 1) * 10.5 + op,
                                Tags = new[] { $"tag_{threadIndex}", $"tag_{op}" },
                                Metadata = new { ThreadId = threadIndex, OperationId = op }
                            };

                            var jsonString = JsonSerializer.Serialize(complexObject);
                            result.Payload = jsonString;

                            var jsonDoc = JsonDocument.Parse(jsonString);
                            var hash = persistenceStore.GenerateHash(jsonDoc.RootElement);
                            result.ActualHash = hash;

                            result.Success = !string.IsNullOrEmpty(hash) && 
                                           hash.Length == 32 && 
                                           hash.All(c => char.IsLetterOrDigit(c));
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
                    results.Add(new HashResult
                    {
                        ThreadIndex = threadIndex,
                        ExceptionThrown = true,
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message
                    });
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    private static string ComputeExpectedMd5Hash(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    [Theory]
    [InlineData(20, 100)]
    [InlineData(30, 50)]
    public void HashGenerationThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        var persistenceStore = new ThreadSafeInMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

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
                        string payload = $"stress_test_thread_{threadIndex}_op_{op}";
                        var jsonValue = JsonValue.Create(payload);
                        var jsonDoc = JsonDocument.Parse(jsonValue!.ToJsonString());
                        persistenceStore.GenerateHash(jsonDoc.RootElement);
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

    [Fact]
    public void HashGenerationThreadSafety_MultipleStoreInstances_ShouldProduceSameHashes()
    {
        const string testPayload = "test_payload_for_consistency";
        var jsonValue = JsonValue.Create(testPayload);
        var jsonString = jsonValue!.ToJsonString();

        var hashes = new ConcurrentBag<string>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(10);
        var tasks = new Task[10];

        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    var store = new ThreadSafeInMemoryPersistenceStore();
                    store.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

                    barrier.SignalAndWait();

                    var jsonDoc = JsonDocument.Parse(jsonString);
                    var hash = store.GenerateHash(jsonDoc.RootElement);
                    hashes.Add(hash);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.Empty(exceptions);
        var distinctHashes = hashes.Distinct().ToList();
        Assert.Single(distinctHashes);
    }

    [Fact]
    public void HashGenerationThreadSafety_VariousJsonTypes_ShouldProduceValidHashes()
    {
        var persistenceStore = new ThreadSafeInMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        var testCases = new[]
        {
            ("string", "\"test string\""),
            ("number", "42"),
            ("decimal", "3.14159"),
            ("boolean_true", "true"),
            ("boolean_false", "false"),
            ("null", "null"),
            ("array", "[1, 2, 3]"),
            ("object", "{\"key\": \"value\"}")
        };

        var results = new ConcurrentBag<(string type, string hash, bool valid)>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(testCases.Length);
        var tasks = new Task[testCases.Length];

        for (int i = 0; i < testCases.Length; i++)
        {
            var (typeName, jsonValue) = testCases[i];
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();

                    var jsonDoc = JsonDocument.Parse(jsonValue);
                    var hash = persistenceStore.GenerateHash(jsonDoc.RootElement);
                    
                    bool isValid = !string.IsNullOrEmpty(hash) && 
                                  hash.Length == 32 && 
                                  hash.All(c => char.IsLetterOrDigit(c));
                    
                    results.Add((typeName, hash, isValid));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.Empty(exceptions);
        Assert.All(results, r => Assert.True(r.valid, $"Hash for type '{r.type}' was invalid: {r.hash}"));
    }
}
