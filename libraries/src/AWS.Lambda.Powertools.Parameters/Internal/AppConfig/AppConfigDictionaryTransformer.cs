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

using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Internal.AppConfig;

/// <summary>
/// Transformer to deserialize dictionary from JSON string.
/// </summary>
internal class AppConfigDictionaryTransformer : ITransformer
{
    /// <summary>
    /// The transformer instance.
    /// </summary>
    private static AppConfigDictionaryTransformer? _instance;

    /// <summary>
    /// Gets the transformer instance.
    /// </summary>
    internal static AppConfigDictionaryTransformer Instance => _instance ??= new AppConfigDictionaryTransformer();

    /// <summary>
    /// AppConfigDictionaryTransformer constructor.
    /// </summary>
    private AppConfigDictionaryTransformer()
    {
        
    }

    /// <summary>
    /// Deserialize a dictionary from a JSON string.
    /// </summary>
    /// <param name="value">JSON string.</param>
    /// <typeparam name="T">JSON value type.</typeparam>
    /// <returns>Key/value pair collection.</returns>
    public T? Transform<T>(string value)
    {
        if (typeof(T) == typeof(string))
            return (T)(object)value;

        if (string.IsNullOrWhiteSpace(value))
            return default;

        if (typeof(T) != typeof(IDictionary<string, string?>))
            return default;
        
        return (T)AppConfigJsonConfigurationParser.Parse(value);
    }
}