/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Amazon;
using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using Amazon.Runtime;
using AWS.Lambda.Powertools.Parameters.Internal.AppConfig;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

/// <summary>
/// The AppConfigProvider to retrieve parameter values from a AWS AppConfig.
/// </summary>
public class AppConfigProvider : ParameterProvider<AppConfigProviderConfigurationBuilder>, IAppConfigProvider
{
    /// <summary>
    /// The default application Id.
    /// </summary>
    private string _defaultApplicationId = string.Empty;

    /// <summary>
    /// The default environment Id.
    /// </summary>
    private string _defaultEnvironmentId = string.Empty;

    /// <summary>
    /// The default configuration profile Id.
    /// </summary>
    private string _defaultConfigProfileId = string.Empty;

    /// <summary>
    /// Instance of datetime wrapper.
    /// </summary>
    private readonly IDateTimeWrapper _dateTimeWrapper;

    /// <summary>
    /// Thread safe dictionary to store results.  
    /// </summary>
    private readonly ConcurrentDictionary<string, AppConfigResult> _results = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The client instance.
    /// </summary>
    private IAmazonAppConfigData? _client;

    /// <summary>
    /// Gets the client instance.
    /// </summary>
    private IAmazonAppConfigData Client => _client ??= new AmazonAppConfigDataClient();

    /// <summary>
    /// AppConfigProvider constructor.
    /// </summary>
    public AppConfigProvider()
    {
        _dateTimeWrapper = DateTimeWrapper.Instance;
    }

    /// <summary>
    /// AppConfigProvider constructor for test.
    /// </summary>
    internal AppConfigProvider(
        IDateTimeWrapper dateTimeWrapper,
        string? appConfigResultKey = null,
        AppConfigResult? appConfigResult = null)
    {
        _dateTimeWrapper = dateTimeWrapper;
        if (appConfigResultKey is not null && appConfigResult is not null)
            _results.TryAdd(appConfigResultKey, appConfigResult);
    }

    #region IParameterProviderConfigurableClient implementation

