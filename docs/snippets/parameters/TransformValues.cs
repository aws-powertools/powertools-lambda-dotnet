using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Docs.Snippets.Parameters.CustomTransformer;

// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).


namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters.TransformValues.JsonTransformation
{
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
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:json_transformation]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters.TransformValues.Base64Transformation
{
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
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:base64_transformation]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters.TransformValues.RaiseTransformationError
{
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
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:raise_transformation_error]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters.TransformValues.AutoTransform
{
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
            .GetMultipleAsync<object>("/param")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:auto_transform]
}
