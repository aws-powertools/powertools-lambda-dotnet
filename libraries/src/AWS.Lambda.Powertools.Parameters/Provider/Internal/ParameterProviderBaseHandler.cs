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

using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Provider.Internal;

internal class ParameterProviderBaseHandler : IParameterProviderBaseHandler
{
    internal delegate Task<string?> GetAsyncDelegate(string key, ParameterProviderConfiguration? config);
    internal delegate Task<IDictionary<string, string>> GetMultipleAsyncDelegate(string path,
        ParameterProviderConfiguration? config);

    private ICacheManager? _cache;
    private ITransformerManager? _transformManager;
    private TimeSpan _defaultMaxAge = CacheManager.DefaultMaxAge;
    private readonly GetAsyncDelegate _getAsyncHandler;
    private readonly GetMultipleAsyncDelegate _getMultipleAsyncHandler;

    private ICacheManager Cache => _cache ??= new CacheManager(DateTimeWrapper.Instance);
    private ITransformerManager TransformManager => _transformManager ??= TransformerManager.Instance;

    internal ParameterProviderBaseHandler(GetAsyncDelegate getAsyncHandler,
        GetMultipleAsyncDelegate getMultipleAsyncHandler)
    {
        _getAsyncHandler = getAsyncHandler;
        _getMultipleAsyncHandler = getMultipleAsyncHandler;
    }
    
    private async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        return await _getAsyncHandler(key, config);
    }

    private async Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config)
    {
        return await _getMultipleAsyncHandler(path, config);
    }

    public void SetDefaultMaxAge(TimeSpan maxAge)
    {
        _defaultMaxAge = maxAge;
    }

    public TimeSpan GetDefaultMaxAge()
    {
        return _defaultMaxAge;
    }

    public void SetCacheManager(ICacheManager cacheManager)
    {
        _cache = cacheManager;
    }

    public void SetTransformerManager(ITransformerManager transformerManager)
    {
        _transformManager = transformerManager;
    }

    public void AddCustomTransformer(string name, ITransformer transformer)
    {
        TransformManager.AddTransformer(name, transformer);
    }

    public async Task<T?> GetAsync<T>(string key, ParameterProviderConfiguration? config,
        Transformation? transformation, string? transformerName)
    {
        var cachedObject = config is null || !config.ForceFetch ? Cache.Get(key) : null;
        if (cachedObject is T cachedValue)
            return cachedValue;

        var value = await GetAsync(key, config).ConfigureAwait(false);
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
        
        T? retValue;
        if (transformer is not null)
            retValue = transformer.Transform<T>(value);
        else if (value is T strVal)
            retValue = strVal;
        else
            throw new Exception($"Transformer is required. '{value}' cannot be converted to type '{typeof(T)}'.");

        var maxAge = config?.MaxAge is not null && config.MaxAge > TimeSpan.Zero ? config.MaxAge : _defaultMaxAge;
        Cache.Set(key, retValue, maxAge);

        return retValue;
    }

    public async Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config, Transformation? transformation, string? transformerName)
    {
        var cachedObject = config is null || !config.ForceFetch ? Cache.Get(path) : null;
        if (cachedObject is IDictionary<string, string> cachedValue)
            return cachedValue;

        var respValues = await GetMultipleAsync(path, config).ConfigureAwait(false);
        if (!respValues.Any())
            return respValues;

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

        var maxAge = config?.MaxAge is not null && config.MaxAge > TimeSpan.Zero ? config.MaxAge : _defaultMaxAge;
        var retValues = new Dictionary<string, string>();
        foreach (var (key, value) in respValues)
        {
            var newTransformer = transformer;
            if (newTransformer is null && transformation == Transformation.Auto)
                newTransformer = TransformManager.TryGetTransformer(transformation.Value, key);

            var newValue = value;
            if (newTransformer is not null)
                newValue = newTransformer.Transform<string>(value) ?? "";

            Cache.Set(key, newValue, maxAge);
            retValues.Add(key, newValue);
        }

        Cache.Set(path, retValues, maxAge);
        return respValues;
    }
}