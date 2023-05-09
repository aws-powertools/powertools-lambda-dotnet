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
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Internal.Transform;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Internal.Provider;

/// <summary>
/// ParameterProviderBaseHandler class.
/// </summary>
internal class ParameterProviderBaseHandler : IParameterProviderBaseHandler
{
    /// <summary>
    /// The parameter provider GetAsync callback.
    /// </summary>
    internal delegate Task<string?> GetAsyncDelegate(string key, ParameterProviderConfiguration? config);

    /// <summary>
    /// The parameter provider GetMultipleAsync callback.
    /// </summary>
    internal delegate Task<IDictionary<string, string?>> GetMultipleAsyncDelegate(string key,
        ParameterProviderConfiguration? config);

    /// <summary>
    /// The CacheManager instance.
    /// </summary>
    private ICacheManager? _cache;
    
    /// <summary>
    /// The TransformerManager instance.
    /// </summary>
    private ITransformerManager? _transformManager;
    
    /// <summary>
    /// The DefaultMaxAge.
    /// </summary>
    private TimeSpan? _defaultMaxAge;
    
    /// <summary>
    /// The flag to raise exception on transformation error.
    /// </summary>
    private bool _raiseTransformationError;
    
    /// <summary>
    /// The CacheMode.
    /// </summary>
    private readonly ParameterProviderCacheMode _cacheMode;
    
    /// <summary>
    /// The parameter provider GetAsync callback handler.
    /// </summary>
    private readonly GetAsyncDelegate _getAsyncHandler;
    
    /// <summary>
    /// The parameter provider GetMultipleAsync callback handler.
    /// </summary>
    private readonly GetMultipleAsyncDelegate _getMultipleAsyncHandler;

    /// <summary>
    /// Gets the CacheManager instance.
    /// </summary>
    private ICacheManager Cache => _cache ??= new CacheManager(DateTimeWrapper.Instance);
    
    /// <summary>
    /// Gets the TransformManager instance.
    /// </summary>
    private ITransformerManager TransformManager => _transformManager ??= TransformerManager.Instance;

    /// <summary>
    /// ParameterProviderBaseHandler constructor
    /// </summary>
    /// <param name="getAsyncHandler">The parameter provider GetAsync callback handler.</param>
    /// <param name="getMultipleAsyncHandler">The parameter provider GetMultipleAsync callback handler.</param>
    /// <param name="cacheMode">The CacheMode.</param>
    /// <param name="powertoolsConfigurations">The Powertools configurations.</param>
    internal ParameterProviderBaseHandler(GetAsyncDelegate getAsyncHandler,
        GetMultipleAsyncDelegate getMultipleAsyncHandler,
        ParameterProviderCacheMode cacheMode,
        IPowertoolsConfigurations powertoolsConfigurations)
    {
        _getAsyncHandler = getAsyncHandler;
        _getMultipleAsyncHandler = getMultipleAsyncHandler;
        _cacheMode = cacheMode;
        powertoolsConfigurations.SetExecutionEnvironment(this);
    }

    /// <summary>
    /// Try transform a value using a transformer.
    /// </summary>
    /// <param name="transformer">The transformer instance to use.</param>
    /// <param name="value">The value to transform.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The transformed value.</returns>
    /// <exception cref="TransformationException"></exception>
    private T? TryTransform<T>(ITransformer? transformer, string? value)
    {
        T? transformedValue = default;
        if (value is null)
            return transformedValue;
        
        if (transformer is not null)
        {
            try
            {
                transformedValue = transformer.Transform<T>(value);
            }
            catch (Exception e)
            {
                transformedValue = default;
                if (_raiseTransformationError)
                {
                    if (e is not TransformationException error)
                        error = new TransformationException(e.Message, e);
                    throw error;
                }
            }
        }
        else if (value is T strVal)
            transformedValue = strVal;
        else
            throw new TransformationException(
                $"Transformer is required. '{value}' cannot be converted to type '{typeof(T)}'.");

        return transformedValue;
    }

    /// <summary>
    /// Sets the cache maximum age.
    /// </summary>
    /// <param name="maxAge">The cache maximum age </param>
    public void SetDefaultMaxAge(TimeSpan maxAge)
    {
        _defaultMaxAge = maxAge;
    }

    /// <summary>
    /// Gets the maximum age or default value.
    /// </summary>
    /// <returns>the maxAge</returns>
    public TimeSpan? GetDefaultMaxAge()
    {
        return _defaultMaxAge;
    }
    
