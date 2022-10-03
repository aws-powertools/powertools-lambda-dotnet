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

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Internal.DynamoDB;

namespace AWS.Lambda.Powertools.Parameters.DynamoDB;

public class DynamoDBProvider : ParameterProvider<DynamoDBProviderConfigurationBuilder>
{
    private IAmazonDynamoDB? _client;
    private IAmazonDynamoDB Client => _client ??= new AmazonDynamoDBClient();

    private string? _defaultTableName;

    public DynamoDBProvider UseClient(IAmazonDynamoDB client)
    {
        _client = client;
        return this;
    }
    
    public DynamoDBProvider DefaultTableName(string tableName)
    {
        _defaultTableName = tableName;
        return this;
    }

    public DynamoDBProviderConfigurationBuilder WithTableName(string tableName)
    {
        return NewConfigurationBuilder().WithTableName(tableName);
    }

    protected override DynamoDBProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new DynamoDBProviderConfigurationBuilder(this);
    }

    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        var configuration = config as DynamoDBProviderConfiguration;
        var tableName = !string.IsNullOrWhiteSpace(configuration?.WithTableName)
            ? configuration.WithTableName
            : _defaultTableName;

        var response = await Client.GetItemAsync(
            new GetItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = key } }
                },
            }).ConfigureAwait(false);
        
        return response?.Item is not null &&
               response.Item.TryGetValue("value", out var attributeValue)
            ? attributeValue.S
            : null;
    }

    protected override async Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config)
    {
        var configuration = config as DynamoDBProviderConfiguration;
        var tableName = !string.IsNullOrWhiteSpace(configuration?.WithTableName)
            ? configuration.WithTableName
            : _defaultTableName;

        var retValues = new Dictionary<string, string>();
        var response = await Client.QueryAsync(
            new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "id = :v_id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_id", new AttributeValue { S = path } }
                }
            }).ConfigureAwait(false);

        if (response?.Items is null)
            return retValues;

        foreach (var item in response.Items)
        {
            if (item.TryGetValue("value", out var attributeValue) && !string.IsNullOrWhiteSpace(attributeValue.S))
                retValues.TryAdd(item["sk"].S, attributeValue.S);
            else
                retValues.TryAdd(item["sk"].S, string.Empty);
        }

        return retValues;
    }
}