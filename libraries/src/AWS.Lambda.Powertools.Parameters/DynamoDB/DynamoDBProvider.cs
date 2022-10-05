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

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.DynamoDB;

public class DynamoDBProvider : ParameterProvider
{
    private IAmazonDynamoDB? _client;
    private string? _defaultTableName;
    private string? _defaultPrimaryKeyAttribute;
    private string? _defaultSortKeyAttribute;
    private string? _defaultValueAttribute;
    
    private IAmazonDynamoDB Client => _client ??= new AmazonDynamoDBClient();
    
    public DynamoDBProvider UseClient(IAmazonDynamoDB client)
    {
        _client = client;
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(region);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(config);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(AWSCredentials credentials)
    {
        _client = new AmazonDynamoDBClient(credentials);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(credentials, region);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(AWSCredentials credentials, AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(credentials, config);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, region);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, config);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region);
        return this;
    }
    
    public DynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config);
        return this;
    }

    public DynamoDBProvider UseTable(string tableName)
    {
        _defaultTableName = tableName;
        return this;
    }
    
    public DynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string valueAttribute)
    {
        _defaultTableName = tableName;
        _defaultPrimaryKeyAttribute = primaryKeyAttribute;
        _defaultValueAttribute = valueAttribute;
        return this;
    }
    
    public DynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string sortKeyAttribute, string valueAttribute)
    {
        _defaultTableName = tableName;
        _defaultPrimaryKeyAttribute = primaryKeyAttribute;
        _defaultSortKeyAttribute = sortKeyAttribute;
        _defaultValueAttribute = valueAttribute;
        return this;
    }

    private (string TableName, string PrimaryKeyAttribute, string SortKeyAttribute, string ValueAttribute) GetTableInfo()
    {
        var tableName = _defaultTableName ?? "";
        var primaryKeyAttribute = !string.IsNullOrWhiteSpace(_defaultPrimaryKeyAttribute) ? _defaultPrimaryKeyAttribute : "id";
        var sortKeyAttribute = !string.IsNullOrWhiteSpace(_defaultSortKeyAttribute) ? _defaultSortKeyAttribute : "sk";
        var valueAttribute = !string.IsNullOrWhiteSpace(_defaultValueAttribute) ? _defaultValueAttribute : "value";
        return (tableName, primaryKeyAttribute, sortKeyAttribute, valueAttribute);
    }

    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        var tableInfo = GetTableInfo();
        var response = await Client.GetItemAsync(
            new GetItemRequest
            {
                TableName = tableInfo.TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { tableInfo.PrimaryKeyAttribute, new AttributeValue { S = key } }
                },
            }).ConfigureAwait(false);

        return response?.Item is not null &&
               response.Item.TryGetValue(tableInfo.ValueAttribute, out var attributeValue)
            ? attributeValue.S
            : null;
    }

    protected override async Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config)
    {
        var tableInfo = GetTableInfo();

        var retValues = new Dictionary<string, string>();
        var response = await Client.QueryAsync(
            new QueryRequest
            {
                TableName = tableInfo.TableName,
                KeyConditionExpression = $"{tableInfo.PrimaryKeyAttribute} = :v_id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_id", new AttributeValue { S = path } }
                }
            }).ConfigureAwait(false);

        if (response?.Items is null)
            return retValues;

        foreach (var item in response.Items)
        {
            if (item.TryGetValue(tableInfo.ValueAttribute, out var attributeValue) && !string.IsNullOrWhiteSpace(attributeValue.S))
                retValues.TryAdd(item[tableInfo.SortKeyAttribute].S, attributeValue.S);
            else
                retValues.TryAdd(item[tableInfo.SortKeyAttribute].S, string.Empty);
        }

        return retValues;
    }
}