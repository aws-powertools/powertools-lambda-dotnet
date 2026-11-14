using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;

// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).


namespace Docs.ParametersSnippets.DynamoDbProvider.DynamodbProviderSingle
{
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
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:dynamodb_provider_single]
}

namespace Docs.ParametersSnippets.DynamoDbProvider.DynamodbProviderMultiple
{
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
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:dynamodb_provider_multiple]
}

namespace Docs.ParametersSnippets.DynamoDbProvider.DynamodbProviderCustomizing
{
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
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:dynamodb_provider_customizing]
}
