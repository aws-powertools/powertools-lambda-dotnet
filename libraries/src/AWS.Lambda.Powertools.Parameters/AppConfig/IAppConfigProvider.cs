using Amazon.AppConfigData;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

/// <summary>
/// Represents a type used to retrieve parameter values from a AWS AppConfig.
/// </summary>
public interface IAppConfigProvider : IParameterProvider<AppConfigProviderConfigurationBuilder>,
    IParameterProviderConfigurableClient<IAppConfigProvider, IAmazonAppConfigData, AmazonAppConfigDataConfig>
{
    /// <summary>
    /// Sets the default application ID or name.
    /// </summary>
    /// <param name="applicationId">The application ID or name.</param>
    /// <returns>The AppConfigProvider instance.</returns>
    IAppConfigProvider DefaultApplication(string applicationId);

    /// <summary>
    /// Sets the default environment ID or name.
    /// </summary>
    /// <param name="environmentId">The environment ID or name.</param>
    /// <returns>The AppConfigProvider instance.</returns>
    IAppConfigProvider DefaultEnvironment(string environmentId);

    /// <summary>
    /// Sets the default configuration profile ID or name.
    /// </summary>
    /// <param name="configProfileId">The configuration profile ID or name.</param>
    /// <returns>The AppConfigProvider instance.</returns>
    IAppConfigProvider DefaultConfigProfile(string configProfileId);

    /// <summary>
    /// Sets the application ID or name.
    /// </summary>
    /// <param name="applicationId">The application ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    AppConfigProviderConfigurationBuilder WithApplication(string applicationId);

    /// <summary>
    /// Sets the environment ID or name.
    /// </summary>
    /// <param name="environmentId">The environment ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId);

    /// <summary>
    /// Sets the configuration profile ID or name.
    /// </summary>
    /// <param name="configProfileId">The configuration profile ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    AppConfigProviderConfigurationBuilder WithConfigProfile(string configProfileId);

    /// <summary>
    /// Get last AppConfig value.
    /// </summary>
    /// <returns>Application Configuration.</returns>
    IDictionary<string, string?> Get();

    /// <summary>
    /// Get last AppConfig value.
    /// </summary>
    /// <returns>The AppConfig value.</returns>
    Task<IDictionary<string, string?>> GetAsync();

    /// <summary>
    /// Get last AppConfig value and transform it to JSON value.
    /// </summary>
    /// <typeparam name="T">JSON value type.</typeparam>
    /// <returns>The AppConfig JSON value.</returns>
    T? Get<T>() where T : class;

    /// <summary>
    /// Get last AppConfig value and transform it to JSON value.
    /// </summary>
    /// <typeparam name="T">JSON value type.</typeparam>
    /// <returns>The AppConfig JSON value.</returns>
    Task<T?> GetAsync<T>() where T : class;
}

