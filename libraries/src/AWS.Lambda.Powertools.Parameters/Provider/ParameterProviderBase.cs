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
using AWS.Lambda.Powertools.Parameters.Internal.Provider;

namespace AWS.Lambda.Powertools.Parameters.Provider;

public abstract class ParameterProviderBase : IParameterProviderBase
{
    private IParameterProviderBaseHandler? _handler;

    internal IParameterProviderBaseHandler Handler =>
        _handler ??= new ParameterProviderBaseHandler(GetAsync, GetMultipleAsync, CacheMode);

    protected ICacheManager Cache => Handler.GetCacheManager();

    protected virtual ParameterProviderCacheMode CacheMode => ParameterProviderCacheMode.All;

    protected TimeSpan GetMaxAge(ParameterProviderConfiguration? config)
    {
        return Handler.GetMaxAge(config);
    }

    internal void SetHandler(IParameterProviderBaseHandler handler)
    {
        _handler = handler;
    }

    public string? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    public async Task<string?> GetAsync(string key)
    {
        return await GetAsync<string>(key).ConfigureAwait(false);
    }

    public T? Get<T>(string key) where T : class
    {
        return GetAsync<T>(key).GetAwaiter().GetResult();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        return await Handler.GetAsync<T>(key, null, null, null).ConfigureAwait(false);
    }

    public IDictionary<string, string?> GetMultiple(string path)
    {
        return GetMultipleAsync(path).GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, string?>> GetMultipleAsync(string path)
    {
        return await GetMultipleAsync<string>(path).ConfigureAwait(false);
    }
    
    public IDictionary<string, T?> GetMultiple<T>(string path) where T : class
    {
        return GetMultipleAsync<T>(path).GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, T?>> GetMultipleAsync<T>(string path) where T : class
    {
        return await Handler.GetMultipleAsync<T>(path, null, null, null).ConfigureAwait(false);
    }

    protected abstract Task<string?> GetAsync(string key, ParameterProviderConfiguration? config);

    protected abstract Task<IDictionary<string, string?>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config);
}