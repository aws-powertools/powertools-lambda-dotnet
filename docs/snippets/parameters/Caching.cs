// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters;

// --8<-- [start:default_max_age]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider
            .DefaultMaxAge(TimeSpan.FromSeconds(10));

        // Retrieve a single parameter
        string? value = await ssmProvider
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:default_max_age]

// --8<-- [start:max_age_per_parameter]
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
        string? value = await ssmProvider
            .WithMaxAge(TimeSpan.FromSeconds(10))
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:max_age_per_parameter]

// --8<-- [start:force_fetch]
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
        string? value = await ssmProvider
            .ForceFetch()
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);
    }
}
// --8<-- [end:force_fetch]