    /// <summary>
    /// Gets the maximum age or default value.
    /// </summary>
    /// <param name="config"></param>
    /// <returns>the maxAge</returns>
    public TimeSpan GetMaxAge(ParameterProviderConfiguration? config)
    {
        var maxAge = config?.MaxAge;
        if (maxAge.HasValue && maxAge.Value > TimeSpan.Zero) return maxAge.Value;
        if (_defaultMaxAge.HasValue && _defaultMaxAge.Value > TimeSpan.Zero) return _defaultMaxAge.Value;
        return CacheManager.DefaultMaxAge;
    }

    /// <summary>
    /// Sets the CacheManager.
    /// </summary>
    /// <param name="cacheManager">The CacheManager instance.</param>
    public void SetCacheManager(ICacheManager cacheManager)
    {
        _cache = cacheManager;
    }

    /// <summary>
    /// Gets the CacheManager instance.
    /// </summary>
    /// <returns>The CacheManager instance</returns>
    public ICacheManager GetCacheManager()
    {
        return Cache;
    }

    /// <summary>
    /// Sets the TransformerManager.
    /// </summary>
    /// <param name="transformerManager">The TransformerManager instance.</param>
    public void SetTransformerManager(ITransformerManager transformerManager)
    {
        _transformManager = transformerManager;
    }

    /// <summary>
    /// Registers a new transformer instance by name.
    /// </summary>
    /// <param name="name">The transformer unique name.</param>
    /// <param name="transformer">The transformer instance.</param>
    public void AddCustomTransformer(string name, ITransformer transformer)
    {
        TransformManager.AddTransformer(name, transformer);
    }

    /// <summary>
    /// Configure the transformer to raise exception or return Null on transformation error
    /// </summary>
    /// <param name="raiseError">true for raise error, false for return Null.</param>
    public void SetRaiseTransformationError(bool raiseError)
    {
        _raiseTransformationError = raiseError;
    }

    /// <summary>
    /// Gets parameter value for the provided key and configuration.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The optional parameter provider configuration.</param>
    /// <param name="transformation">The optional transformation.</param>
    /// <param name="transformerName">The optional transformer name.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter value.</returns>
    public async Task<T?> GetAsync<T>(string key, ParameterProviderConfiguration? config,
        Transformation? transformation, string? transformerName) where T : class
    {
        var cachedObject = config is null || !config.ForceFetch ? Cache.Get(key) : null;
        if (cachedObject is T cachedValue)
            return cachedValue;

        var value = await _getAsyncHandler(key, config).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(value))
            return default;

        var transformer = config?.Transformer;
        if (transformer is null)
        {
            if (!string.IsNullOrWhiteSpace(transformerName))
                transformer = TransformManager.GetTransformer(transformerName);
            else if (transformation.HasValue)
                transformer = TransformManager.TryGetTransformer(transformation.Value, key);

            if (config is not null)
                config.Transformer = transformer;
        }
        
        var retValue = TryTransform<T>(transformer, value);
        if (retValue is null) 
            return retValue;
        
        if (_cacheMode is ParameterProviderCacheMode.All or ParameterProviderCacheMode.GetResultOnly)
            Cache.Set(key, retValue, GetMaxAge(config));

        return retValue;
    }

    /// <summary>
    /// Gets multiple parameter values for the provided key and configuration.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The optional parameter provider configuration.</param>
    /// <param name="transformation">The optional transformation.</param>
    /// <param name="transformerName">The optional transformer name.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    public async Task<IDictionary<string, T?>> GetMultipleAsync<T>(string key,
        ParameterProviderConfiguration? config, Transformation? transformation, string? transformerName) where T : class
    {
        var cachedObject = config is null || !config.ForceFetch ? Cache.Get(key) : null;
        if (cachedObject is IDictionary<string, T?> cachedValue)
            return cachedValue;

        var retValues = new Dictionary<string, T?>();

        var respValues = await _getMultipleAsyncHandler(key, config)
            .ConfigureAwait(false);

        if (!respValues.Any())
            return retValues;

        var transformer = config?.Transformer;
        if (transformer is null)
        {
            if (!string.IsNullOrWhiteSpace(transformerName))
                transformer = TransformManager.GetTransformer(transformerName);
            else if (transformation.HasValue && transformation.Value != Transformation.Auto)
                transformer = TransformManager.GetTransformer(transformation.Value);

            if (config is not null)
                config.Transformer = transformer;
        }

        foreach (var (k, v) in respValues)
        {
            var newTransformer = transformer;
            if (newTransformer is null && transformation == Transformation.Auto)
                newTransformer = TransformManager.TryGetTransformer(transformation.Value, k);
            
            retValues.Add(k, TryTransform<T>(newTransformer, v));
        }

        if (_cacheMode is ParameterProviderCacheMode.All or ParameterProviderCacheMode.GetMultipleResultOnly)
            Cache.Set(key, retValues, GetMaxAge(config));

        return retValues;
    }
}