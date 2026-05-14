// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters;

// --8<-- [start:app_config_provider]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.AppConfig;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get AppConfig Provider instance
        IAppConfigProvider appConfigProvider = ParametersManager.AppConfigProvider
            .DefaultApplication("MyApplicationId")
            .DefaultEnvironment("MyEnvironmentId")
            .DefaultConfigProfile("MyConfigProfileId");

        // Retrieve a single configuration, latest version
        IDictionary<string, string?> value = await appConfigProvider
            .GetAsync()
            .ConfigureAwait(false);
    }
}
// --8<-- [end:app_config_provider]

// --8<-- [start:app_config_provider_explicit_region]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.AppConfig;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get AppConfig Provider instance
        IAppConfigProvider appConfigProvider = ParametersManager.AppConfigProvider
            .ConfigureClient(RegionEndpoint.EUCentral1)
            .DefaultApplication("MyApplicationId")
            .DefaultEnvironment("MyEnvironmentId")
            .DefaultConfigProfile("MyConfigProfileId");

        // Retrieve a single configuration, latest version
        IDictionary<string, string?> value = await appConfigProvider
            .GetAsync()
            .ConfigureAwait(false);
    }
}
// --8<-- [end:app_config_provider_explicit_region]

// --8<-- [start:app_config_feature_flags]
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.AppConfig;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Get AppConfig Provider instance
        IAppConfigProvider appConfigProvider = ParametersManager.AppConfigProvider
            .DefaultApplication("MyApplicationId")
            .DefaultEnvironment("MyEnvironmentId")
            .DefaultConfigProfile("MyConfigProfileId");

        // Check if feature flag is enabled
        var isFeatureFlagEnabled = await appConfigProvider
            .IsFeatureFlagEnabledAsync("MyFeatureFlag")
            .ConfigureAwait(false);

        if (isFeatureFlagEnabled)
        {
            // Retrieve an attribute value of the feature flag
            var strAttValue = await appConfigProvider
                .GetFeatureFlagAttributeValueAsync<string>("MyFeatureFlag", "StringAttribute")
                .ConfigureAwait(false);

            // Retrieve another attribute value of the feature flag
            var numberAttValue = await appConfigProvider
                .GetFeatureFlagAttributeValueAsync<int>("MyFeatureFlag", "NumberAttribute")
                .ConfigureAwait(false);
        }
    }
}
// --8<-- [end:app_config_feature_flags]
