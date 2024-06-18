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

    /// <summary>
    /// Check if the feature flag is enabled.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="defaultValue">The default value of the flag</param>
    /// <returns>The feature flag value, or defaultValue if the flag cannot be evaluated</returns>
    bool IsFeatureFlagEnabled(string key, bool defaultValue = false);
    
    /// <summary>
    /// Check if the feature flag is enabled.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="defaultValue">The default value of the flag</param>
    /// <returns>The feature flag value, or defaultValue if the flag cannot be evaluated</returns>
    Task<bool> IsFeatureFlagEnabledAsync(string key, bool defaultValue = false);

    /// <summary>
    /// Get feature flag's attribute value.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="attributeKey">The unique attribute key for the feature flag</param>
    /// <param name="defaultValue">The default value of the feature flag's attribute value</param>
    /// <typeparam name="T">The type of the value to obtain from feature flag's attribute.</typeparam>
    /// <returns>The feature flag's attribute value.</returns>
    T? GetFeatureFlagAttributeValue<T>(string key, string attributeKey, T? defaultValue = default);
    
    /// <summary>
    /// Get feature flag's attribute value.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="attributeKey">The unique attribute key for the feature flag</param>
    /// <param name="defaultValue">The default value of the feature flag's attribute value</param>
    /// <typeparam name="T">The type of the value to obtain from feature flag's attribute.</typeparam>
    /// <returns>The feature flag's attribute value.</returns>
    Task<T?> GetFeatureFlagAttributeValueAsync<T>(string key, string attributeKey, T? defaultValue = default);
}

