using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
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
        public DataRecord DataRecord;
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

        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().BeNull();
        dr.IdempotencyKey.Should().Be("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
        dr.PayloadHash.Should().BeEmpty();
        persistenceStore.Status.Should().Be(1);
    }

    [Fact]
    public async Task SaveInProgress_WhenRemainingTime_ShouldSaveRecordInStore()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        var now = DateTimeOffset.UtcNow;
        var lambdaTimeoutMs = 30000;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, lambdaTimeoutMs);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().BeNull();
        dr.IdempotencyKey.Should().Be("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
        dr.PayloadHash.Should().BeEmpty();
        dr.InProgressExpiryTimestamp.Should().Be(now.AddMilliseconds(lambdaTimeoutMs).ToUnixTimeMilliseconds());
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
            .Build(), "myfunc", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

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
    public async Task
        SaveInProgress_WhenKeyJmesPathIsSetToMultipleFields_ShouldSaveRecordInStore_WithIdempotencyKeyEqualsKeyJmesPath()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).[id, message]") //[43876123454654,"Lambda rocks"]
            .Build(), "myfunc", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

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
            .Build(), "", null);
        var now = DateTimeOffset.UtcNow;

        // Act
        var act = async () =>
            await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

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
            .Build(), "", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

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

        LRUCache<string, DataRecord> cache = new(2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .Build(), null, null, cache);

        var now = DateTimeOffset.UtcNow;
        cache.Set("testFunction#2fef178cc82be5ce3da6c5e0466a6182",
            new DataRecord(
                "testFunction#2fef178cc82be5ce3da6c5e0466a6182",
                DataRecord.DataRecordStatus.INPROGRESS,
                now.AddSeconds(3600).ToUnixTimeSeconds(),
                null, null)
        );

        // Act
        var act = () => persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

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

        LRUCache<string, DataRecord> cache = new(2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .WithUseLocalCache(true)
            .WithExpiration(TimeSpan.FromSeconds(2))
            .Build(), null, null, cache);

        var now = DateTimeOffset.UtcNow;
        cache.Set("testFunction#2fef178cc82be5ce3da6c5e0466a6182",
            new DataRecord(
                "testFunction#2fef178cc82be5ce3da6c5e0466a6182",
                DataRecord.DataRecordStatus.INPROGRESS,
                now.AddSeconds(-3).ToUnixTimeSeconds(),
                null, null)
        );

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

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
        LRUCache<string, DataRecord> cache = new(2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null, cache);

        var product = new Product(34543, "product", 42);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveSuccess(JsonSerializer.SerializeToDocument(request)!, product, now);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        dr.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        dr.ResponseData.Should().Be(IdempotencySerializer.Serialize(product, typeof(Product)));
        dr.IdempotencyKey.Should().Be("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
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
        LRUCache<string, DataRecord> cache = new(2);

        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), null, null, cache);

        var product = new Product(34543, "product", 42);
        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveSuccess(JsonSerializer.SerializeToDocument(request)!, product, now);

        // Assert
        persistenceStore.Status.Should().Be(2);
        cache.Count.Should().Be(1);

        var foundDataRecord = cache.TryGet("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6", out var record);
        foundDataRecord.Should().BeTrue();
        record.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        record.ExpiryTimestamp.Should().Be(now.AddSeconds(3600).ToUnixTimeSeconds());
        record.ResponseData.Should().Be(IdempotencySerializer.Serialize(product, typeof(Product)));
        record.IdempotencyKey.Should().Be("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
        record.PayloadHash.Should().BeEmpty();
    }

    /// Get Record
    [Fact]
    public async Task GetRecord_WhenRecordIsInStore_ShouldReturnRecordFromPersistence()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        LRUCache<string, DataRecord> cache = new(2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "myfunc", null, cache);

        var now = DateTimeOffset.UtcNow;

        // Act
        var record = await persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);

        // Assert
        record.IdempotencyKey.Should().Be("testFunction.myfunc#5eff007a9ed2789a9f9f6bc182fc6ae6");
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
        LRUCache<string, DataRecord> cache = new(2);

        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), "myfunc", null, cache);

        var now = DateTimeOffset.UtcNow;
        var dr = new DataRecord(
            "testFunction.myfunc#5eff007a9ed2789a9f9f6bc182fc6ae6",
            DataRecord.DataRecordStatus.COMPLETED,
            now.AddSeconds(3600).ToUnixTimeSeconds(),
            "result of the function",
            null);
        cache.Set("testFunction.myfunc#5eff007a9ed2789a9f9f6bc182fc6ae6", dr);

        // Act
        var record = await persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);

        // Assert
        record.IdempotencyKey.Should().Be("testFunction.myfunc#5eff007a9ed2789a9f9f6bc182fc6ae6");
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
        LRUCache<string, DataRecord> cache = new(2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), "myfunc", null, cache);

        var now = DateTimeOffset.UtcNow;
        var dr = new DataRecord(
            "testFunction.myfunc#5eff007a9ed2789a9f9f6bc182fc6ae6",
            DataRecord.DataRecordStatus.COMPLETED,
            now.AddSeconds(-3).ToUnixTimeSeconds(),
            "result of the function",
            null);
        cache.Set("testFunction.myfunc#5eff007a9ed2789a9f9f6bc182fc6ae6", dr);

        // Act
        var record = await persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);

        // Assert
        record.IdempotencyKey.Should().Be("testFunction.myfunc#5eff007a9ed2789a9f9f6bc182fc6ae6");
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
            "myfunc", null);

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

        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

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
        LRUCache<string, DataRecord> cache = new(2);
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true).Build(), null, null, cache);

        cache.Set("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6",
            new DataRecord("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6",
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
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);
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
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);
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
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);
        var expectedHash = "bb84c94278119c8838649706df4db42b"; // MD5(256.42)

        // Act
        var generatedHash = persistenceStore.GenerateHash(JsonDocument.Parse("256.42").RootElement);

        // Assert
        generatedHash.Should().Be(expectedHash);
    }

    [Fact]
    public async Task When_Key_Prefix_Set_Should_Create_With_Prefix()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .Build(), "myfunc", "MyCustomPrefixKey");

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().Be("MyCustomPrefixKey#2fef178cc82be5ce3da6c5e0466a6182");
    }

    private static APIGatewayProxyRequest LoadApiGatewayProxyRequest()
    {
        var eventJson = File.ReadAllText("./resources/apigw_event.json");
        try
        {
            IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);
            var request = IdempotencySerializer.Deserialize<APIGatewayProxyRequest>(eventJson);
            return request!;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Fact]
    public async Task ProcessExistingRecord_WhenValidRecord_ShouldReturnRecordAndSaveToCache()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new(2);

        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .Build(), null, null, cache);

        var now = DateTimeOffset.UtcNow;
        var existingRecord = new DataRecord(
            "testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6",
            DataRecord.DataRecordStatus.COMPLETED,
            now.AddSeconds(3600).ToUnixTimeSeconds(),
            "existing response",
            null);

        // Act
        var result =
            persistenceStore.ProcessExistingRecord(existingRecord, JsonSerializer.SerializeToDocument(request)!);

        // Assert
        result.Should().Be(existingRecord);
        cache.Count.Should().Be(1);
        cache.TryGet("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6", out var cachedRecord).Should().BeTrue();
        cachedRecord.Should().Be(existingRecord);
    }

    #region Configure Code Coverage Tests

    [Fact]
    public async Task Configure_WhenUseLocalCacheIsFalse_ShouldNotCreateCache()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with UseLocalCache = false (default)
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(false)
            .Build(), null, null);

        var now = DateTimeOffset.UtcNow;

        // Act - SaveSuccess should work without cache
        var product = new Product(34543, "product", 42);
        await persistenceStore.SaveSuccess(JsonSerializer.SerializeToDocument(request)!, product, now);

        // Assert - Record should be saved to persistence store
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        dr.IdempotencyKey.Should().Be("testFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
        persistenceStore.Status.Should().Be(2);
    }

    [Fact]
    public async Task Configure_WhenUseLocalCacheIsTrue_ShouldCreateCacheWithPublicMethod()
    {
        // Arrange - This test covers the positive path: if (useLocalCache) { _cache = new LRUCache... }
        // Using the PUBLIC Configure method (not the internal one with cache parameter)
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with UseLocalCache = true using the PUBLIC method
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .Build(), null, null);

        var now = DateTimeOffset.UtcNow;

        // Act - SaveSuccess should save to the internally created cache
        var product = new Product(34543, "product", 42);
        await persistenceStore.SaveSuccess(JsonSerializer.SerializeToDocument(request)!, product, now);

        // Assert - Record should be saved to persistence store
        var dr = persistenceStore.DataRecord;
        dr.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        persistenceStore.Status.Should().Be(2);

        // Verify cache is working by getting the record (should come from cache, not persistence)
        var record = await persistenceStore.GetRecord(JsonSerializer.SerializeToDocument(request)!, now);
        record.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        // Status should still be 2 (not 0) because record came from cache, not from GetRecord override
        persistenceStore.Status.Should().Be(2);
    }

    [Fact]
    public async Task Configure_WhenKeyPrefixIsSet_ShouldUsePrefixAsFunctionName()
    {
        // Arrange - This test covers the positive path: if (!string.IsNullOrEmpty(keyPrefix)) { _functionName = keyPrefix; }
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with a non-empty keyPrefix
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "ignoredFunctionName", "MyKeyPrefix");

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - IdempotencyKey should use the keyPrefix, not the functionName
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().StartWith("MyKeyPrefix#");
        dr.IdempotencyKey.Should().NotContain("ignoredFunctionName");
    }

    [Fact]
    public async Task Configure_WhenPayloadValidationJmesPathIsSet_ShouldEnablePayloadValidation()
    {
        // Arrange - This test covers the positive path: if (!string.IsNullOrWhiteSpace(...PayloadValidationJmesPath)) { PayloadValidationEnabled = true; }
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with a valid PayloadValidationJmesPath
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .WithPayloadValidationJmesPath("powertools_json(Body).message")
            .Build(), "myfunc", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - PayloadHash should NOT be empty when validation IS enabled
        var dr = persistenceStore.DataRecord;
        dr.PayloadHash.Should().NotBeEmpty();
        // The hash should be the MD5 of "Lambda rocks" (the message in the test payload)
        dr.PayloadHash.Should().Be("70c24d88041893f7fbab4105b76fd9e1");
    }

    [Fact]
    public async Task Configure_WhenKeyPrefixIsNull_ShouldUseFunctionNameFromEnvironment()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with null keyPrefix - should use default function name
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "myFunction", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - IdempotencyKey should include the function name
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().Contain("myFunction");
        dr.IdempotencyKey.Should().Be("testFunction.myFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
    }

    [Fact]
    public async Task Configure_WhenKeyPrefixIsEmpty_ShouldUseFunctionNameFromEnvironment()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with empty keyPrefix - should use default function name
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "anotherFunction", "");

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - IdempotencyKey should include the function name
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().Contain("anotherFunction");
        dr.IdempotencyKey.Should().Be("testFunction.anotherFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
    }

    [Fact]
    public async Task Configure_WhenPayloadValidationJmesPathIsNull_ShouldNotEnablePayloadValidation()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure without PayloadValidationJmesPath
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .Build(), "myfunc", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - PayloadHash should be empty when validation is not enabled
        var dr = persistenceStore.DataRecord;
        dr.PayloadHash.Should().BeEmpty();
    }

    [Fact]
    public async Task Configure_WhenPayloadValidationJmesPathIsEmpty_ShouldNotEnablePayloadValidation()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with empty PayloadValidationJmesPath
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .WithPayloadValidationJmesPath("")
            .Build(), "myfunc", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - PayloadHash should be empty when validation is not enabled
        var dr = persistenceStore.DataRecord;
        dr.PayloadHash.Should().BeEmpty();
    }

    [Fact]
    public async Task Configure_WhenPayloadValidationJmesPathIsWhitespace_ShouldNotEnablePayloadValidation()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with whitespace PayloadValidationJmesPath
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithEventKeyJmesPath("powertools_json(Body).id")
            .WithPayloadValidationJmesPath("   ")
            .Build(), "myfunc", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - PayloadHash should be empty when validation is not enabled
        var dr = persistenceStore.DataRecord;
        dr.PayloadHash.Should().BeEmpty();
    }

    [Fact]
    public async Task Configure_WhenFunctionNameIsNullAndKeyPrefixIsNull_ShouldUseDefaultFunctionName()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with both functionName and keyPrefix as null
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), null, null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - IdempotencyKey should use default "testFunction"
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().StartWith("testFunction#");
    }

    [Fact]
    public async Task Configure_WhenFunctionNameIsWhitespace_ShouldUseDefaultFunctionNameOnly()
    {
        // Arrange
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // Configure with whitespace functionName
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "   ", null);

        var now = DateTimeOffset.UtcNow;

        // Act
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - IdempotencyKey should use default "testFunction" without appending whitespace
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().StartWith("testFunction#");
        dr.IdempotencyKey.Should().NotContain("testFunction.   ");
    }

    [Fact]
    public async Task Configure_WhenCalledMultipleTimes_ShouldOnlyConfigureOnceButUpdateFunctionName()
    {
        // Arrange - This test covers the fast path: if (_isConfigured) { SetFullFunctionName(...); return; }
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // First configuration
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "firstFunction", null);

        var now = DateTimeOffset.UtcNow;

        // Act - Call configure again with different function name (simulates multiple idempotent methods)
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "secondFunction", null);
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - Should use the second function name (SetFullFunctionName was called in fast path)
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().Contain("secondFunction");
        dr.IdempotencyKey.Should().Be("testFunction.secondFunction#5eff007a9ed2789a9f9f6bc182fc6ae6");
    }

    [Fact]
    public async Task Configure_WhenCalledMultipleTimesWithKeyPrefix_ShouldUpdateKeyPrefix()
    {
        // Arrange - This test covers the fast path with keyPrefix: if (_isConfigured) { SetFullFunctionName(...); return; }
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();

        // First configuration with keyPrefix
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "function", "FirstPrefix");

        var now = DateTimeOffset.UtcNow;

        // Act - Call configure again with different keyPrefix
        persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(), "function", "SecondPrefix");
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - Should use the second keyPrefix
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().StartWith("SecondPrefix#");
        dr.IdempotencyKey.Should().NotContain("FirstPrefix");
    }

    [Fact]
    public async Task Configure_InternalMethod_WhenCalledMultipleTimes_ShouldOnlyConfigureOnceButUpdateFunctionName()
    {
        // Arrange - This test covers the internal Configure method's fast path
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new(2);

        // First configuration using internal method
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .Build(), "firstFunction", null, cache);

        var now = DateTimeOffset.UtcNow;

        // Act - Call configure again with different function name
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .Build(), "secondFunction", null, cache);
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - Should use the second function name
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().Contain("secondFunction");
    }

    [Fact]
    public async Task Configure_InternalMethod_WhenCalledMultipleTimesWithKeyPrefix_ShouldUpdateKeyPrefix()
    {
        // Arrange - This test covers the internal Configure method's fast path with keyPrefix
        var persistenceStore = new InMemoryPersistenceStore();
        var request = LoadApiGatewayProxyRequest();
        LRUCache<string, DataRecord> cache = new(2);

        // First configuration with keyPrefix using internal method
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .Build(), "function", "FirstPrefix", cache);

        var now = DateTimeOffset.UtcNow;

        // Act - Call configure again with different keyPrefix
        persistenceStore.Configure(new IdempotencyOptionsBuilder()
            .WithUseLocalCache(true)
            .Build(), "function", "SecondPrefix", cache);
        await persistenceStore.SaveInProgress(JsonSerializer.SerializeToDocument(request)!, now, null);

        // Assert - Should use the second keyPrefix
        var dr = persistenceStore.DataRecord;
        dr.IdempotencyKey.Should().StartWith("SecondPrefix#");
    }

    #endregion
}