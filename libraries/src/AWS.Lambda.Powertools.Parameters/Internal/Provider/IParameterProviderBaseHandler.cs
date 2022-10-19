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

namespace AWS.Lambda.Powertools.Parameters.Internal.Provider;

/// <summary>
/// Represents a type used to retrieve parameter values from a store.
/// </summary>
public interface IParameterProviderBaseHandler
{
    /// <summary>
    /// Sets the cache maximum age.
    /// </summary>
    /// <param name="maxAge">The cache maximum age </param>
    void SetDefaultMaxAge(TimeSpan maxAge);

    /// <summary>
    /// Gets the maximum age or default value.
    /// </summary>
    /// <returns>the maxAge</returns>
    TimeSpan? GetDefaultMaxAge();

    /// <summary>
    /// Gets the maximum age or default value.
    /// </summary>
    /// <param name="config"></param>
    /// <returns>the maxAge</returns>
    TimeSpan GetMaxAge(ParameterProviderConfiguration? config);

    /// <summary>
    /// Sets the CacheManager.
    /// </summary>
    /// <param name="cacheManager">The CacheManager instance.</param>
    void SetCacheManager(ICacheManager cacheManager);

    /// <summary>
    /// Gets the CacheManager instance.
    /// </summary>
    /// <returns>The CacheManager instance</returns>
    ICacheManager GetCacheManager();

    /// <summary>
    /// Sets the TransformerManager.
    /// </summary>
    /// <param name="transformerManager">The TransformerManager instance.</param>
    void SetTransformerManager(ITransformerManager transformerManager);

    /// <summary>
    /// Registers a new transformer instance by name.
    /// </summary>
    /// <param name="name">The transformer unique name.</param>
    /// <param name="transformer">The transformer instance.</param>
    void AddCustomTransformer(string name, ITransformer transformer);
    
    /// <summary>
    /// Configure the transformer to raise exception or return Null on transformation error
    /// </summary>
    /// <param name="raiseError">true for raise error, false for return Null.</param>
    void SetRaiseTransformationError(bool raiseError);

    /// <summary>
    /// Gets parameter value for the provided key and configuration.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The optional parameter provider configuration.</param>
    /// <param name="transformation">The optional transformation.</param>
    /// <param name="transformerName">The optional transformer name.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter value.</returns>
    Task<T?> GetAsync<T>(string key, ParameterProviderConfiguration? config, Transformation? transformation,
        string? transformerName) where T: class;

    /// <summary>
    /// Gets multiple parameter values for the provided key and configuration.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The optional parameter provider configuration.</param>
    /// <param name="transformation">The optional transformation.</param>
    /// <param name="transformerName">The optional transformer name.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    Task<IDictionary<string, T?>> GetMultipleAsync<T>(string key,
        ParameterProviderConfiguration? config, Transformation? transformation, string? transformerName) where T: class;
}
