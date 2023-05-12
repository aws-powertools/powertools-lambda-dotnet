using Amazon.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

/// <summary>
/// Represents a type used to retrieve parameter values from AWS Systems Manager Parameter Store.
/// </summary>
public interface ISsmProvider : IParameterProvider<SsmProviderConfigurationBuilder>,
    IParameterProviderConfigurableClient<ISsmProvider, IAmazonSimpleSystemsManagement, AmazonSimpleSystemsManagementConfig>
{
    /// <summary>
    /// Automatically decrypt the parameter.
    /// </summary>
    /// <returns>The provider configuration builder.</returns>
    SsmProviderConfigurationBuilder WithDecryption();
    
    /// <summary>
    /// Fetches all parameter values recursively based on a path prefix.
    /// For GetMultiple() only.
    /// </summary>
    /// <returns>The provider configuration builder.</returns>
    SsmProviderConfigurationBuilder Recursive();
}

