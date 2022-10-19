using Amazon.SecretsManager;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SecretsManager;

/// <summary>
/// Represents a type used to retrieve parameter values from SAWS Secrets Manager.
/// </summary>
public interface ISecretsProvider : IParameterProvider,
    IParameterProviderConfigurableClient<ISecretsProvider, IAmazonSecretsManager, AmazonSecretsManagerConfig>
{
   
}