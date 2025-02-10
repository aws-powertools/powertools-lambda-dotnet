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

namespace AWS.Lambda.Powertools.Parameters.Internal.AppConfig;

/// <summary>
/// AppConfigProviderCacheHelper class.
/// </summary>
internal static class AppConfigProviderCacheHelper
{
    /// <summary>
    /// Gets a new  key for caching from provided inputs.
    /// </summary>
    /// <param name="applicationId">The application Id.</param>
    /// <param name="environmentId">The environment Id.</param>
    /// <param name="configProfileId">the configuration profile Id.</param>
    /// <returns>The cache key</returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static string GetCacheKey(string? applicationId, string? environmentId, string? configProfileId)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
            throw new ArgumentNullException(nameof(applicationId));
        if (string.IsNullOrWhiteSpace(environmentId))
            throw new ArgumentNullException(nameof(environmentId));
        if (string.IsNullOrWhiteSpace(configProfileId))
            throw new ArgumentNullException(nameof(configProfileId));

        return $"{applicationId}_{environmentId}_{configProfileId}";
    }
}