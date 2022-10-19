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

using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Configuration;

public class ParameterProviderConfigurationBuilder
{
    /// <summary>
    /// the force fetch.
    /// </summary>
    private bool _forceFetch;
    
    /// <summary>
    /// The transformation.
    /// </summary>
    private Transformation? _transformation;
    
    /// <summary>
    /// The transformer instance.
    /// </summary>
    private ITransformer? _transformer;
    
    /// <summary>
    /// The transformer name.
    /// </summary>
    private string? _transformerName;
    
    /// <summary>
    /// The cache maximum age.
    /// </summary>
    private TimeSpan? _maxAge;
    
    /// <summary>
    /// Has transformation or custom transformer
    /// </summary>
    protected bool HasTransformation { get; private set; }
    
    /// <summary>
    /// The parameter provider handler instance.
    /// </summary>
    private readonly IParameterProviderBaseHandler _handler;

    /// <summary>
    /// Constructor for test purpose.
    /// </summary>
    /// <param name="handler">The parameter provider handler instance</param>
    internal ParameterProviderConfigurationBuilder(IParameterProviderBaseHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// ParameterProviderConfigurationBuilder Constructor.
    /// </summary>
    /// <param name="parameterProvider">The parameter provider instance</param>
    public ParameterProviderConfigurationBuilder(ParameterProviderBase parameterProvider)
    {
        _handler = parameterProvider.Handler;
    }

    #region Internal Functions

    /// <summary>
    /// Creates, configures and returns an instance of parameter provider configuration.
    /// </summary>
    /// <returns>The parameter provider configuration</returns>
    private ParameterProviderConfiguration GetConfiguration()
    {
        var config = NewConfiguration();

        config.ForceFetch = _forceFetch;
        config.MaxAge = _maxAge;
        config.Transformer = _transformer;

        return config;
    }

    /// <summary>
    /// Set the force fetch.
    /// </summary>
    internal void SetForceFetch(bool forceFetch)
    {
        _forceFetch = forceFetch;
    }

    /// <summary>
    /// Set the max age.
    /// </summary>
    internal void SetMaxAge(TimeSpan age)
    {
        _maxAge = age;
    }

    /// <summary>
    /// Set the transformation.
    /// </summary>
    internal void SetTransformation(Transformation transformation)
    {
        _transformer = null;
        _transformerName = null;
        _transformation = transformation;
        HasTransformation = true;
    }

    /// <summary>
    /// Set the transformer.
    /// </summary>
    internal void SetTransformer(ITransformer transformer)
    {
        _transformation = null;
        _transformerName = null;
        _transformer = transformer;
        HasTransformation = true;
    }

    /// <summary>
    /// Set the transformer name.
    /// </summary>
    internal void SetTransformerName(string transformerName)
    {
        _transformation = null;
        _transformer = null;
        _transformerName = transformerName;
        HasTransformation = true;
    }

    /// <summary>
    /// Creates and returns an instance of parameter provider configuration.
    /// </summary>
    /// <returns>The parameter provider configuration</returns>
    protected virtual ParameterProviderConfiguration NewConfiguration()
    {
        return new ParameterProviderConfiguration();
    }

    #endregion

    #region Public Functions

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
        return await _handler
            .GetAsync<T>(key, GetConfiguration(), _transformation, _transformerName)
            .ConfigureAwait(false);
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
        return await _handler
            .GetMultipleAsync<T>(key, GetConfiguration(), _transformation, _transformerName)
            .ConfigureAwait(false);
    }

    #endregion
}