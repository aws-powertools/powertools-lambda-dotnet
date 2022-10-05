using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.DynamoDB;

public interface IDynamoDBProvider : IParameterProvider
{
    IDynamoDBProvider UseClient(IAmazonDynamoDB client);
    
    IDynamoDBProvider ConfigureClient(RegionEndpoint region);
    
    IDynamoDBProvider ConfigureClient(AmazonDynamoDBConfig config);
    
    IDynamoDBProvider ConfigureClient(AWSCredentials credentials);
    
    IDynamoDBProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region);
    
    IDynamoDBProvider ConfigureClient(AWSCredentials credentials, AmazonDynamoDBConfig config);
    
    IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey);
    
    IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region);

    IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonDynamoDBConfig config);

    IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken);

    IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region);
    
    IDynamoDBProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonDynamoDBConfig config);

    IDynamoDBProvider UseTable(string tableName);

    IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string valueAttribute);

    IDynamoDBProvider UseTable(string tableName, string primaryKeyAttribute, string sortKeyAttribute,
        string valueAttribute);
}