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

using AWS.Lambda.Powertools.Idempotency.Exceptions;

namespace AWS.Lambda.Powertools.Idempotency.Persistence;

/// <summary>
/// Persistence layer that will store the idempotency result.
/// In order to provide another implementation, extends {@link BasePersistenceStore}.
/// </summary>
public interface IPersistenceStore 
{

    /// <summary>
    /// Retrieve item from persistence store using idempotency key and return it as a DataRecord instance.
    /// </summary>
    /// <param name="idempotencyKey">idempotencyKey the key of the record</param>
    /// <returns>DataRecord representation of existing record found in persistence store</returns>
    /// <exception cref="IdempotencyItemNotFoundException">Exception thrown if no record exists in persistence store with the idempotency key</exception>
    Task<DataRecord> GetRecord(string idempotencyKey);
    
    /// <summary>
    /// Add a DataRecord to persistence store if it does not already exist with that key
    /// </summary>
    /// <param name="record">record DataRecord instance</param>
    /// <param name="now"></param>
    /// <returns></returns>
    /// <exception cref="IdempotencyItemAlreadyExistsException">if a non-expired entry already exists.</exception>
    Task PutRecord(DataRecord record, DateTimeOffset now);
    
    /// <summary>
    /// Update item in persistence store
    /// </summary>
    /// <param name="record">DataRecord instance</param>
    Task UpdateRecord(DataRecord record);

    /// <summary>
    /// Remove item from persistence store
    /// </summary>
    /// <param name="idempotencyKey">idempotencyKey the key of the record</param>
    Task DeleteRecord(string idempotencyKey);
}
