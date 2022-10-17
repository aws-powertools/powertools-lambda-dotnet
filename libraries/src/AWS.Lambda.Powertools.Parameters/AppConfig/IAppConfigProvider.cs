using Amazon.AppConfigData;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

public interface IAppConfigProvider : IParameterProvider<AppConfigProviderConfigurationBuilder>,
    IParameterProviderConfigurableClient<IAppConfigProvider, IAmazonAppConfigData, AmazonAppConfigDataConfig>
{
    IAppConfigProvider DefaultApplication(string applicationId);

    IAppConfigProvider DefaultEnvironment(string environmentId);

    IAppConfigProvider DefaultConfigProfile(string configProfileId);

    AppConfigProviderConfigurationBuilder WithApplication(string applicationId);

    AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId);

    AppConfigProviderConfigurationBuilder WithConfigProfile(string configProfileId);

    IDictionary<string, string?> Get();

    Task<IDictionary<string, string?>> GetAsync();
}

