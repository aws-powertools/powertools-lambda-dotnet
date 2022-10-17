using Amazon.DynamoDBv2;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.DynamoDB;

public interface IDynamoDBProvider : IParameterProvider,
    IParameterProviderConfigurableClient<IDynamoDBProvider, IAmazonDynamoDB, AmazonDynamoDBConfig>
{
    IDynamoDBProvider UseTable(string tableName);

    IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string valueAttribute);

    IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string sortKeyAttribute,
        string valueAttribute);
}