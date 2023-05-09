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

using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Transform;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;

namespace AWS.Lambda.Powertools.Parameters.Provider;

/// <summary>
/// Provide a generic view of a parameter provider. This is an abstract class.  
/// </summary>
public abstract class ParameterProvider : IParameterProvider
{
    /// <summary>
    /// The parameter provider handler instance.
    /// </summary>
    private IParameterProviderBaseHandler? _handler;

    /// <summary>
    /// Gets parameter provider handler instance.
    /// </summary>
    internal IParameterProviderBaseHandler Handler =>
        _handler ??= new ParameterProviderBaseHandler(
            GetAsync,
            GetMultipleAsync,
            CacheMode,
            PowertoolsConfigurations.Instance
        );

    /// <summary>
    /// Gets the CacheManager instance.
    /// </summary>
    protected ICacheManager Cache => Handler.GetCacheManager();

    /// <summary>
    /// Gets parameter provider cache mode.
    /// </summary>
    protected virtual ParameterProviderCacheMode CacheMode => ParameterProviderCacheMode.All;

    /// <summary>
    /// Get the cache maximum age based on provided configuration.
    /// </summary>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>The cache maximum age.</returns>
    protected TimeSpan GetMaxAge(ParameterProviderConfiguration? config)
    {
        return Handler.GetMaxAge(config);
    }

    /// <summary>
    /// Sets parameter provider handler instance.
    /// </summary>
    internal void SetHandler(IParameterProviderBaseHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Get parameter value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value.</returns>
    public string? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get parameter value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value.</returns>
    public async Task<string?> GetAsync(string key)
    {
        return await GetAsync<string>(key).ConfigureAwait(false);
    }

    /// <summary>
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
    public T? Get<T>(string key) where T : class
    {
        return GetAsync<T>(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
    public virtual async Task<T?> GetAsync<T>(string key) where T : class
    {
        return await Handler.GetAsync<T>(key, null, null, null).ConfigureAwait(false);
    }

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    public IDictionary<string, string?> GetMultiple(string key)
    {
        return GetMultipleAsync(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    public async Task<IDictionary<string, string?>> GetMultipleAsync(string key)
    {
        return await GetMultipleAsync<string>(key).ConfigureAwait(false);
    }

    /// <summary>
    /// Get multiple transformed parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/transformed value pairs.</returns>
    public IDictionary<string, T?> GetMultiple<T>(string key) where T : class
    {
        return GetMultipleAsync<T>(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get multiple transformed parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/transformed value pairs.</returns>
    public virtual async Task<IDictionary<string, T?>> GetMultipleAsync<T>(string key) where T : class
    {
        return await Handler.GetMultipleAsync<T>(key, null, null, null).ConfigureAwait(false);
    }

    /// <summary>
    /// Get parameter value for the provided key. 
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>The parameter value.</returns>
    protected abstract Task<string?> GetAsync(string key, ParameterProviderConfiguration? config);

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    protected abstract Task<IDictionary<string, string?>> GetMultipleAsync(string key,
        ParameterProviderConfiguration? config);
    
    /// <summary>
    /// Creates and configures a new instance of the specified type ConfigurationBuilder.
    /// </summary>
    /// <returns>A new instance of ConfigurationBuilder.</returns>
    private ParameterProviderConfigurationBuilder CreateConfigurationBuilder()
    {
        var configBuilder = new ParameterProviderConfigurationBuilder(Handler);
        var maxAge = Handler.GetDefaultMaxAge();
        if (maxAge is not null)
            configBuilder = configBuilder.WithMaxAge(maxAge.Value);
        return configBuilder;
    }

    /// <summary>
    /// Set the cache maximum age.
    /// </summary>
    /// <param name="maxAge">The maximum cache age</param>
    /// <returns>Provider Configuration Builder instance</returns>
    public ParameterProviderConfigurationBuilder WithMaxAge(TimeSpan maxAge)
    {
        return CreateConfigurationBuilder().WithMaxAge(maxAge);
    }

    /// <summary>
    /// Forces provider to fetch the latest value from the store regardless if already available in cache.
    /// </summary>
    /// <returns>Provider Configuration Builder instance</returns>
    public ParameterProviderConfigurationBuilder ForceFetch()
    {
        return CreateConfigurationBuilder().ForceFetch();
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformation">The transformation type.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    public ParameterProviderConfigurationBuilder WithTransformation(Transformation transformation)
    {
        return CreateConfigurationBuilder().WithTransformation(transformation);
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformer">The instance of the transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    public ParameterProviderConfigurationBuilder WithTransformation(ITransformer transformer)
    {
        return CreateConfigurationBuilder().WithTransformation(transformer);
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformerName">The name of the registered transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    public ParameterProviderConfigurationBuilder WithTransformation(string transformerName)
    {
        return CreateConfigurationBuilder().WithTransformation(transformerName);
    }
}