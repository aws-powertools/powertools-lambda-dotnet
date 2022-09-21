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

namespace AWS.Lambda.Powertools.Parameters.Cache;

/// <summary>
/// Class CacheManager.
/// </summary>
internal class CacheManager : ICacheManager
{
    internal static TimeSpan DefaultMaxAge = TimeSpan.FromSeconds(5);
    
    private readonly ConcurrentDictionary<string, CacheObject> _cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly IDateTimeWrapper _dateTimeWrapper;

    internal CacheManager(IDateTimeWrapper dateTimeWrapper)
    {
        _dateTimeWrapper = dateTimeWrapper;
    }

    public object? Get(string key)
    {
        if (!_cache.TryGetValue(key, out var cacheObject))
            return null;

        if (cacheObject.ExpiryTime > _dateTimeWrapper.UtcNow)
            return cacheObject.Value;

        _cache.TryRemove(key, out cacheObject);
        return null;
    }

    public void Set(string key, object? value, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
            return;

        if (_cache.TryGetValue(key, out var cacheObject))
        {
            cacheObject.Value = value;
            cacheObject.ExpiryTime = _dateTimeWrapper.UtcNow.Add(duration);
        }
        else
        {
            _cache.TryAdd(key, new CacheObject(value, _dateTimeWrapper.UtcNow.Add(duration)));
        }
    }
}