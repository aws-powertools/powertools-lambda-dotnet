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

public class ParameterProviderConfigurationBuilder : IProviderBase
{
    private bool _forceFetch;
    private Transformation? _transformation;
    private ITransformer? _transformer;
    private string? _transformerName;
    private TimeSpan? _maxAge;
    private readonly IParameterProviderBaseHandler _parameterProvider;

    /// <summary>
    /// Constructor for test purpose
    /// </summary>
    /// <param name="parameterProvider"></param>
    internal ParameterProviderConfigurationBuilder(IParameterProviderBaseHandler parameterProvider)
    {
        _parameterProvider = parameterProvider;
    }
    
    public ParameterProviderConfigurationBuilder(ParameterProviderBase parameterProvider)
    {
        _parameterProvider = parameterProvider.Handler;
    }

    #region Internal Functions

    private ParameterProviderConfiguration GetConfiguration()
    {
        var config = NewConfiguration();

        config.ForceFetch = _forceFetch;
        config.MaxAge = _maxAge;
        config.Transformer = _transformer;

        return config;
    }

    internal void SetForceFetch(bool forceFetch)
    {
        _forceFetch = forceFetch;
    }

    internal void SetMaxAge(TimeSpan age)
    {
        _maxAge = age;
    }

    internal void SetTransformation(Transformation transformation)
    {
        _transformer = null; 
        _transformerName = null;
        _transformation = transformation;
    }

    internal void SetTransformer(ITransformer transformer)
    {
        _transformation = null;
        _transformerName = null;
        _transformer = transformer;
    }
    
    internal void SetTransformerName(string transformerName)
    {
        _transformation = null; 
        _transformer = null;
        _transformerName = transformerName;
    }
    
    protected virtual ParameterProviderConfiguration NewConfiguration()
    {
        return new ParameterProviderConfiguration();
    }

    #endregion

    #region Public Functions

    public string? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _parameterProvider
            .GetAsync<string>(key, GetConfiguration(), _transformation, _transformerName)
            .ConfigureAwait(false);
    }

    public T? Get<T>(string key) where T : class
    {
        return GetAsync<T>(key).GetAwaiter().GetResult();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        return await _parameterProvider
            .GetAsync<T>(key, GetConfiguration(), _transformation, _transformerName)
            .ConfigureAwait(false);
    }

    public IDictionary<string, string> GetMultiple(string path)
    {
        return GetMultipleAsync(path).GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, string>> GetMultipleAsync(string path)
    {
        return await _parameterProvider
            .GetMultipleAsync(path, GetConfiguration(), _transformation, _transformerName)
            .ConfigureAwait(false);
    }

    #endregion
}