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

namespace AWS.Lambda.Powertools.Parameters.Internal.Cache;

/// <summary>
/// Class CacheObject.
/// </summary>
internal class CacheObject
{
    /// <summary>
    /// The value to cache.
    /// </summary>
    internal object Value { get; set; }

    /// <summary>
    /// The expiry time.
    /// </summary>
    internal DateTime ExpiryTime { get; set; }

    /// <summary>
    /// CacheObject constructor.
    /// </summary>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiryTime">The expiry time.</param>
    internal CacheObject(object value, DateTime expiryTime)
    {
        Value = value;
        ExpiryTime = expiryTime;
    }
}