    /// <summary>
    /// Use a custom client
    /// </summary>
    /// <param name="client">The custom client</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider UseClient(IAmazonAppConfigData client)
    {
        _client = client;
        return this;
    }

    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(region);
        return this;
    }

    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(config);
        return this;
    }

    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(AWSCredentials credentials)
    {
        _client = new AmazonAppConfigDataClient(credentials);
        return this;
    }

    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(credentials, region);
        return this;
    }

    /// <summary>
    /// Configure client with AWS credentials and a client configuration object.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(AWSCredentials credentials, AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(credentials, config);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, region);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, config);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config);
        return this;
    }

    #endregion

    /// <summary>
    /// Sets the default application ID or name.
    /// </summary>
    /// <param name="applicationId">The application ID or name.</param>
    /// <returns>The AppConfigProvider instance.</returns>
    public IAppConfigProvider DefaultApplication(string applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
            throw new ArgumentNullException(nameof(applicationId));
        _defaultApplicationId = applicationId;
        return this;
    }

    /// <summary>
    /// Sets the default environment ID or name.
    /// </summary>
    /// <param name="environmentId">The environment ID or name.</param>
    /// <returns>The AppConfigProvider instance.</returns>
    public IAppConfigProvider DefaultEnvironment(string environmentId)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
            throw new ArgumentNullException(nameof(environmentId));
        _defaultEnvironmentId = environmentId;
        return this;
    }

    /// <summary>
    /// Sets the default configuration profile ID or name.
    /// </summary>
    /// <param name="configProfileId">The configuration profile ID or name.</param>
    /// <returns>The AppConfigProvider instance.</returns>
    public IAppConfigProvider DefaultConfigProfile(string configProfileId)
    {
        _defaultConfigProfileId = configProfileId;
        return this;
    }

    /// <summary>
    /// Sets the application ID or name.
    /// </summary>
    /// <param name="applicationId">The application ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    public AppConfigProviderConfigurationBuilder WithApplication(string applicationId)
    {
        return NewConfigurationBuilder().WithApplication(applicationId);
    }

    /// <summary>
    /// Sets the environment ID or name.
    /// </summary>
    /// <param name="environmentId">The environment ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    public AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId)
    {
        return NewConfigurationBuilder().WithEnvironment(environmentId);
    }

    /// <summary>
    /// Sets the configuration profile ID or name.
    /// </summary>
    /// <param name="configProfileId">The configuration profile ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    public AppConfigProviderConfigurationBuilder WithConfigProfile(string configProfileId)
    {
        return NewConfigurationBuilder().WithConfigProfile(configProfileId);
    }

    /// <summary>
    /// Creates and configures a new AppConfigProviderConfigurationBuilder
    /// </summary>
    /// <returns></returns>
    protected override AppConfigProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new AppConfigProviderConfigurationBuilder(this)
            .WithApplication(_defaultApplicationId)
            .WithEnvironment(_defaultEnvironmentId)
            .WithConfigProfile(_defaultConfigProfileId);

    }

    /// <summary>
    /// Get AppConfig transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The AppConfig transformed value.</returns>
    public override async Task<T?> GetAsync<T>(string key) where T : class
    {
        return await NewConfigurationBuilder()
            .GetAsync<T>(key)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get last AppConfig value.
    /// </summary>
    /// <returns>Application Configuration.</returns>
    public IDictionary<string, string?> Get()
    {
        return GetAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get last AppConfig value.
    /// </summary>
    /// <returns>The AppConfig value.</returns>
    public async Task<IDictionary<string, string?>> GetAsync()
    {
        return await NewConfigurationBuilder()
            .GetAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get last AppConfig value and transform it to JSON value.
    /// </summary>
    /// <typeparam name="T">JSON value type.</typeparam>
    /// <returns>The AppConfig JSON value.</returns>
    public T? Get<T>() where T : class
    {
        return GetAsync<T>().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get last AppConfig value and transform it to JSON value.
    /// </summary>
    /// <typeparam name="T">JSON value type.</typeparam>
    /// <returns>The AppConfig JSON value.</returns>
    public async Task<T?> GetAsync<T>() where T : class
    {
        return await NewConfigurationBuilder()
            .GetAsync<T>()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get parameter value for the provided key. 
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>The parameter value.</returns>
    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        if (config is not AppConfigProviderConfiguration configuration)
            throw new ArgumentNullException(nameof(config));

        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey
        (
            configuration.ApplicationId,
            configuration.EnvironmentId,
            configuration.ConfigProfileId
        );

        var result = GetAppConfigResult(cacheKey);

        if (_dateTimeWrapper.UtcNow < result.NextAllowedPollTime)
        {
            if (!config.ForceFetch)
                return result.LastConfig;

            result.PollConfigurationToken = string.Empty;
            result.NextAllowedPollTime = DateTime.MinValue;
        }

        if (string.IsNullOrWhiteSpace(result.PollConfigurationToken))
            result.PollConfigurationToken =
                await GetInitialConfigurationTokenAsync(configuration)
                    .ConfigureAwait(false);

        var request = new GetLatestConfigurationRequest
        {
            ConfigurationToken = result.PollConfigurationToken
        };

        var response =
            await Client.GetLatestConfigurationAsync(request)
                .ConfigureAwait(false);

        result.PollConfigurationToken = response.NextPollConfigurationToken;
        result.NextAllowedPollTime = _dateTimeWrapper.UtcNow.AddSeconds(response.NextPollIntervalInSeconds);

        if (!string.Equals(response.ContentType, "application/json", StringComparison.CurrentCultureIgnoreCase))
            throw new NotImplementedException($"Not implemented AppConfig type: {response.ContentType}");

        using (var reader = new StreamReader(response.Configuration))
        {
            result.LastConfig =
                await reader.ReadToEndAsync()
                    .ConfigureAwait(false);
        }

        return result.LastConfig;
    }

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    protected override Task<IDictionary<string, string?>> GetMultipleAsync(string key,
        ParameterProviderConfiguration? config)
    {
        throw new NotSupportedException("Impossible to get multiple values from AWS AppConfig");
    }

    /// <summary>
    /// Gets Or Adds AppConfigResult with provided key
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <returns>AppConfigResult</returns>
    private AppConfigResult GetAppConfigResult(string cacheKey)
    {
        if (_results.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;

        cachedResult = new AppConfigResult();
        _results.TryAdd(cacheKey, cachedResult);

        return cachedResult;
    }

    /// <summary>
    /// Starts a configuration session used to retrieve a deployed configuration.
    /// </summary>
    /// <param name="config">Teh AppConfig provider configuration</param>
    /// <returns>The initial configuration token</returns>
    private async Task<string> GetInitialConfigurationTokenAsync(AppConfigProviderConfiguration config)
    {
        var request = new StartConfigurationSessionRequest
        {
            ApplicationIdentifier = config.ApplicationId,
            EnvironmentIdentifier = config.EnvironmentId,
            ConfigurationProfileIdentifier = config.ConfigProfileId
        };

        return (await Client.StartConfigurationSessionAsync(request).ConfigureAwait(false)).InitialConfigurationToken;
    }

    /// <summary>
    /// Check if the feature flag is enabled.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="defaultValue">The default value of the flag</param>
    /// <returns>The feature flag value, or defaultValue if the flag cannot be evaluated</returns>
    public bool IsFeatureFlagEnabled(string key, bool defaultValue = false)
    {
        return IsFeatureFlagEnabledAsync(key, defaultValue).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if the feature flag is enabled.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="defaultValue">The default value of the flag</param>
    /// <returns>The feature flag value, or defaultValue if the flag cannot be evaluated</returns>
    public async Task<bool> IsFeatureFlagEnabledAsync(string key, bool defaultValue = false)
    {
        return await GetFeatureFlagAttributeValueAsync(key, AppConfigFeatureFlagHelper.EnabledAttributeKey,
            defaultValue).ConfigureAwait(false);
    }

    /// <summary>
    /// Get feature flag's attribute value.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="attributeKey">The unique attribute key for the feature flag</param>
    /// <param name="defaultValue">The default value of the feature flag's attribute value</param>
    /// <typeparam name="T">The type of the value to obtain from feature flag's attribute.</typeparam>
    /// <returns>The feature flag's attribute value.</returns>
    public T? GetFeatureFlagAttributeValue<T>(string key, string attributeKey, T? defaultValue = default)
    {
        return GetFeatureFlagAttributeValueAsync(key, attributeKey, defaultValue).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get feature flag's attribute value.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="attributeKey">The unique attribute key for the feature flag</param>
    /// <param name="defaultValue">The default value of the feature flag's attribute value</param>
    /// <typeparam name="T">The type of the value to obtain from feature flag's attribute.</typeparam>
    /// <returns>The feature flag's attribute value.</returns>
    public async Task<T?> GetFeatureFlagAttributeValueAsync<T>(string key, string attributeKey,
        T? defaultValue = default)
    {
        return string.IsNullOrWhiteSpace(key)
            ? defaultValue
            : AppConfigFeatureFlagHelper.GetFeatureFlagAttributeValueAsync(key, attributeKey, defaultValue,
                await GetAsync<JsonObject>().ConfigureAwait(false));
    }
}