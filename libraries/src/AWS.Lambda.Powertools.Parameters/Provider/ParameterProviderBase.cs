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

namespace AWS.Lambda.Powertools.Parameters.Provider;

public abstract class ParameterProviderBase : IParameterProviderBase
{
    private ICacheManager? _cache;
    private ITransformerManager? _transformManager;
    private TimeSpan _defaultMaxAge = CacheManager.DefaultMaxAge;

    private ICacheManager Cache => _cache ??= new CacheManager(DateTimeWrapper.Instance);
    private ITransformerManager TransformManager => _transformManager ??= TransformerManager.Instance;

    #region Internal Functions

    internal void SetDefaultMaxAge(TimeSpan maxAge)
    {
        _defaultMaxAge = maxAge;
    }

    internal TimeSpan GetDefaultMaxAge()
    {
        return _defaultMaxAge;
    }

    internal void SetCacheManager(ICacheManager cacheManager)
    {
        _cache = cacheManager;
    }

    internal void SetTransformerManager(ITransformerManager transformerManager)
    {
        _transformManager = transformerManager;
    }

    internal void AddCustomTransformer(string name, ITransformer transformer)
    {
        TransformManager.AddTransformer(name, transformer);
    }

    public async Task<T?> GetAsync<T>(string key, ParameterProviderConfiguration? config,
        Transformation? transformation, string? transformerName)
    {
        var cachedObject = config is null || !config.ForceFetch ? Cache.Get(key) : null;
        if (cachedObject is T cachedValue)
            return cachedValue;

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

        var value = await GetAsync(key, config).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(value))
            return default;

        var maxAge = config?.MaxAge ?? _defaultMaxAge;

        T? retValue;
        if (transformer is not null)
            retValue = transformer.Transform<T>(value);
        else if (value is T strVal)
            retValue = strVal;
        else
            throw new Exception($"Transformer is required. '{value}' cannot be converted to type '{typeof(T)}'.");

        Cache.Set(key, retValue, maxAge);

        return retValue;
    }

    public async Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config, Transformation? transformation, string? transformerName)
    {
        var cachedObject = config is null || !config.ForceFetch ? Cache.Get(path) : null;
        if (cachedObject is IDictionary<string, string> cachedValue)
            return cachedValue;

        var transformer = config?.Transformer;
        if (transformer is null && transformation.HasValue && transformation.Value != Transformation.Auto)
        {
            transformer = TransformManager.GetTransformer(transformation.Value);
            if (config is not null)
                config.Transformer = transformer;
        }

        var retValues = await GetMultipleAsync(path, config).ConfigureAwait(false);
        if (!retValues.Any())
            return retValues;

        var maxAge = config?.MaxAge ?? _defaultMaxAge;
        Cache.Set(path, retValues, maxAge);
        foreach (var (key, value) in retValues)
            Cache.Set(key, value, maxAge);

        return retValues;
    }

    #endregion

    public string? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    public async Task<string?> GetAsync(string key)
    {
        return await GetAsync<string>(key, null, null, null).ConfigureAwait(false);
    }

    public T? Get<T>(string key) where T : class
    {
        return GetAsync<T>(key).GetAwaiter().GetResult();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        return await GetAsync<T>(key, null, null, null).ConfigureAwait(false);
    }

    public IDictionary<string, string> GetMultiple(string path)
    {
        return GetMultipleAsync(path).GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, string>> GetMultipleAsync(string path)
    {
        return await GetMultipleAsync(path, null, null, null).ConfigureAwait(false);
    }

    protected abstract Task<string?> GetAsync(string key, ParameterProviderConfiguration? config);

    protected abstract Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config);
}