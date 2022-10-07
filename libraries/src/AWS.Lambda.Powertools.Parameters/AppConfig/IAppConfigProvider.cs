using Amazon;
using Amazon.AppConfigData;
using Amazon.Runtime;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

public interface IAppConfigProvider : IParameterProvider<AppConfigProviderConfigurationBuilder>
{
    IAppConfigProvider UseClient(IAmazonAppConfigData client);

    IAppConfigProvider ConfigureClient(RegionEndpoint region);

    IAppConfigProvider ConfigureClient(AmazonAppConfigDataConfig config);

    IAppConfigProvider ConfigureClient(AWSCredentials credentials);

    IAppConfigProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region);

    IAppConfigProvider ConfigureClient(AWSCredentials credentials, AmazonAppConfigDataConfig config);

    IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey);

    IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region);

    IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonAppConfigDataConfig config);

    IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken);

    IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region);

    IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonAppConfigDataConfig config);
    
    IAppConfigProvider ConfigureSource(string applicationId, string environmentId, string configProfileId);

    AppConfigProviderConfigurationBuilder WithApplicationIdentifier(string applicationId);

    AppConfigProviderConfigurationBuilder WithEnvironmentIdentifier(string environmentId);

    AppConfigProviderConfigurationBuilder WithConfigurationProfileIdentifier(string configProfileId);
    
    IDictionary<string, string> Get();

    Task<IDictionary<string, string>> GetAsync();
}

