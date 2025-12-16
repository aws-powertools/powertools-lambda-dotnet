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
using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Idempotency;

/// <summary>
/// Tests for validating persistence store thread safety under concurrent execution scenarios.
/// 
/// These tests verify that when multiple threads perform SaveInProgress, SaveSuccess,
/// GetRecord, and DeleteRecord operations simultaneously, all operations complete without
/// exceptions and each operation affects only its intended record.
/// </summary>
[Collection("Idempotency Persistence Store Tests")]
public class IdempotencyPersistenceStoreTests
{
    #region Helper Classes

    /// <summary>
    /// Result of a persistence store operation for tracking test outcomes.
    /// </summary>
    private class PersistenceOperationResult
    {
        public int ThreadIndex { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
        public DataRecord? RetrievedRecord { get; set; }
    }

    /// <summary>
    /// Creates a configured ThreadSafeInMemoryPersistenceStore for testing.
    /// </summary>
    private static ThreadSafeInMemoryPersistenceStore CreateConfiguredStore()
    {
        var store = new ThreadSafeInMemoryPersistenceStore();
        var options = new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("id")
            .WithExpiration(TimeSpan.FromMinutes(5))
            .Build();
        store.Configure(options, "TestFunction", null);
        return store;
    }

    /// <summary>
    /// Creates a DataRecord for testing purposes.
    /// </summary>
    private static DataRecord CreateTestRecord(string key, DataRecord.DataRecordStatus status, string? responseData = null)
    {
        return new DataRecord(
            key,
            status,
            DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
            responseData ?? $"response_{key}",
            $"hash_{key}"
        );
    }

    #endregion


    #region Property 4: Persistence Store Operations Thread Safety

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 4: Persistence Store Operations Thread Safety**
    /// *For any* set of concurrent persistence store operations (SaveInProgress, SaveSuccess, GetRecord, DeleteRecord)
    /// with different idempotency keys, all operations should complete without throwing concurrency-related exceptions
    /// and each operation should affect only its intended record.
    /// **Validates: Requirements 1.3, 3.1, 3.2, 3.3, 3.4, 3.5**
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(5, 10)]
    [InlineData(10, 20)]
    public void PersistenceStoreThreadSafety_ConcurrentOperations_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        var store = CreateConfiguredStore();
        var results = new ConcurrentBag<PersistenceOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

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
                        var result = new PersistenceOperationResult { ThreadIndex = threadIndex };
                        string key = $"key_{threadIndex}_{op}";
                        result.IdempotencyKey = key;
                        
