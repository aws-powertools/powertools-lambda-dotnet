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
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Idempotency.Exceptions;

namespace AWS.Lambda.Powertools.Idempotency.Persistence;

/// <summary>
/// DynamoDB version of the <see cref="BasePersistenceStore"/>. Will store idempotency data in DynamoDB.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DynamoDBPersistenceStore : BasePersistenceStore
{
    private readonly string _tableName;
    private readonly string _keyAttr;
    private readonly string _staticPkValue;
    private readonly string _sortKeyAttr;
    private readonly string _expiryAttr;
    private readonly string _statusAttr;
    private readonly string _dataAttr;
    private readonly string _validationAttr;
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    internal DynamoDBPersistenceStore(string tableName,
        string keyAttr,
        string staticPkValue,
        string sortKeyAttr,
        string expiryAttr,
        string statusAttr,
        string dataAttr,
        string validationAttr,
        AmazonDynamoDBClient client)
    {
        _tableName = tableName;
        _keyAttr = keyAttr;
        _staticPkValue = staticPkValue;
        _sortKeyAttr = sortKeyAttr;
        _expiryAttr = expiryAttr;
        _statusAttr = statusAttr;
        _dataAttr = dataAttr;
        _validationAttr = validationAttr;

        if (client != null) 
        {
            _dynamoDbClient = client;
        } 
        else
        {
            if (PowertoolsConfigurations.Instance.IdempotencyDisabled) 
            {                
                // we do not want to create a DynamoDbClient if idempotency is disabled
                // null is ok as idempotency won't be called
                _dynamoDbClient = null;
                
            } else {
                var clientConfig = new AmazonDynamoDBConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable(Constants.AwsRegionEnv))
                };
                _dynamoDbClient = new AmazonDynamoDBClient(clientConfig);
            }
        }
    }


    /// <inheritdoc />
    public override async Task<DataRecord> GetRecord(string idempotencyKey)
    {
        var getItemRequest = new GetItemRequest
        {
            TableName = _tableName,
            ConsistentRead = true,
            Key = GetKey(idempotencyKey)
        };
        var response = await _dynamoDbClient!.GetItemAsync(getItemRequest);

        if (!response.IsItemSet)
        {
            throw new IdempotencyItemNotFoundException(idempotencyKey);
        }

        return ItemToRecord(response.Item);
    }


    /// <inheritdoc />
    public override async Task PutRecord(DataRecord record, DateTimeOffset now)
    {
        Dictionary<string, AttributeValue> item = new(GetKey(record.IdempotencyKey))
        {
            {
                _expiryAttr, new AttributeValue
                {
                    N = record.ExpiryTimestamp.ToString()
                }
            },
            { _statusAttr, new AttributeValue(record.Status.ToString()) }
        };

        if (PayloadValidationEnabled)
        {
            item.Add(_validationAttr, new AttributeValue(record.PayloadHash));
        }

        try
        {
            var expressionAttributeNames = new Dictionary<string, string>
            {
                {"#id", _keyAttr},
                {"#expiry", _expiryAttr}
            };

            var request = new PutItemRequest
            {
                TableName = _tableName,
                Item = item,
                ConditionExpression = "attribute_not_exists(#id) OR #expiry < :now",
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":now", new AttributeValue {N = now.ToUnixTimeSeconds().ToString()}}
                }
            };
            await _dynamoDbClient!.PutItemAsync(request);
        }
        catch (ConditionalCheckFailedException e)
        {
            throw new IdempotencyItemAlreadyExistsException(
                "Failed to put record for already existing idempotency key: " + record.IdempotencyKey, e);
        }
    }


    /// <inheritdoc />
    public override async Task UpdateRecord(DataRecord record) 
    {
        var updateExpression = "SET #response_data = :response_data, #expiry = :expiry, #status = :status";

        var expressionAttributeNames = new Dictionary<string, string>
        {
            {"#response_data", _dataAttr},
            {"#expiry", _expiryAttr},
            {"#status", _statusAttr}
        };

        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            {":response_data", new AttributeValue(record.ResponseData)},
            {":expiry", new AttributeValue {N=record.ExpiryTimestamp.ToString()}},
            {":status", new AttributeValue(record.Status.ToString())}
        };

        if (PayloadValidationEnabled)
        {
            updateExpression += ", #validation_key = :validation_key";
            expressionAttributeNames.Add("#validation_key", _validationAttr);
            expressionAttributeValues.Add(":validation_key", new AttributeValue(record.PayloadHash));
        }

        var request = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = GetKey(record.IdempotencyKey),
            UpdateExpression = updateExpression,
            ExpressionAttributeNames = expressionAttributeNames,
            ExpressionAttributeValues = expressionAttributeValues
        };
        await _dynamoDbClient!.UpdateItemAsync(request);
    }

    /// <inheritdoc />
    public override async Task DeleteRecord(string idempotencyKey) 
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = GetKey(idempotencyKey)
        };
        await _dynamoDbClient!.DeleteItemAsync(request);
    }

    /// <summary>
    /// Translate raw item records from DynamoDB to DataRecord 
    /// </summary>
    /// <param name="item">item Item from dynamodb response</param>
    /// <returns>DataRecord instance</returns>
    private DataRecord ItemToRecord(Dictionary<string, AttributeValue> item)
    {
        // data and validation payload may be null
        var hasDataAttribute = item.TryGetValue(_dataAttr, out var data);
        var hasValidationAttribute = item.TryGetValue(_validationAttr, out var validation);

        return new DataRecord(item[_sortKeyAttr ?? _keyAttr].S,
            Enum.Parse<DataRecord.DataRecordStatus>(item[_statusAttr].S),
            long.Parse(item[_expiryAttr].N),
            hasDataAttribute ? data?.S : null,
            hasValidationAttribute ? validation?.S : null);
    }

    /// <summary>
    /// Get the key to use for requests (depending on if we have a sort key or not)
    /// </summary>
    /// <param name="idempotencyKey">idempotencyKey</param>
    /// <returns></returns>
    private Dictionary<string, AttributeValue> GetKey(string idempotencyKey)
    {
        Dictionary<string, AttributeValue> key = new();
        if (_sortKeyAttr != null)
        {
            key[_keyAttr] = new AttributeValue(_staticPkValue);
            key[_sortKeyAttr] = new AttributeValue(idempotencyKey);
        }
        else
        {
            key[_keyAttr] = new AttributeValue(idempotencyKey);
        }

        return key;
    }

}

