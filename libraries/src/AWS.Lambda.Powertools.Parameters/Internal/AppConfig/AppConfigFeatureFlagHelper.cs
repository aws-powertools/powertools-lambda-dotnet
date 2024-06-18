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

using System.Text.Json.Nodes;

namespace AWS.Lambda.Powertools.Parameters.Internal.AppConfig;

/// <summary>
/// AppConfigProviderCacheHelper class.
/// </summary>
internal static class AppConfigFeatureFlagHelper
{
    internal const string EnabledAttributeKey = "enabled";
    
    /// <summary>
    /// Get feature flag's attribute value.
    /// </summary>
    /// <param name="key">The unique feature key for the feature flag</param>
    /// <param name="attributeKey">The unique attribute key for the feature flag</param>
    /// <param name="defaultValue">The default value of the feature flag's attribute value</param>
    /// <param name="featureFlag">The AppConfig JSON value of the feature flag</param>
    /// <typeparam name="T">The type of the value to obtain from feature flag's attribute.</typeparam>
    /// <returns>The feature flag's attribute value.</returns>
    internal static T? GetFeatureFlagAttributeValueAsync<T>(string key, string attributeKey, T? defaultValue,
        JsonObject? featureFlag)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(attributeKey) || featureFlag is null)
            return defaultValue;

        var keyElement = featureFlag[key];
        if (keyElement is null)
            return defaultValue;

        var attributeElement = keyElement[attributeKey];
        if (attributeElement is null)
            return defaultValue;

        return attributeElement.GetValue<T>();
    }
}