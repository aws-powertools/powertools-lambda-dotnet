// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters;

// --8<-- [start:dynamodb_provider_single]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.DynamoDB;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get DynamoDB Provider instance
        IDynamoDBProvider dynamoDbProvider = ParametersManager.DynamoDBProvider
            .UseTable("my-table");

        // Retrieve a single parameter
        string? value = await dynamoDbProvider
            .GetAsync("my-param")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:dynamodb_provider_single]

// --8<-- [start:dynamodb_provider_multiple]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.DynamoDB;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get DynamoDB Provider instance
        IDynamoDBProvider dynamoDbProvider = ParametersManager.DynamoDBProvider
            .UseTable("my-table");

        // Retrieve a single parameter
        IDictionary<string, string?> value = await dynamoDbProvider
            .GetMultipleAsync("my-hash-key")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:dynamodb_provider_multiple]

// --8<-- [start:dynamodb_provider_customizing]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.DynamoDB;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get DynamoDB Provider instance
        IDynamoDBProvider dynamoDbProvider = ParametersManager.DynamoDBProvider
            .UseTable
            (
                tableName: "TableName",    // DynamoDB table name, Required.
                primaryKeyAttribute: "id", // Partition Key attribute name, optional, default is 'id'
                sortKeyAttribute: "sk",    // Sort Key attribute name, optional, default is 'sk'
                valueAttribute: "value"    // Value attribute name, optional, default is 'value'
            );
    }
}
// --8<-- [end:dynamodb_provider_customizing]
