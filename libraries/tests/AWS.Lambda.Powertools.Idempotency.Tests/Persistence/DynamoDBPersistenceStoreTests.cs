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
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Persistence;

[Collection("Sequential")]
[Trait("Category", "Integration")]
public class DynamoDbPersistenceStoreTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDBPersistenceStore _dynamoDbPersistenceStore;
    private readonly AmazonDynamoDBClient _client;
    private readonly string _tableName;

    public DynamoDbPersistenceStoreTests(DynamoDbFixture fixture)
    {
        _dynamoDbPersistenceStore = fixture.DynamoDbPersistenceStore;
        _client = fixture.Client;
        _tableName = fixture.TableName;
    }
    
    //putRecord
    [Fact]
    public async Task PutRecord_WhenRecordDoesNotExist_ShouldCreateRecordInDynamoDB()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddSeconds(3600).ToUnixTimeSeconds();
        var key = CreateKey("key");
        
        // Act
        await _dynamoDbPersistenceStore
            .PutRecord(new DataRecord("key", DataRecord.DataRecordStatus.COMPLETED, expiry, null, null), now);

        // Assert
        var getItemResponse =
            await _client.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = key
            });

        var item = getItemResponse.Item;
        item.Should().NotBeNull();
        item["status"].S.Should().Be("COMPLETED");
        item["expiration"].N.Should().Be(expiry.ToString());
    }

    [Fact]
    public async Task PutRecord_WhenRecordAlreadyExist_ShouldThrowIdempotencyItemAlreadyExistsException() 
    {
        // Arrange
        var key = CreateKey("key");

        // Insert a fake item with same id
        Dictionary<string, AttributeValue> item = new(key);
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddSeconds(30).ToUnixTimeMilliseconds();
        item.Add("expiration", new AttributeValue {N = expiry.ToString()});
        item.Add("status", new AttributeValue(DataRecord.DataRecordStatus.COMPLETED.ToString()));
        item.Add("data", new AttributeValue("Fake Data"));
        await _client.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
        var expiry2 = now.AddSeconds(3600).ToUnixTimeSeconds();

        // Act
        var act = () => _dynamoDbPersistenceStore.PutRecord(
            new DataRecord("key",
                DataRecord.DataRecordStatus.INPROGRESS,
                expiry2,
                null,
                null
            ), now);
        
        // Assert
        await act.Should().ThrowAsync<IdempotencyItemAlreadyExistsException>();
        
        // item was not updated, retrieve the initial one
        var itemInDb = (await _client.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = key
            })).Item;
        itemInDb.Should().NotBeNull();
        itemInDb["status"].S.Should().Be("COMPLETED");
        itemInDb["expiration"].N.Should().Be(expiry.ToString());
        itemInDb["data"].S.Should().Be("Fake Data");
    }
    
    //getRecord
    [Fact]
    public async Task GetRecord_WhenRecordExistsInDynamoDb_ShouldReturnExistingRecord()
    {
        // Arrange
        //await InitializeAsync();
        
        // Insert a fake item with same id
        Dictionary<string, AttributeValue> item = new()
        {
            {"id", new AttributeValue("key")} //key
        };
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddSeconds(30).ToUnixTimeMilliseconds();
        item.Add("expiration", new AttributeValue
        {
            N = expiry.ToString()
        });
        item.Add("status", new AttributeValue(DataRecord.DataRecordStatus.COMPLETED.ToString()));
        item.Add("data", new AttributeValue("Fake Data"));
        var _ = await _client.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });

        // Act
        var record = await _dynamoDbPersistenceStore.GetRecord("key");

        // Assert
        record.IdempotencyKey.Should().Be("key");
        record.Status.Should().Be(DataRecord.DataRecordStatus.COMPLETED);
        record.ResponseData.Should().Be("Fake Data");
        record.ExpiryTimestamp.Should().Be(expiry);
    }

    [Fact]
    public async Task GetRecord_WhenRecordIsAbsent_ShouldThrowException()
    {
        //Arrange
        await _dynamoDbPersistenceStore.DeleteRecord("key");
        
        // Act
        Func<Task> act = () => _dynamoDbPersistenceStore.GetRecord("key");
        
        // Assert
        await act.Should().ThrowAsync<IdempotencyItemNotFoundException>();
    }
    //updateRecord

    [Fact]
    public async Task UpdateRecord_WhenRecordExistsInDynamoDb_ShouldUpdateRecord()
    {
        // Arrange: Insert a fake item with same id
        var key = CreateKey("key");
        Dictionary<string, AttributeValue> item = new(key);
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddSeconds(360).ToUnixTimeMilliseconds();
        item.Add("expiration", new AttributeValue
        {
            N = expiry.ToString()
        });
        item.Add("status", new AttributeValue(DataRecord.DataRecordStatus.INPROGRESS.ToString()));
        await _client.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
        // enable payload validation
        _dynamoDbPersistenceStore.Configure(new IdempotencyOptionsBuilder().WithPayloadValidationJmesPath("path").Build(),
            null);

        // Act
        expiry = now.AddSeconds(3600).ToUnixTimeMilliseconds();
        var record = new DataRecord("key", DataRecord.DataRecordStatus.COMPLETED, expiry, "Fake result", "hash");
        await _dynamoDbPersistenceStore.UpdateRecord(record);

        // Assert
        var itemInDb = (await _client.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = key
        })).Item;

        itemInDb["status"].S.Should().Be("COMPLETED");
        itemInDb["expiration"].N.Should().Be(expiry.ToString());
        itemInDb["data"].S.Should().Be("Fake result");
        itemInDb["validation"].S.Should().Be("hash");
    }

    //deleteRecord
    [Fact]
    public async Task DeleteRecord_WhenRecordExistsInDynamoDb_ShouldDeleteRecord() 
    {
        // Arrange: Insert a fake item with same id
        var key = CreateKey("key");
        Dictionary<string, AttributeValue> item = new(key);
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddSeconds(360).ToUnixTimeMilliseconds();
        item.Add("expiration", new AttributeValue {N=expiry.ToString()});
        item.Add("status", new AttributeValue(DataRecord.DataRecordStatus.INPROGRESS.ToString()));
        await _client.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
        var scanResponse = await _client.ScanAsync(new ScanRequest
        {
            TableName = _tableName
        });
        scanResponse.Items.Count.Should().Be(1);

        // Act
        await _dynamoDbPersistenceStore.DeleteRecord("key");

        // Assert
        scanResponse = await _client.ScanAsync(new ScanRequest
        {
            TableName = _tableName
        });
        scanResponse.Items.Count.Should().Be(0);
    }

    [Fact]
    public async Task EndToEndWithCustomAttrNamesAndSortKey()
    {
        const string tableNameCustom = "idempotency_table_custom";
        try
        {
            var createTableRequest = new CreateTableRequest
            {
                TableName = tableNameCustom,
                KeySchema = new List<KeySchemaElement>
                {
                    new("key", KeyType.HASH),
                    new("sortkey", KeyType.RANGE)
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new("key", ScalarAttributeType.S),
                    new("sortkey", ScalarAttributeType.S)
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };
            await _client.CreateTableAsync(createTableRequest);
            var persistenceStore = new DynamoDBPersistenceStoreBuilder()
                .WithTableName(tableNameCustom)
                .WithDynamoDBClient(_client)
                .WithDataAttr("result")
                .WithExpiryAttr("expiry")
                .WithKeyAttr("key")
                .WithSortKeyAttr("sortkey")
                .WithStaticPkValue("pk")
                .WithStatusAttr("state")
                .WithValidationAttr("valid")
                .Build();
            persistenceStore.Configure(new IdempotencyOptionsBuilder().Build(),functionName: null);

            var now = DateTimeOffset.UtcNow;
            var record = new DataRecord(
                "mykey",
                DataRecord.DataRecordStatus.INPROGRESS,
                now.AddSeconds(400).ToUnixTimeMilliseconds(),
                null,
                null
            );
            // PUT
            await persistenceStore.PutRecord(record, now);

            Dictionary<string, AttributeValue> customKey = new()
            {
                { "key", new AttributeValue("pk") },
                { "sortkey", new AttributeValue("mykey") }
            };

            var itemInDb = (await _client.GetItemAsync(new GetItemRequest
            {
                TableName = tableNameCustom,
                Key = customKey
            })).Item;

            // GET
            var recordInDb = await persistenceStore.GetRecord("mykey");

            itemInDb.Should().NotBeNull();
            itemInDb["key"].S.Should().Be("pk");
            itemInDb["sortkey"].S.Should().Be(recordInDb.IdempotencyKey);
            itemInDb["state"].S.Should().Be(recordInDb.Status.ToString());
            itemInDb["expiry"].N.Should().Be(recordInDb.ExpiryTimestamp.ToString());

            // UPDATE
            var updatedRecord = new DataRecord(
                "mykey",
                DataRecord.DataRecordStatus.COMPLETED,
                now.AddSeconds(500).ToUnixTimeMilliseconds(),
                "response",
                null
            );
            await persistenceStore.UpdateRecord(updatedRecord);
            recordInDb = await persistenceStore.GetRecord("mykey");
            recordInDb.Should().Be(updatedRecord);

            // DELETE
            await persistenceStore.DeleteRecord("mykey");
            (await _client.ScanAsync(new ScanRequest
            {
                TableName = tableNameCustom
            })).Count.Should().Be(0);

        }
        finally
        {
            try
            {
                await _client.DeleteTableAsync(new DeleteTableRequest
                {
                    TableName = tableNameCustom
                });
            }
            catch (Exception)
            {
                // OK
            }
        }
    }

    [Fact]
    public async Task GetRecord_WhenIdempotencyDisabled_ShouldNotCreateClients() 
    {
        try
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "true");
            
            var store = new DynamoDBPersistenceStoreBuilder().WithTableName(_tableName).Build();
            
            // Act
            Func<Task> act = () => store.GetRecord("fake");
            
            // Assert
            await act.Should().ThrowAsync<NullReferenceException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "false");
        }
    }
    private static Dictionary<string, AttributeValue> CreateKey(string keyValue)
    {
        var key = new Dictionary<string, AttributeValue>
        {
            {"id", new AttributeValue(keyValue)}
        };
        return key;
    }
}