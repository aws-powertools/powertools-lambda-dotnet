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

/// <summary>
/// Provider to retrieve parameter values from Amazon DynamoDB table.
/// </summary>
public class DynamoDBProvider : ParameterProvider, IDynamoDBProvider
{
    /// <summary>
    /// The default table name.
    /// </summary>
    private string? _defaultTableName;
    
    /// <summary>
    /// The primary key attribute name.
    /// </summary>
    private string? _defaultPrimaryKeyAttribute;
    
    /// <summary>
    /// The sort key attribute name.
    /// </summary>
    private string? _defaultSortKeyAttribute;
    
    /// <summary>
    /// The value attribute name.
    /// </summary>
    private string? _defaultValueAttribute;
    
    #region IParameterProviderConfigurableClient implementation
    
    /// <summary>
    /// The client instance.
    /// </summary>
    private IAmazonDynamoDB? _client;
    
    /// <summary>
    /// Gets the client instance.
    /// </summary>
    private IAmazonDynamoDB Client => _client ??= new AmazonDynamoDBClient();
    
    /// <summary>
    /// Use a custom client
    /// </summary>
    /// <param name="client">The custom client</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider UseClient(IAmazonDynamoDB client)
    {
        _client = client;
        return this;
    }
    
    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(region);
        return this;
    }
    
    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(config);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(AWSCredentials credentials)
    {
        _client = new AmazonDynamoDBClient(credentials);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(credentials, region);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS credentials and a client configuration object.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(AWSCredentials credentials, AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(credentials, config);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, region);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, config);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, RegionEndpoint region)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, AmazonDynamoDBConfig config)
    {
        _client = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config);
        return this;
    }

    #endregion
    
    /// <summary>
    /// Specify the DynamoDB table
    /// </summary>
    /// <param name="tableName">DynamoDB table name.</param>
    /// <returns>Provider instance.</returns>
    public IDynamoDBProvider UseTable(string tableName)
    {
        _defaultTableName = tableName;
        return this;
    }
    
    /// <summary>
    /// Specify the DynamoDB table
    /// </summary>
    /// <param name="tableName">DynamoDB table name.</param>
    /// <param name="primaryKeyAttribute">The primary key attribute name.</param>
    /// <param name="valueAttribute">The value attribute name.</param>
    /// <returns>Provider instance.</returns>
    public IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string valueAttribute)
    {
        _defaultTableName = tableName;
        _defaultPrimaryKeyAttribute = primaryKeyAttribute;
        _defaultValueAttribute = valueAttribute;
        return this;
    }
    
    /// <summary>
    /// Specify the DynamoDB table
    /// </summary>
    /// <param name="tableName">DynamoDB table name.</param>
    /// <param name="primaryKeyAttribute">The primary key attribute name.</param>
    /// <param name="sortKeyAttribute">The sort key attribute name.</param>
    /// <param name="valueAttribute">The value attribute name.</param>
    /// <returns>Provider instance.</returns>
    public IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string sortKeyAttribute, string valueAttribute)
    {
        _defaultTableName = tableName;
        _defaultPrimaryKeyAttribute = primaryKeyAttribute;
        _defaultSortKeyAttribute = sortKeyAttribute;
        _defaultValueAttribute = valueAttribute;
        return this;
    }

    /// <summary>
    /// Gets DynamoDB table information.
    /// </summary>
    /// <returns></returns>
    private (string TableName, string PrimaryKeyAttribute, string SortKeyAttribute, string ValueAttribute) GetTableInfo()
    {
        var tableName = _defaultTableName ?? "";
        var primaryKeyAttribute = !string.IsNullOrWhiteSpace(_defaultPrimaryKeyAttribute) ? _defaultPrimaryKeyAttribute : "id";
        var sortKeyAttribute = !string.IsNullOrWhiteSpace(_defaultSortKeyAttribute) ? _defaultSortKeyAttribute : "sk";
        var valueAttribute = !string.IsNullOrWhiteSpace(_defaultValueAttribute) ? _defaultValueAttribute : "value";
        return (tableName, primaryKeyAttribute, sortKeyAttribute, valueAttribute);
    }

    /// <summary>
    /// Get parameter value for the provided key. 
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>The parameter value.</returns>
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

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string key,
        ParameterProviderConfiguration? config)
    {
        var tableInfo = GetTableInfo();

        var retValues = new Dictionary<string, string?>();
        var response = await Client.QueryAsync(
            new QueryRequest
            {
                TableName = tableInfo.TableName,
                KeyConditionExpression = $"{tableInfo.PrimaryKeyAttribute} = :v_id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_id", new AttributeValue { S = key } }
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