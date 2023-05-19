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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Persistence;

public class BasePersistenceStoreTests
{
    class InMemoryPersistenceStore : BasePersistenceStore
    {
        private string _validationHash = null;
        public DataRecord DataRecord = null;
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
    public async Task SaveInProgress_WhenDefaultConfig_ShouldSaveRecordInStore()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null);
        
        var now = DateTimeOffset.UtcNow;
        
        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().BeNull();
        dr.IdempotencyKey.Should().Be("testFunction#b105f675a45bab746c0723da594d3b06");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(1);
    }

    [Fact]
    public async Task SaveInProgress_WhenKeyJmesPathIsSet_ShouldSaveRecordInStore_WithIdempotencyKeyEqualsKeyJmesPath()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .Build(), "myfunc");

        var now = DateTimeOffset.UtcNow;
        
        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now);
        
        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().BeNull();
        dr.IdempotencyKey.Should().Be("testFunction.myfunc#2fef178cc82be5ce3da6c5e0466a6182");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(1);
    }
    
    [Fact]
    public async Task SaveInProgress_WhenKeyJmesPathIsSetToMultipleFields_ShouldSaveRecordInStore_WithIdempotencyKeyEqualsKeyJmesPath()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).[id, message]") //[43876123454654,"Lambda rocks"]
            .Build(), "myfunc");

        var now = DateTimeOffset.UtcNow;
        
        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now);
        
        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().BeNull();
        dr.IdempotencyKey.Should().Be("testFunction.myfunc#5ca4c8c44d427e9d43ca918a24d6cf42");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(1);
    }
    
    
    [Fact]
    public async Task SaveInProgress_WhenJMESPath_NotFound_ShouldThrowException()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("unavailable")
            .WithThrowOnNoIdempotencyKey(true) // should throw
            .Build(), "");
        var now = DateTimeOffset.UtcNow;
        
        // Act
        var act = async () => await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now);
        
        // Assert
        await act.Should()
            .ThrowAsync<IdempotencyKeyException>()
            .WithMessage("No data found to create a hashed idempotency key");
        
        persistenceStore.Status.Should().Be(-1);
    }
    
    [Fact]
    public async Task SaveInProgress_WhenJMESpath_NotFound_ShouldNotThrowException()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("unavailable")
            .Build(), "");
        
        var now = DateTimeOffset.UtcNow;
        
        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        persistenceStore.Status.Should().Be(1);
    }
    
    [Fact]
    public async Task SaveInProgress_WhenLocalCacheIsSet_AndNotExpired_ShouldThrowException()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .Build(), null, cache);
        
        var now = DateTimeOffset.UtcNow;
        cache.Set("testFunction#2fef178cc82be5ce3da6c5e0466a6182",
            new DataRecord(
                "testFunction#2fef178cc82be5ce3da6c5e0466a6182",
                DataRecord.DataRecordStatus.INPROGRESS,
                now.AddSeconds(3600).ToUnixTimeSeconds(),
                null, null)
        );
        
        // Act
        var act = () => persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now);

        // Assert
        await act.Should()
            .ThrowAsync<IdempotencyItemAlreadyExistsException>();

        persistenceStore.Status.Should().Be(-1);
    }
    
    [Fact]
    public async Task SaveInProgress_WhenLocalCacheIsSetButExpired_ShouldRemoveFromCache()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .WithUseLocalCache(true)
            .WithExpiration(TimeSpan.FromSeconds(2))
            .Build(), null, cache);
        
        var now = DateTimeOffset.UtcNow;
        cache.Set("testFunction#2fef178cc82be5ce3da6c5e0466a6182",
            new DataRecord(
                "testFunction#2fef178cc82be5ce3da6c5e0466a6182",
                DataRecord.DataRecordStatus.INPROGRESS,
                now.AddSeconds(-3).ToUnixTimeSeconds(),
                null, null)
        );
        
        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        cache.Count.Should().Be(0);
        persistenceStore.Status.Should().Be(1);
    }
    
    ////// Save Success
    
    [Fact]
    public async Task SaveSuccess_WhenDefaultConfig_ShouldUpdateRecord() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, cache);

        var product = new Product(34543, "product", 42);
        
        var now = DateTimeOffset.UtcNow;
        
        // Act
        await persistenceStore.SaveSuccess(JsonSerializer.SerializeToDocument(request)!, product, now);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().Be(JsonSerializer.Serialize(product));
        dr.IdempotencyKey.Should().Be("testFunction#b105f675a45bab746c0723da594d3b06");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(2);
        cache.Count.Should().Be(0);
    }
    
    [Fact]
    public async Task SaveSuccess_WhenCacheEnabled_ShouldSaveInCache()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new ((int) 2);
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), null, cache);

        var product = new Product(34543, "product", 42);
        var now = DateTimeOffset.UtcNow;
        
        // Act
        await persistenceStore.SaveSuccess(JsonSerializer.SerializeToDocument(request)!, product, now);

        // Assert
        persistenceStore.Status.Should().Be(2);
        cache.Count.Should().Be(1);
    
        var foundDataRecord = cache.TryGet("testFunction#b105f675a45bab746c0723da594d3b06", out var record);
        foundDataRecord.Should().BeTrue();
        record.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        record.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        record.ResponseData.Should().Be(JsonSerializer.Serialize(product));
        record.IdempotencyKey.Should().Be("testFunction#b105f675a45bab746c0723da594d3b06");
        record.PayloadHash.Should().BeEmpty();
    }
    
    /// Get Record
    
    [Fact]
    public async Task GetRecord_WhenRecordIsInStore_ShouldReturnRecordFromPersistence() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        LRUCache<string, DataRecord> cache = new((int) 2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "myfunc", cache);

        var now = DateTimeOffset.UtcNow;
        
        // Act
        var record = await persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);
        
        // Assert
        record.IdempotencyKey.Should().Be("testFunction.myfunc#b105f675a45bab746c0723da594d3b06");
        record.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        record.ResponseData.Should().Be("Response");
        persistenceStore.Status.Should().Be(0);
    }
    
    [Fact]
    public async Task GetRecord_WhenCacheEnabledNotExpired_ShouldReturnRecordFromCache() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new((int) 2);
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), "myfunc", cache);

        var now = DateTimeOffset.UtcNow;
        var dr = new DataRecord(
            "testFunction.myfunc#b105f675a45bab746c0723da594d3b06",
            DataRecord.DataRecordStatus.COMPLETED,
            now.AddSeconds(3600).ToUnixTimeSeconds(),
            "result of the function",
            null);
        cache.Set("testFunction.myfunc#b105f675a45bab746c0723da594d3b06", dr);

        // Act
        var record = await persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);
        
        // Assert
        record.IdempotencyKey.Should().Be("testFunction.myfunc#b105f675a45bab746c0723da594d3b06");
        record.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        record.ResponseData.Should().Be("result of the function");
        persistenceStore.Status.Should().Be(-1);
    }
    
    [Fact]
    public async Task GetRecord_WhenLocalCacheEnabledButRecordExpired_ShouldReturnRecordFromPersistence() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new((int) 2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), "myfunc", cache);

        var now = DateTimeOffset.UtcNow;
        var dr = new DataRecord(
            "testFunction.myfunc#b105f675a45bab746c0723da594d3b06",
            DataRecord.DataRecordStatus.COMPLETED,
            now.AddSeconds(-3).ToUnixTimeSeconds(),
            "result of the function",
            null);
        cache.Set("testFunction.myfunc#b105f675a45bab746c0723da594d3b06", dr);

        // Act
        var record = await persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);
        
        // Assert
        record.IdempotencyKey.Should().Be("testFunction.myfunc#b105f675a45bab746c0723da594d3b06");
        record.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        record.ResponseData.Should().Be("Response");
        persistenceStore.Status.Should().Be(0);
        cache.Count.Should().Be(0);
    }
    
    [Fact]
    public async Task GetRecord_WhenInvalidPayload_ShouldThrowValidationException()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
                .WithEventKeyJmesPath("powertools_json(Body).id")
                .WithPayloadValidationJmesPath("powertools_json(Body).message")
                .Build(),
            "myfunc");
        
        var now = DateTimeOffset.UtcNow;
        
        // Act
        Func<Task> act = () => persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);
        
        // Assert
        await act.Should().ThrowAsync<IdempotencyValidationException>();
    }
    
    // Delete Record
    [Fact]
    public async Task DeleteRecord_WhenRecordExist_ShouldDeleteRecordFromPersistence() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null);

        // Act
        await persistenceStore.DeleteRecord(JsonSerializer.SerializeToDocument(request)!, new ArithmeticException());
        
        // Assert
        persistenceStore.Status.Should().Be(3);
    }
    
    [Fact]
    public async Task DeleteRecord_WhenLocalCacheEnabled_ShouldDeleteRecordFromCache() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new ((int) 2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), null, cache);

        cache.Set("testFunction#b105f675a45bab746c0723da594d3b06",
            new DataRecord("testFunction#b105f675a45bab746c0723da594d3b06", 
                DataRecord.DataRecordStatus.COMPLETED,
                123,
                null, null));
        
        // Act
        await persistenceStore.DeleteRecord(JsonSerializer.SerializeToDocument(request)!, new ArithmeticException());
        
        // Assert
        persistenceStore.Status.Should().Be(3);
        cache.Count.Should().Be(0); 
    }
    
    [Fact]
    public void GenerateHash_WhenInputIsString_ShouldGenerateMd5ofString() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null);
        var expectedHash = "70c24d88041893f7fbab4105b76fd9e1"; // MD5(Lambda rocks)
        
        // Act
        var jsonValue = JsonValue.Create("Lambda rocks");
        var generatedHash = persistenceStore.GenerateHash(JsonDocument.Parse(jsonValue!.ToJsonString()).RootElement);
        
        // Assert
        generatedHash.Should().Be(expectedHash);
    }
    
    [Fact]
    public void GenerateHash_WhenInputIsObject_ShouldGenerateMd5ofJsonObject()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null);
        var product = new Product(42, "Product", 12);
        var expectedHash = "c83e720b399b3b4898c8734af177c53a"; // MD5({"Id":42,"Name":"Product","Price":12})
        
        // Act
        var jsonValue = JsonValue.Create(product);
        var generatedHash = persistenceStore.GenerateHash(JsonDocument.Parse(jsonValue!.ToJsonString()).RootElement);
        
        // Assert
        generatedHash.Should().Be(expectedHash);
    }

    [Fact]
    public void GenerateHash_WhenInputIsDouble_ShouldGenerateMd5ofDouble() 
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null);
        var expectedHash = "bb84c94278119c8838649706df4db42b"; // MD5(256.42)
        
        // Act
        var generatedHash = persistenceStore.GenerateHash(JsonDocument.Parse("256.42").RootElement);
        
        // Assert
        generatedHash.Should().Be(expectedHash);
    }
    
    private static APIGatewayProxyRequest LoadApiGatewayProxyRequest()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var eventJson = File.ReadAllText("./resources/apigw_event.json");
        var request = JsonSerializer.Deserialize<APIGatewayProxyRequest>(eventJson, options);
        return request!;
    }
}