/// <summary>
/// Use this builder to get an instance of <see cref="DynamoDBPersistenceStore"/>.<br/>
/// With this builder you can configure the characteristics of the DynamoDB Table
/// (name, key, sort key, and other field names).<br/>
/// You can also set a custom AmazonDynamoDBClient for further tuning.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DynamoDBPersistenceStoreBuilder
{
    private static readonly string FuncEnv = Environment.GetEnvironmentVariable(Constants.LambdaFunctionNameEnv);

    private string _tableName = null!;
    private string _keyAttr = "id";
    private string _staticPkValue = $"idempotency#{FuncEnv}";
    private string _sortKeyAttr;
    private string _expiryAttr = "expiration";
    private string _statusAttr = "status";
    private string _dataAttr = "data";
    private string _validationAttr = "validation";
    private AmazonDynamoDBClient _dynamoDbClient;

    /// <summary>
    /// Initialize and return a new instance of {@link DynamoDBPersistenceStore}.
    /// Example:
    ///    DynamoDBPersistenceStore.builder().withTableName("idempotency_store").build();
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public DynamoDBPersistenceStore Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
        {
            throw new ArgumentNullException($"Table name is not specified");
        }

        return new DynamoDBPersistenceStore(_tableName,
            _keyAttr,
            _staticPkValue,
            _sortKeyAttr,
            _expiryAttr,
            _statusAttr,
            _dataAttr,
            _validationAttr,
            _dynamoDbClient);
    }

    /// <summary>
    /// Name of the table to use for storing execution records (mandatory)
    /// </summary>
    /// <param name="tableName">tableName Name of the DynamoDB table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithTableName(string tableName)
    {
        _tableName = tableName;
        return this;
    }

    /// <summary>
    /// DynamoDB attribute name for partition key (optional), by default "id"
    /// </summary>
    /// <param name="keyAttr">keyAttr name of the key attribute in the table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithKeyAttr(string keyAttr)
    {
        _keyAttr = keyAttr;
        return this;
    }

    /// <summary>
    /// DynamoDB attribute value for partition key (optional), by default "idempotency#[function-name]".
    /// This will be used if the {@link #sortKeyAttr} is set.
    /// </summary>
    /// <param name="staticPkValue">staticPkValue name of the partition key attribute in the table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithStaticPkValue(string staticPkValue)
    {
        _staticPkValue = staticPkValue;
        return this;
    }

    /// <summary>
    /// DynamoDB attribute name for the sort key (optional)
    /// </summary>
    /// <param name="sortKeyAttr">sortKeyAttr name of the sort key attribute in the table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithSortKeyAttr(string sortKeyAttr)
    {
        _sortKeyAttr = sortKeyAttr;
        return this;
    }

    /// <summary>
    /// DynamoDB attribute name for expiry timestamp (optional), by default "expiration"
    /// </summary>
    /// <param name="expiryAttr">expiryAttr name of the expiry attribute in the table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithExpiryAttr(string expiryAttr)
    {
        _expiryAttr = expiryAttr;
        return this;
    }

    /// <summary>
    /// DynamoDB attribute name for status (optional), by default "status"
    /// </summary>
    /// <param name="statusAttr">statusAttr name of the status attribute in the table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithStatusAttr(string statusAttr)
    {
        _statusAttr = statusAttr;
        return this;
    }

    /// <summary>
    /// DynamoDB attribute name for response data (optional), by default "data"
    /// </summary>
    /// <param name="dataAttr">dataAttr name of the data attribute in the table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithDataAttr(string dataAttr)
    {
        _dataAttr = dataAttr;
        return this;
    }

    /// <summary>
    /// DynamoDB attribute name for validation (optional), by default "validation"
    /// </summary>
    /// <param name="validationAttr">validationAttr name of the validation attribute in the table</param>
    /// <returns>the builder instance (to chain operations)</returns>
    public DynamoDBPersistenceStoreBuilder WithValidationAttr(string validationAttr)
    {
        _validationAttr = validationAttr;
        return this;
    }

    /// <summary>
    /// Custom DynamoDbClient used to query DynamoDB (optional).
    /// The default one uses UrlConnectionHttpClient as a http client and
    /// </summary>
    /// <param name="dynamoDbClient">dynamoDbClient the DynamoDbClient instance to use</param>
    /// <returns>the builder instance (to chain operations)</returns>
    // ReSharper disable once InconsistentNaming
    public DynamoDBPersistenceStoreBuilder WithDynamoDBClient(AmazonDynamoDBClient dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
        return this;
    }
}