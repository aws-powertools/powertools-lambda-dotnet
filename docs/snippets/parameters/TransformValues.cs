// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters;

// --8<-- [start:json_transformation]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider;

        // Retrieve a single parameter
        var value = await ssmProvider
            .WithTransformation(Transformation.Json)
            .GetAsync<MyObj>("/my/parameter/json")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:json_transformation]

// --8<-- [start:base64_transformation]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider;

        // Retrieve a single parameter
        var value = await ssmProvider
            .WithTransformation(Transformation.Base64)
            .GetAsync("/my/parameter/b64")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:base64_transformation]

// --8<-- [start:raise_transformation_error]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider
            .RaiseTransformationError();

        // Retrieve a single parameter
        var value = await ssmProvider
            .WithTransformation(Transformation.Json)
            .GetAsync<MyObj>("/my/parameter/json")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:raise_transformation_error]

// --8<-- [start:auto_transform]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider;

        // Retrieve multiple parameters from a path prefix
        // This returns a Dictionary with the parameter name as key
        IDictionary<string, object?> values = await ssmProvider
            .WithTransformation(Transformation.Auto)
            .GetMultipleAsync("/param")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:auto_transform]
