using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;

// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).


namespace Docs.ParametersSnippets.SsmProvider.SsmProvider
{
// --8<-- [start:ssm_provider]
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
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);

        // Retrieve multiple parameters from a path prefix
        // This returns a Dictionary with the parameter name as key
        IDictionary<string, string?> values = await ssmProvider
            .GetMultipleAsync("/my/path/prefix")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:ssm_provider]
}

namespace Docs.ParametersSnippets.SsmProvider.SsmProviderExplicitRegion
{
// --8<-- [start:ssm_provider_explicit_region]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider
            .ConfigureClient(RegionEndpoint.EUCentral1);

        // Retrieve a single parameter
        string? value = await ssmProvider
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);

        // Retrieve multiple parameters from a path prefix
        // This returns a Dictionary with the parameter name as key
        IDictionary<string, string?> values = await ssmProvider
            .GetMultipleAsync("/my/path/prefix")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:ssm_provider_explicit_region]
}

namespace Docs.ParametersSnippets.SsmProvider.SsmProviderCustomClient
{
// --8<-- [start:ssm_provider_custom_client]
using Amazon.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Create a new instance of client
        IAmazonSimpleSystemsManagement client = new AmazonSimpleSystemsManagementClient();

        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider
            .UseClient(client);

        // Retrieve a single parameter
        string? value = await ssmProvider
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);

        // Retrieve multiple parameters from a path prefix
        // This returns a Dictionary with the parameter name as key
        IDictionary<string, string?> values = await ssmProvider
            .GetMultipleAsync("/my/path/prefix")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:ssm_provider_custom_client]
}

namespace Docs.ParametersSnippets.SsmProvider.SsmProviderAdditionalArgs
{
// --8<-- [start:ssm_provider_additional_args]
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
            .WithDecryption()
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);

        // Retrieve multiple parameters from a path prefix
        // This returns a Dictionary with the parameter name as key
        IDictionary<string, string?> values = await ssmProvider
            .Recursive()
            .GetMultipleAsync("/my/path/prefix")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:ssm_provider_additional_args]
}
