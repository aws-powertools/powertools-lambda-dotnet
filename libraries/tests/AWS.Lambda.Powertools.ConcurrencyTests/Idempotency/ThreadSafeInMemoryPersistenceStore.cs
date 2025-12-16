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
using AWS.Lambda.Powertools.Idempotency.Persistence;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Idempotency;

/// <summary>
/// A thread-safe in-memory persistence store for concurrency testing.
/// This implementation wraps all dictionary operations with proper synchronization
/// to ensure safe concurrent access from multiple threads.
/// 
/// This store is designed for testing purposes only and should not be used in production.
/// </summary>
public class ThreadSafeInMemoryPersistenceStore : BasePersistenceStore
{
    /// <summary>
    /// Thread-safe dictionary to store idempotency records.
    /// Using ConcurrentDictionary for atomic operations.
    /// </summary>
    private readonly ConcurrentDictionary<string, DataRecord> _records = new();

    /// <summary>
    /// Lock object for operations that require multiple steps to be atomic.
    /// </summary>
    private readonly object _lockObj = new();

    /// <summary>
    /// Counter for tracking the number of GetRecord operations (for testing purposes).
    /// </summary>
    private int _getRecordCount;

    /// <summary>
    /// Counter for tracking the number of PutRecord operations (for testing purposes).
    /// </summary>
    private int _putRecordCount;

    /// <summary>
    /// Counter for tracking the number of UpdateRecord operations (for testing purposes).
    /// </summary>
    private int _updateRecordCount;

    /// <summary>
    /// Counter for tracking the number of DeleteRecord operations (for testing purposes).
    /// </summary>
    private int _deleteRecordCount;

    /// <summary>
    /// Gets the number of GetRecord operations performed.
    /// </summary>
    public int GetRecordCount => _getRecordCount;

    /// <summary>
    /// Gets the number of PutRecord operations performed.
    /// </summary>
    public int PutRecordCount => _putRecordCount;

    /// <summary>
    /// Gets the number of UpdateRecord operations performed.
    /// </summary>
    public int UpdateRecordCount => _updateRecordCount;

    /// <summary>
    /// Gets the number of DeleteRecord operations performed.
    /// </summary>
    public int DeleteRecordCount => _deleteRecordCount;

    /// <summary>
    /// Gets the current number of records in the store.
    /// </summary>
    public int RecordCount => _records.Count;

    /// <inheritdoc />
    public override Task<DataRecord?> GetRecord(string idempotencyKey)
    {
        Interlocked.Increment(ref _getRecordCount);
        
        _records.TryGetValue(idempotencyKey, out var record);
        return Task.FromResult(record);
    }

    /// <inheritdoc />
    public override Task PutRecord(DataRecord record, DateTimeOffset now)
    {
        Interlocked.Increment(ref _putRecordCount);
        
        // Use AddOrUpdate to handle concurrent puts atomically
        _records.AddOrUpdate(
            record.IdempotencyKey,
            record,
            (_, _) => record);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task UpdateRecord(DataRecord record)
    {
        Interlocked.Increment(ref _updateRecordCount);
        
        // Use AddOrUpdate to handle concurrent updates atomically
        _records.AddOrUpdate(
            record.IdempotencyKey,
            record,
            (_, _) => record);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task DeleteRecord(string idempotencyKey)
    {
        Interlocked.Increment(ref _deleteRecordCount);
        
        _records.TryRemove(idempotencyKey, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all records from the store.
    /// Thread-safe operation.
    /// </summary>
    public void Clear()
    {
        _records.Clear();
    }

    /// <summary>
    /// Resets all operation counters to zero.
    /// Thread-safe operation.
    /// </summary>
    public void ResetCounters()
    {
        Interlocked.Exchange(ref _getRecordCount, 0);
        Interlocked.Exchange(ref _putRecordCount, 0);
        Interlocked.Exchange(ref _updateRecordCount, 0);
        Interlocked.Exchange(ref _deleteRecordCount, 0);
    }

    /// <summary>
    /// Gets all records currently in the store.
    /// Returns a snapshot of the records at the time of the call.
    /// </summary>
    /// <returns>A dictionary containing all records.</returns>
    public IDictionary<string, DataRecord> GetAllRecords()
    {
        return new Dictionary<string, DataRecord>(_records);
    }

    /// <summary>
    /// Checks if a record exists for the given idempotency key.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key to check.</param>
    /// <returns>True if a record exists, false otherwise.</returns>
    public bool ContainsKey(string idempotencyKey)
    {
        return _records.ContainsKey(idempotencyKey);
    }

    /// <summary>
    /// Tries to get a record for the given idempotency key.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key to look up.</param>
    /// <param name="record">The record if found, null otherwise.</param>
    /// <returns>True if the record was found, false otherwise.</returns>
    public bool TryGetRecord(string idempotencyKey, out DataRecord? record)
    {
        return _records.TryGetValue(idempotencyKey, out record);
    }
}
