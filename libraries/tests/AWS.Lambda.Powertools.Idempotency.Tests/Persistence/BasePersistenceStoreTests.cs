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
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Persistence;

public class BasePersistenceStoreTests
{
    class InMemoryPersistenceStore : BasePersistenceStore
    {
        private string _validationHash = null;
        public DataRecord? DataRecord = null;
        public int Status = -1;
        public override Task<DataRecord> GetRecord(string idempotencyKey)
        {
            Status = 0;
            var dataRecord = new DataRecord(
                idempotencyKey,
                DataRecord.DataRecordStatus.INPROGRESS,
                DateTimeOffset.UtcNow.AddSeconds(3600).ToUnixTimeSeconds(),
                "Response",
                _validationHash);
            return Task.FromResult(dataRecord);
        }

        public override Task PutRecord(DataRecord record, DateTimeOffset now)
        {
            DataRecord = record;
            Status = 1;
            return Task.CompletedTask;
        }

        public override Task UpdateRecord(DataRecord record)
        {
            DataRecord = record;
            Status = 2;
            return Task.CompletedTask;
        }

        public override Task DeleteRecord(string idempotencyKey)
        {
            DataRecord = null;
            Status = 3;
            return Task.CompletedTask;
        }
    }
    
    [Fact]
    public async Task SaveInProgress_DefaultConfig()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(IdempotencyConfig.Builder().Build(), null);
        
        DateTimeOffset now = DateTimeOffset.UtcNow;
        
