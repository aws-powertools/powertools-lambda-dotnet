using Amazon.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public interface ISsmProvider : IParameterProvider<SsmProviderConfigurationBuilder>,
    IParameterProviderConfigurableClient<ISsmProvider, IAmazonSimpleSystemsManagement, AmazonSimpleSystemsManagementConfig>
{
    SsmProviderConfigurationBuilder WithDecryption();
    
    SsmProviderConfigurationBuilder Recursive();
}

