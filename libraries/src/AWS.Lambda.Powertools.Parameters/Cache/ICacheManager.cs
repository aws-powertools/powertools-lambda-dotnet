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

namespace AWS.Lambda.Powertools.Parameters.Cache;

/// <summary>
/// Represents a type used to manage cache.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Retrieves a cached value by key. 
    /// </summary>
    /// <param name="key">The key to retrieve.</param>
    /// <returns>The cached object.</returns>
    object? Get(string key);

    /// <summary>
    /// Adds a value to the cache by key for a specific duration. 
    /// </summary>
    /// <param name="key">The key to store the value.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="duration">The expiry duration.</param>
    void Set(string key, object? value, TimeSpan duration);
}