using Amazon;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public interface ISsmProvider : IParameterProvider<SsmProviderConfigurationBuilder>
{
    ISsmProvider UseClient(IAmazonSimpleSystemsManagement client);
    
    ISsmProvider ConfigureClient(RegionEndpoint region);
    
    ISsmProvider ConfigureClient(AmazonSimpleSystemsManagementConfig config);
    
    ISsmProvider ConfigureClient(AWSCredentials credentials);
    
    ISsmProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region);
    
    ISsmProvider ConfigureClient(AWSCredentials credentials, AmazonSimpleSystemsManagementConfig config);
    
    ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey);
    
    ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region);

    ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonSimpleSystemsManagementConfig config);

    ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken);

    ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region);

    ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonSimpleSystemsManagementConfig config);

    SsmProviderConfigurationBuilder WithDecryption();
    
    SsmProviderConfigurationBuilder Recursive();
}

