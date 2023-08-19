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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Idempotency.Persistence;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

public class InMemoryPersistenceStore: BasePersistenceStore
{
    private readonly Dictionary<string, DataRecord> _records = new();
    public override Task<DataRecord> GetRecord(string idempotencyKey)
    {
        return Task.FromResult(_records.ContainsKey(idempotencyKey) ? _records[idempotencyKey] : null);
    }

    public override Task PutRecord(DataRecord record, DateTimeOffset now)
    {
        _records[record.IdempotencyKey] = record;
        return Task.CompletedTask;
    }

    public override Task UpdateRecord(DataRecord record)
    {
        _records[record.IdempotencyKey] = record;
        return Task.CompletedTask;
    }

    public override Task DeleteRecord(string idempotencyKey)
    {
        _records.Remove(idempotencyKey);
        return Task.CompletedTask;
    }
}