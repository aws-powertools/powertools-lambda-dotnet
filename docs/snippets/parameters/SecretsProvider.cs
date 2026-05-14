// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters;

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
    }
}
// --8<-- [end:secrets_provider]

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
    }
}
// --8<-- [end:secrets_provider_explicit_region]

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
    }
}
// --8<-- [end:secrets_provider_custom_client]
