using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SecretsManager;

public interface ISecretsProvider : IParameterProvider
{
    ISecretsProvider UseClient(IAmazonSecretsManager client);
    
    ISecretsProvider ConfigureClient(RegionEndpoint region);
    
    ISecretsProvider ConfigureClient(AmazonSecretsManagerConfig config);
    
    ISecretsProvider ConfigureClient(AWSCredentials credentials);
    
    ISecretsProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region);
    
    ISecretsProvider ConfigureClient(AWSCredentials credentials, AmazonSecretsManagerConfig config);
    
    ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey);
    
    ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region);

    ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonSecretsManagerConfig config);

    ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken);

    ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region);
    
    ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonSecretsManagerConfig config);
}