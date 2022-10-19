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

using AWS.Lambda.Powertools.Parameters.Internal.AppConfig;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

/// <summary>
/// AppConfigProviderConfigurationBuilder class.
/// </summary>
public class AppConfigProviderConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    /// <summary>
    /// The application Id.
    /// </summary>
    private string? _applicationId;

    /// <summary>
    /// The environment Id.
    /// </summary>
    private string? _environmentId;

    /// <summary>
    /// The configuration profile Id.
    /// </summary>
    private string? _configProfileId;

    /// <summary>
    /// AppConfigProviderConfigurationBuilder constructor
    /// </summary>
    /// <param name="parameterProvider">The AppConfigProvider instance</param>
    public AppConfigProviderConfigurationBuilder(ParameterProviderBase parameterProvider) :
        base(parameterProvider)
    {
    }

    /// <summary>
    /// Sets the application ID or name.
    /// </summary>
    /// <param name="applicationId">The application ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    public AppConfigProviderConfigurationBuilder WithApplication(string applicationId)
    {
        _applicationId = applicationId;
        return this;
    }

    /// <summary>
    /// Sets the environment ID or name.
    /// </summary>
    /// <param name="environmentId">The environment ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    public AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId)
    {
        _environmentId = environmentId;
        return this;
    }

    /// <summary>
    /// Sets the configuration profile ID or name.
    /// </summary>
    /// <param name="configProfileId">The configuration profile ID or name.</param>
    /// <returns>The AppConfigProvider configuration builder.</returns>
    public AppConfigProviderConfigurationBuilder WithConfigProfile(string configProfileId)
    {
        _configProfileId = configProfileId;
        return this;
    }

    /// <summary>
    /// Creates and configures new AppConfigProviderConfiguration instance.
    /// </summary>
    /// <returns></returns>
    protected override ParameterProviderConfiguration NewConfiguration()
    {
        return new AppConfigProviderConfiguration
        {
            EnvironmentId = _environmentId,
            ApplicationId = _applicationId,
            ConfigProfileId = _configProfileId
        };
    }

    /// <summary>
    /// Get AppConfig transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The AppConfig transformed value.</returns>
    public override async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        if (typeof(T) != typeof(string))
            return default;

        var dictionary = await GetAsync().ConfigureAwait(false);
        if (dictionary.TryGetValue(key, out var value) && value != null)
            return (T)(object)value;

        return default;
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
        return await GetAsync<IDictionary<string, string?>>().ConfigureAwait(false) ??
               new Dictionary<string, string?>();
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
        if (!HasTransformation)
        {
            if (typeof(T) == typeof(IDictionary<string, string?>))
                SetTransformer(AppConfigDictionaryTransformer.Instance);
            else
                SetTransformation(Transformation.Json);
        }

        return await base.GetAsync<T>(AppConfigProviderCacheHelper.GetCacheKey(_applicationId, _environmentId,
            _configProfileId)).ConfigureAwait(false);
    }
}