        await persistenceStore.SaveInProgress(JToken.FromObject(request), now);

        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().BeNull();
        dr.IdempotencyKey.Should().Be("testFunction#36e3de9a3270f82fb957c645178dfab9");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(1);
    }

    

    [Fact]
    public async Task SaveInProgress_jmespath()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .Build(), "myfunc");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        await persistenceStore.SaveInProgress(JToken.FromObject(request), now);
        
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().BeNull();
        dr.IdempotencyKey.Should().Be("testFunction.myfunc#2fef178cc82be5ce3da6c5e0466a6182");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(1);
    }
    
    
    [Fact]
    public async Task SaveInProgress_JMESPath_NotFound_ShouldThrowException()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithEventKeyJmesPath("unavailable")
            .WithThrowOnNoIdempotencyKey(true) // should throw
            .Build(), "");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        
        Func<Task> act = async () => await persistenceStore.SaveInProgress(JToken.FromObject(request), now);
        await act.Should()
            .ThrowAsync<IdempotencyKeyException>()
            .WithMessage("No data found to create a hashed idempotency key");
        
        persistenceStore.Status.Should().Be(-1);
    }
    
    [Fact]
    public async Task SaveInProgress_JMESpath_NotFound_ShouldNotThrowException()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithEventKeyJmesPath("unavailable")
            .Build(), "");
        
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await persistenceStore.SaveInProgress(JToken.FromObject(request), now);

        DataRecord dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        persistenceStore.Status.Should().Be(1);
    }
    
    [Fact]
    public async Task SaveInProgress_WithLocalCache_NotExpired_ShouldThrowException()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithUseLocalCache(true)
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .Build(), null, cache);
        
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Set("testFunction#2fef178cc82be5ce3da6c5e0466a6182",
            new DataRecord(
                "testFunction#2fef178cc82be5ce3da6c5e0466a6182",
                DataRecord.DataRecordStatus.INPROGRESS,
                now.AddSeconds(3600).ToUnixTimeSeconds(),
                null, null)
        );
        
        Func<Task> act = () => persistenceStore.SaveInProgress(JToken.FromObject(request), now);

        await act.Should()
            .ThrowAsync<IdempotencyItemAlreadyExistsException>();

        persistenceStore.Status.Should().Be(-1);
    }
    
    [Fact]
    public async Task SaveInProgress_WithLocalCache_Expired_ShouldRemoveFromCache()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .WithUseLocalCache(true)
            .WithExpiration(TimeSpan.FromSeconds(2))
            .Build(), null, cache);
        
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Set("testFunction#2fef178cc82be5ce3da6c5e0466a6182",
            new DataRecord(
                "testFunction#2fef178cc82be5ce3da6c5e0466a6182",
                DataRecord.DataRecordStatus.INPROGRESS,
                now.AddSeconds(-3).ToUnixTimeSeconds(),
                null, null)
        );
        
        await persistenceStore.SaveInProgress(JToken.FromObject(request), now);

        DataRecord dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        cache.Count.Should().Be(0);
        persistenceStore.Status.Should().Be(1);
    }
    
    ////// Save Success
    
    [Fact]
    public async Task SaveSuccess_ShouldUpdateRecord() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(IdempotencyConfig.Builder().Build(), null, cache);

        Product product = new Product(34543, "product", 42);
        
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await persistenceStore.SaveSuccess(JToken.FromObject(request), product, now);

        DataRecord dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().Be(JsonConvert.SerializeObject(product));
        dr.IdempotencyKey.Should().Be("testFunction#36e3de9a3270f82fb957c645178dfab9");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(2);
        cache.Count.Should().Be(0);
    }
    
    [Fact]
    public async Task SaveSuccess_WithCacheEnabled_ShouldSaveInCache()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new ((int) 2);
        
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithUseLocalCache(true).Build(), null, cache);

        Product product = new Product(34543, "product", 42);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        
        await persistenceStore.SaveSuccess(JToken.FromObject(request), product, now);

        persistenceStore.Status.Should().Be(2);
        cache.Count.Should().Be(1);
    
        var foundDataRecord = cache.TryGet("testFunction#36e3de9a3270f82fb957c645178dfab9", out DataRecord record);
        foundDataRecord.Should().BeTrue();
        record.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        record.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        record.ResponseData.Should().Be(JsonConvert.SerializeObject(product));
        record.IdempotencyKey.Should().Be("testFunction#36e3de9a3270f82fb957c645178dfab9");
        record.PayloadHash.Should().BeEmpty();
    }
    
    /// Get Record
    
    [Fact]
    public async Task GetRecord_ShouldReturnRecordFromPersistence() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        LRUCache<string, DataRecord> cache = new((int) 2);
        persistenceStore.Configure(IdempotencyConfig.Builder().Build(), "myfunc", cache);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DataRecord record = await persistenceStore.GetRecord(JToken.FromObject(request), now);
        record.IdempotencyKey.Should().Be("testFunction.myfunc#36e3de9a3270f82fb957c645178dfab9");
        record.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        record.ResponseData.Should().Be("Response");
        persistenceStore.Status.Should().Be(0);
    }
    
    [Fact]

    public async Task GetRecord_CacheEnabledNotExpired_ShouldReturnRecordFromCache() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new((int) 2);
        
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithUseLocalCache(true).Build(), "myfunc", cache);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DataRecord dr = new DataRecord(
            "testFunction.myfunc#36e3de9a3270f82fb957c645178dfab9",
            DataRecord.DataRecordStatus.COMPLETED,
            now.AddSeconds(3600).ToUnixTimeSeconds(),
            "result of the function",
            null);
        cache.Set("testFunction.myfunc#36e3de9a3270f82fb957c645178dfab9", dr);

        DataRecord record = await persistenceStore.GetRecord(JToken.FromObject(request), now);
        record.IdempotencyKey.Should().Be("testFunction.myfunc#36e3de9a3270f82fb957c645178dfab9");
        record.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        record.ResponseData.Should().Be("result of the function");
        persistenceStore.Status.Should().Be(-1);
    }
    
    [Fact]
    public async Task GetRecord_CacheEnabledExpired_ShouldReturnRecordFromPersistence() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new((int) 2);
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithUseLocalCache(true).Build(), "myfunc", cache);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DataRecord dr = new DataRecord(
            "testFunction.myfunc#36e3de9a3270f82fb957c645178dfab9",
            DataRecord.DataRecordStatus.COMPLETED,
            now.AddSeconds(-3).ToUnixTimeSeconds(),
            "result of the function",
            null);
        cache.Set("testFunction.myfunc#36e3de9a3270f82fb957c645178dfab9", dr);

        DataRecord record = await persistenceStore.GetRecord(JToken.FromObject(request), now);
        record.IdempotencyKey.Should().Be("testFunction.myfunc#36e3de9a3270f82fb957c645178dfab9");
        record.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        record.ResponseData.Should().Be("Response");
        persistenceStore.Status.Should().Be(0);
        cache.Count.Should().Be(0);
    }
    
    [Fact]
    public async Task GetRecord_InvalidPayload_ShouldThrowValidationException()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(IdempotencyConfig.Builder()
                .WithEventKeyJmesPath("powertools_json(Body).id")
                .WithPayloadValidationJmesPath("powertools_json(Body).message")
                .Build(),
            "myfunc");

        var validationHash = "different hash"; // "Lambda rocks" ==> 70c24d88041893f7fbab4105b76fd9e1
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Func<Task> act = () => persistenceStore.GetRecord(JToken.FromObject(request), now);
        await act.Should().ThrowAsync<IdempotencyValidationException>();
    }
    
    // Delete Record
    [Fact]
    public async Task DeleteRecord_ShouldDeleteRecordFromPersistence() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(IdempotencyConfig.Builder().Build(), null);

        await persistenceStore.DeleteRecord(JToken.FromObject(request), new ArithmeticException());
        persistenceStore.Status.Should().Be(3);
    }
    
    [Fact]
    public async Task DeleteRecord_CacheEnabled_ShouldDeleteRecordFromCache() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(IdempotencyConfig.Builder()
            .WithUseLocalCache(true).Build(), null, cache);

        cache.Set("testFunction#36e3de9a3270f82fb957c645178dfab9",
            new DataRecord("testFunction#36e3de9a3270f82fb957c645178dfab9", 
                DataRecord.DataRecordStatus.COMPLETED,
                123,
                null, null));
        
        await persistenceStore.DeleteRecord(JToken.FromObject(request), new ArithmeticException());
        persistenceStore.Status.Should().Be(3);
        cache.Count.Should().Be(0); 
    }
    
    [Fact]
    public void GenerateHashString_ShouldGenerateMd5ofString() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        persistenceStore.Configure(IdempotencyConfig.Builder().Build(), null);
        string expectedHash = "70c24d88041893f7fbab4105b76fd9e1"; // MD5(Lambda rocks)
        string generatedHash = persistenceStore.GenerateHash(new JValue("Lambda rocks"));
        generatedHash.Should().Be(expectedHash);
    }
    
    [Fact]
    public void GenerateHashObject_ShouldGenerateMd5ofJsonObject()
    {
        var persistenceStore = new InMemoryPersistenceStore();
        persistenceStore.Configure(IdempotencyConfig.Builder().Build(), null);
        Product product = new Product(42, "Product", 12);
        string expectedHash = "87dd2e12074c65c9bac728795a6ebb45"; // MD5({"Id":42,"Name":"Product","Price":12.0})
        string generatedHash = persistenceStore.GenerateHash(JToken.FromObject(product));
        generatedHash.Should().Be(expectedHash);
    }

    [Fact]
    public void GenerateHashDouble_ShouldGenerateMd5ofDouble() 
    {
        var persistenceStore = new InMemoryPersistenceStore();
        persistenceStore.Configure(IdempotencyConfig.Builder().Build(), null);
        string expectedHash = "bb84c94278119c8838649706df4db42b"; // MD5(256.42)
        var generatedHash = persistenceStore.GenerateHash(new JValue(256.42));
        generatedHash.Should().Be(expectedHash);
    }
    
    private static APIGatewayProxyRequest LoadApiGatewayProxyRequest()
    {
        var eventJson = File.ReadAllText("./resources/apigw_event.json");
        var request = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(eventJson);
        return request!;
    }
}