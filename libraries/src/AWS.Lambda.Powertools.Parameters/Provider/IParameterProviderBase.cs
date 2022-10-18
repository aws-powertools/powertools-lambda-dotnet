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

namespace AWS.Lambda.Powertools.Parameters.Provider;

/// <summary>
/// Represents a base type used to retrieve parameter values from a store.
/// </summary>
public interface IParameterProviderBase
{
    /// <summary>
    /// Get parameter value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value.</returns>
    string? Get(string key);
    
    /// <summary>
    /// Get parameter value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
    T? Get<T>(string key) where T : class;

    /// <summary>
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    IDictionary<string, string?> GetMultiple(string key);

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    Task<IDictionary<string, string?>> GetMultipleAsync(string key);
    
    /// <summary>
    /// Get multiple transformed parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/transformed value pairs.</returns>
    IDictionary<string, T?> GetMultiple<T>(string key) where T : class;

    /// <summary>
    /// Get multiple transformed parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/transformed value pairs.</returns>
    Task<IDictionary<string, T?>> GetMultipleAsync<T>(string key) where T : class;
}