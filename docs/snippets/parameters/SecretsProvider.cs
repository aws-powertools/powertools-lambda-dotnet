using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;

// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).


namespace Docs.ParametersSnippets.SecretsProvider.SecretsProvider
{
// --8<-- [start:secrets_provider]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SecretsManager;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get Secrets Provider instance
        ISecretsProvider secretsProvider = ParametersManager.SecretsProvider;

        // Retrieve a single secret
        string? value = await secretsProvider
            .GetAsync("/my/secret")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:secrets_provider]
}

namespace Docs.ParametersSnippets.SecretsProvider.SecretsProviderExplicitRegion
{
// --8<-- [start:secrets_provider_explicit_region]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SecretsManager;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get Secrets Provider instance
        ISecretsProvider secretsProvider = ParametersManager.SecretsProvider
            .ConfigureClient(RegionEndpoint.EUCentral1);

        // Retrieve a single secret
        string? value = await secretsProvider
            .GetAsync("/my/secret")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:secrets_provider_explicit_region]
}

namespace Docs.ParametersSnippets.SecretsProvider.SecretsProviderCustomClient
{
// --8<-- [start:secrets_provider_custom_client]
using Amazon.SecretsManager;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SecretsManager;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
         // Create a new instance of client
        IAmazonSecretsManager client = new AmazonSecretsManagerClient();

        // Get Secrets Provider instance
        ISecretsProvider secretsProvider = ParametersManager.SecretsProvider
            .UseClient(client);

        // Retrieve a single secret
        string? value = await secretsProvider
            .GetAsync("/my/secret")
            .ConfigureAwait(false);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:secrets_provider_custom_client]
}
