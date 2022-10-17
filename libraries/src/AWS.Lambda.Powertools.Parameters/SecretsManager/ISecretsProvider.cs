using Amazon.SecretsManager;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SecretsManager;

public interface ISecretsProvider : IParameterProvider,
    IParameterProviderConfigurableClient<ISecretsProvider, IAmazonSecretsManager, AmazonSecretsManagerConfig>
{
   
}