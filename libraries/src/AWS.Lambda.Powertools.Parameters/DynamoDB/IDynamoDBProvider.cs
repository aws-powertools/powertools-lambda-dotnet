using Amazon.DynamoDBv2;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.DynamoDB;

/// <summary>
/// Represents a type used to retrieve parameter values from Amazon DynamoDB table.
/// </summary>
public interface IDynamoDBProvider : IParameterProvider,
    IParameterProviderConfigurableClient<IDynamoDBProvider, IAmazonDynamoDB, AmazonDynamoDBConfig>
{
    /// <summary>
    /// Specify the DynamoDB table
    /// </summary>
    /// <param name="tableName">DynamoDB table name.</param>
    /// <returns>Provider instance.</returns>
    IDynamoDBProvider UseTable(string tableName);

    /// <summary>
    /// Specify the DynamoDB table
    /// </summary>
    /// <param name="tableName">DynamoDB table name.</param>
    /// <param name="primaryKeyAttribute">The primary key attribute name.</param>
    /// <param name="valueAttribute">The value attribute name.</param>
    /// <returns>Provider instance.</returns>
    IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string valueAttribute);

    /// <summary>
    /// Specify the DynamoDB table
    /// </summary>
    /// <param name="tableName">DynamoDB table name.</param>
    /// <param name="primaryKeyAttribute">The primary key attribute name.</param>
    /// <param name="sortKeyAttribute">The sort key attribute name.</param>
    /// <param name="valueAttribute">The value attribute name.</param>
    /// <returns>Provider instance.</returns>
    IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string sortKeyAttribute,
        string valueAttribute);
}