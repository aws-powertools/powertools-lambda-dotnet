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
using AWS.Lambda.Powertools.Parameters.Cache;

namespace AWS.Lambda.Powertools.Parameters.Internal.Cache;

/// <summary>
/// Class CacheManager.
/// </summary>
internal class CacheManager : ICacheManager
{
    /// <summary>
    /// The DefaultMaxAge of five seconds
    /// </summary>
    internal static TimeSpan DefaultMaxAge = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Thread safe dictionary to cache objects  
    /// </summary>
    private readonly ConcurrentDictionary<string, CacheObject> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Instance of datetime wrapper.
    /// </summary>
    private readonly IDateTimeWrapper _dateTimeWrapper;

    /// <summary>
    /// CacheManager Constructor
    /// </summary>
    /// <param name="dateTimeWrapper">Instance of datetime wrapper</param>
    internal CacheManager(IDateTimeWrapper dateTimeWrapper)
    {
        _dateTimeWrapper = dateTimeWrapper;
    }

    /// <summary>
    /// Retrieves a cached value by key. 
    /// </summary>
    /// <param name="key">The key to retrieve.</param>
    /// <returns>The cached object.</returns>
    public object? Get(string key)
    {
        if (!_cache.TryGetValue(key, out var cacheObject))
            return null;

        if (cacheObject.ExpiryTime > _dateTimeWrapper.UtcNow)
            return cacheObject.Value;

        _cache.TryRemove(key, out cacheObject);
        return null;
    }

    /// <summary>
    /// Adds a value to the cache by key for a specific duration. 
    /// </summary>
    /// <param name="key">The key to store the value.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="duration">The expiry duration.</param>
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