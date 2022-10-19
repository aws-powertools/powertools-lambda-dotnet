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
    /// Set the default application.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <returns>The AppConfigProvider instance.</returns>
    IAppConfigProvider DefaultApplication(string applicationId);

    /// <summary>
    /// Set the default environment.
    /// </summary>
    /// <param name="environmentId"></param>
    /// <returns>The AppConfigProvider instance.</returns>
    IAppConfigProvider DefaultEnvironment(string environmentId);

    /// <summary>
    /// Set the default configuration profile.
    /// </summary>
    /// <param name="configProfileId"></param>
    /// <returns>The AppConfigProvider instance.</returns>
    IAppConfigProvider DefaultConfigProfile(string configProfileId);

    /// <summary>
    /// Set the application for the current query.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    AppConfigProviderConfigurationBuilder WithApplication(string applicationId);

    /// <summary>
    /// Set the environment for the current query.
    /// </summary>
    /// <param name="environmentId"></param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId);

    /// <summary>
    /// Set the configuration profile for the current query.
    /// </summary>
    /// <param name="configProfileId"></param>
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
    /// <returns>Application Configuration.</returns>
    Task<IDictionary<string, string?>> GetAsync();

    /// <summary>
    /// Get last AppConfig value and transform it to JSON value.
    /// </summary>
    /// <typeparam name="T">JSON value type.</typeparam>
    /// <returns>JSON value.</returns>
    T? Get<T>() where T : class;

    /// <summary>
    /// Get last AppConfig value and transform it to JSON value.
    /// </summary>
    /// <typeparam name="T">JSON value type.</typeparam>
    /// <returns>JSON value.</returns>
    Task<T?> GetAsync<T>() where T : class;
}

