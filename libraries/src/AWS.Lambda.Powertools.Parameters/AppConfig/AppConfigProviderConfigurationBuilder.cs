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

public class AppConfigProviderConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    private string? _environmentId;
    private string? _applicationId;
    private string? _configProfileId;
    
    private ITransformer? _dictionaryTransformer;
    
    private ITransformer DictionaryTransformer => _dictionaryTransformer ??= AppConfigDictionaryTransformer.Instance;


    public AppConfigProviderConfigurationBuilder(ParameterProviderBase parameterProvider) :
        base(parameterProvider)
    {
    }

    public AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId)
    {
        _environmentId = environmentId;
        return this;
    }

    public AppConfigProviderConfigurationBuilder WithApplication(string applicationId)
    {
        _applicationId = applicationId;
        return this;
    }

    public AppConfigProviderConfigurationBuilder WithConfigProfile(string configProfileId)
    {
        _configProfileId = configProfileId;
        return this;
    }

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
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
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

    public IDictionary<string, string?> Get()
    {
        return GetAsync().GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, string?>> GetAsync()
    {
        return await GetAsync<IDictionary<string, string?>>().ConfigureAwait(false) ??
               new Dictionary<string, string?>();
    }

    public T? Get<T>() where T : class
    {
        return GetAsync<T>().GetAwaiter().GetResult();
    }

    public async Task<T?> GetAsync<T>() where T : class
    {
        if (typeof(T) == typeof(IDictionary<string, string?>))
            SetTransformer(DictionaryTransformer);
        else
            SetTransformation(Transformation.Json);

        return await base.GetAsync<T>(AppConfigProviderCacheHelper.GetCacheKey(_applicationId, _environmentId,
            _configProfileId)).ConfigureAwait(false);
    }
}