                        try
                        {
                            // Mix of operations: PutRecord, GetRecord, UpdateRecord, DeleteRecord
                            int opType = (threadIndex + op) % 4;
                            var record = CreateTestRecord(key, DataRecord.DataRecordStatus.INPROGRESS);
                            
                            switch (opType)
                            {
                                case 0: // PutRecord
                                    result.OperationType = "PutRecord";
                                    await store.PutRecord(record, DateTimeOffset.UtcNow);
                                    break;
                                case 1: // GetRecord
                                    result.OperationType = "GetRecord";
                                    result.RetrievedRecord = await store.GetRecord(key);
                                    break;
                                case 2: // UpdateRecord
                                    result.OperationType = "UpdateRecord";
                                    var completedRecord = CreateTestRecord(key, DataRecord.DataRecordStatus.COMPLETED, $"result_{threadIndex}_{op}");
                                    await store.UpdateRecord(completedRecord);
                                    break;
                                case 3: // DeleteRecord
                                    result.OperationType = "DeleteRecord";
                                    await store.DeleteRecord(key);
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
                    results.Add(new PersistenceOperationResult
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
            $"Thread {r.ThreadIndex} {r.OperationType} for key '{r.IdempotencyKey}' failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 4a: Concurrent SaveInProgress Operations**
    /// *For any* set of concurrent SaveInProgress operations with different keys,
    /// all operations should complete without interference.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void PersistenceStoreThreadSafety_ConcurrentSaveInProgress_ShouldCompleteWithoutInterference(int concurrencyLevel)
    {
        var store = CreateConfiguredStore();
        var results = new ConcurrentBag<PersistenceOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new PersistenceOperationResult 
                { 
                    ThreadIndex = threadIndex,
                    OperationType = "PutRecord",
                    IdempotencyKey = $"inprogress_key_{threadIndex}"
                };

                try
                {
                    barrier.SignalAndWait();

                    var record = CreateTestRecord(result.IdempotencyKey, DataRecord.DataRecordStatus.INPROGRESS);
                    await store.PutRecord(record, DateTimeOffset.UtcNow);
                    
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

        // All operations should succeed
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
        
        // Verify all records were saved
        for (int i = 0; i < concurrencyLevel; i++)
        {
            Assert.True(store.ContainsKey($"inprogress_key_{i}"),
                $"Record for key 'inprogress_key_{i}' was not saved");
        }
    }


    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 4b: Concurrent SaveSuccess Operations**
    /// *For any* set of concurrent SaveSuccess operations with different keys,
    /// all records should be persisted correctly.
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void PersistenceStoreThreadSafety_ConcurrentSaveSuccess_ShouldPersistAllRecords(int concurrencyLevel)
    {
        var store = CreateConfiguredStore();
        var results = new ConcurrentBag<PersistenceOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var expectedResponses = new ConcurrentDictionary<string, string>();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                string key = $"success_key_{threadIndex}";
                string responseData = $"response_data_{threadIndex}";
                expectedResponses[key] = responseData;
                
                var result = new PersistenceOperationResult 
                { 
                    ThreadIndex = threadIndex,
                    OperationType = "UpdateRecord",
                    IdempotencyKey = key
                };

                try
                {
                    barrier.SignalAndWait();

                    var record = CreateTestRecord(key, DataRecord.DataRecordStatus.COMPLETED, responseData);
                    await store.UpdateRecord(record);
                    
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

        // All operations should succeed
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
        
        // Verify all records were saved with correct data
        foreach (var kvp in expectedResponses)
        {
            Assert.True(store.TryGetRecord(kvp.Key, out var record),
                $"Record for key '{kvp.Key}' was not found");
            Assert.Equal(kvp.Value, record?.ResponseData);
        }
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 4c: Concurrent GetRecord Operations**
    /// *For any* set of concurrent GetRecord operations for different keys,
    /// each operation should return the correct record for its key.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void PersistenceStoreThreadSafety_ConcurrentGetRecord_ShouldReturnCorrectRecords(int concurrencyLevel)
    {
        var store = CreateConfiguredStore();
        
        // Pre-populate store with known records
        var expectedRecords = new Dictionary<string, DataRecord>();
        for (int i = 0; i < concurrencyLevel; i++)
        {
            string key = $"get_key_{i}";
            var record = CreateTestRecord(key, DataRecord.DataRecordStatus.COMPLETED, $"response_{i}");
            store.PutRecord(record, DateTimeOffset.UtcNow).Wait();
            expectedRecords[key] = record;
        }

        var results = new ConcurrentBag<PersistenceOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                string key = $"get_key_{threadIndex}";
                var result = new PersistenceOperationResult 
                { 
                    ThreadIndex = threadIndex,
                    OperationType = "GetRecord",
                    IdempotencyKey = key
                };

                try
                {
                    barrier.SignalAndWait();

                    result.RetrievedRecord = await store.GetRecord(key);
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

        // All operations should succeed
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
        
        // Verify all retrieved records match expected
        foreach (var r in results)
        {
            Assert.NotNull(r.RetrievedRecord);
            var expected = expectedRecords[r.IdempotencyKey];
            Assert.Equal(expected.IdempotencyKey, r.RetrievedRecord.IdempotencyKey);
            Assert.Equal(expected.ResponseData, r.RetrievedRecord.ResponseData);
        }
    }


    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 4d: Concurrent DeleteRecord Operations**
    /// *For any* set of concurrent DeleteRecord operations for different keys,
    /// only the specified records should be deleted.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void PersistenceStoreThreadSafety_ConcurrentDeleteRecord_ShouldDeleteOnlySpecifiedRecords(int concurrencyLevel)
    {
        var store = CreateConfiguredStore();
        
        // Pre-populate store with records to delete and records to keep
        var keysToDelete = new List<string>();
        var keysToKeep = new List<string>();
        
        for (int i = 0; i < concurrencyLevel; i++)
        {
            string deleteKey = $"delete_key_{i}";
            string keepKey = $"keep_key_{i}";
            
            var deleteRecord = CreateTestRecord(deleteKey, DataRecord.DataRecordStatus.COMPLETED);
            var keepRecord = CreateTestRecord(keepKey, DataRecord.DataRecordStatus.COMPLETED);
            
            store.PutRecord(deleteRecord, DateTimeOffset.UtcNow).Wait();
            store.PutRecord(keepRecord, DateTimeOffset.UtcNow).Wait();
            
            keysToDelete.Add(deleteKey);
            keysToKeep.Add(keepKey);
        }

        var results = new ConcurrentBag<PersistenceOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                string key = $"delete_key_{threadIndex}";
                var result = new PersistenceOperationResult 
                { 
                    ThreadIndex = threadIndex,
                    OperationType = "DeleteRecord",
                    IdempotencyKey = key
                };

                try
                {
                    barrier.SignalAndWait();

                    await store.DeleteRecord(key);
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

        // All operations should succeed
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} failed: {r.ExceptionType}: {r.ExceptionMessage}"));
        
        // Verify deleted keys are gone
        foreach (var key in keysToDelete)
        {
            Assert.False(store.ContainsKey(key), $"Key '{key}' should have been deleted");
        }
        
        // Verify kept keys still exist
        foreach (var key in keysToKeep)
        {
            Assert.True(store.ContainsKey(key), $"Key '{key}' should still exist");
        }
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 4e: Mixed Concurrent Operations**
    /// *For any* combination of concurrent save, get, and delete operations,
    /// data integrity should be maintained across all operations.
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    public void PersistenceStoreThreadSafety_MixedOperations_ShouldMaintainDataIntegrity(int concurrencyLevel)
    {
        var store = CreateConfiguredStore();
        var results = new ConcurrentBag<PersistenceOperationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        // Pre-populate some records
        for (int i = 0; i < concurrencyLevel / 2; i++)
        {
            var record = CreateTestRecord($"existing_key_{i}", DataRecord.DataRecordStatus.COMPLETED);
            store.PutRecord(record, DateTimeOffset.UtcNow).Wait();
        }

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new PersistenceOperationResult { ThreadIndex = threadIndex };

                try
                {
                    barrier.SignalAndWait();

                    // Different threads do different operations
                    int opType = threadIndex % 4;
                    
                    switch (opType)
                    {
                        case 0: // Put new record
                            result.OperationType = "PutRecord";
                            result.IdempotencyKey = $"new_key_{threadIndex}";
                            var newRecord = CreateTestRecord(result.IdempotencyKey, DataRecord.DataRecordStatus.INPROGRESS);
                            await store.PutRecord(newRecord, DateTimeOffset.UtcNow);
                            break;
                            
                        case 1: // Get existing record
                            result.OperationType = "GetRecord";
                            result.IdempotencyKey = $"existing_key_{threadIndex % (concurrencyLevel / 2)}";
                            result.RetrievedRecord = await store.GetRecord(result.IdempotencyKey);
                            break;
                            
                        case 2: // Update existing record
                            result.OperationType = "UpdateRecord";
                            result.IdempotencyKey = $"existing_key_{threadIndex % (concurrencyLevel / 2)}";
                            var updateRecord = CreateTestRecord(result.IdempotencyKey, DataRecord.DataRecordStatus.COMPLETED, $"updated_{threadIndex}");
                            await store.UpdateRecord(updateRecord);
                            break;
                            
                        case 3: // Delete (non-existing key to avoid affecting other tests)
                            result.OperationType = "DeleteRecord";
                            result.IdempotencyKey = $"delete_target_{threadIndex}";
                            await store.DeleteRecord(result.IdempotencyKey);
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
            });
        }

        Task.WaitAll(tasks);

        // All operations should complete without exceptions
        Assert.All(results, r => Assert.True(r.Success && !r.ExceptionThrown,
            $"Thread {r.ThreadIndex} {r.OperationType} for key '{r.IdempotencyKey}' failed: {r.ExceptionType}: {r.ExceptionMessage}"));
    }

    #endregion


    #region Additional Stress Tests

    /// <summary>
    /// High concurrency stress test for persistence store operations.
    /// </summary>
    [Theory]
    [InlineData(20, 50)]
    [InlineData(30, 30)]
    public void PersistenceStoreThreadSafety_HighConcurrency_ShouldCompleteWithoutExceptions(int concurrencyLevel, int operationsPerThread)
    {
        var store = CreateConfiguredStore();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

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
                        int opType = (threadIndex + op) % 4;
                        string key = $"stress_key_{threadIndex}_{op}";
                        var record = CreateTestRecord(key, DataRecord.DataRecordStatus.INPROGRESS);
                        
                        switch (opType)
                        {
                            case 0:
                                await store.PutRecord(record, DateTimeOffset.UtcNow);
                                break;
                            case 1:
                                await store.GetRecord(key);
                                break;
                            case 2:
                                var completedRecord = CreateTestRecord(key, DataRecord.DataRecordStatus.COMPLETED);
                                await store.UpdateRecord(completedRecord);
                                break;
                            case 3:
                                await store.DeleteRecord(key);
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
    /// Test that verifies operation counters are correctly incremented under concurrent access.
    /// </summary>
    [Fact]
    public void PersistenceStoreThreadSafety_OperationCounters_ShouldBeAccurate()
    {
        var store = CreateConfiguredStore();
        int concurrencyLevel = 10;
        int operationsPerThread = 20;
        
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                barrier.SignalAndWait();

                for (int op = 0; op < operationsPerThread; op++)
                {
                    string key = $"counter_key_{threadIndex}_{op}";
                    var record = CreateTestRecord(key, DataRecord.DataRecordStatus.COMPLETED);
                    
                    await store.PutRecord(record, DateTimeOffset.UtcNow);
                    await store.GetRecord(key);
                    await store.UpdateRecord(record);
                    await store.DeleteRecord(key);
                }
            });
        }

        Task.WaitAll(tasks);

        int expectedOperations = concurrencyLevel * operationsPerThread;
        
        Assert.Equal(expectedOperations, store.PutRecordCount);
        Assert.Equal(expectedOperations, store.GetRecordCount);
        Assert.Equal(expectedOperations, store.UpdateRecordCount);
        Assert.Equal(expectedOperations, store.DeleteRecordCount);
    }

    /// <summary>
    /// Test that verifies concurrent operations on the same key are handled correctly.
    /// </summary>
    [Fact]
    public void PersistenceStoreThreadSafety_SameKeyConcurrentOperations_ShouldNotCorruptData()
    {
        var store = CreateConfiguredStore();
        int concurrencyLevel = 10;
        string sharedKey = "shared_key";
        
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];
        var exceptions = new ConcurrentBag<Exception>();

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    barrier.SignalAndWait();

                    // All threads operate on the same key
                    var record = CreateTestRecord(sharedKey, DataRecord.DataRecordStatus.COMPLETED, $"response_{threadIndex}");
                    
                    await store.PutRecord(record, DateTimeOffset.UtcNow);
                    await store.GetRecord(sharedKey);
                    await store.UpdateRecord(record);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);

        // No exceptions should be thrown
        Assert.Empty(exceptions);
        
        // The key should still exist with valid data
        Assert.True(store.ContainsKey(sharedKey));
        Assert.True(store.TryGetRecord(sharedKey, out var finalRecord));
        Assert.NotNull(finalRecord);
        Assert.Equal(sharedKey, finalRecord.IdempotencyKey);
    }

    #endregion
}
