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
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Provider;

/// <summary>
/// Class ParameterProviderExtensions.
/// </summary>
public static class ParameterProviderExtensions
{
    /// <summary>
    /// Set the cache maximum age.
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <param name="maxAge">The cache maximum age.</param>
    /// <typeparam name="TProvider">The provider type.</typeparam>
    /// <returns>The provider instance.</returns>
    public static TProvider DefaultMaxAge<TProvider>(this TProvider provider, TimeSpan maxAge)
        where TProvider : IParameterProvider
    {
        ((ParameterProvider)(object)provider).Handler.SetDefaultMaxAge(maxAge);
        return provider;
    }

    /// <summary>
    /// Set the CacheManager instance.
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <param name="cacheManager">The CacheManager instance.</param>
    /// <typeparam name="TProvider">The provider type.</typeparam>
    /// <returns>The provider instance.</returns>
    public static TProvider UseCacheManager<TProvider>(this TProvider provider, ICacheManager cacheManager)
        where TProvider : IParameterProvider
    {
        ((ParameterProvider)(object)provider).Handler.SetCacheManager(cacheManager);
        return provider;
    }

    /// <summary>
    /// Set the TransformerManager instance.
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <param name="transformerManager">The TransformerManager instance.</param>
    /// <typeparam name="TProvider">The provider type.</typeparam>
    /// <returns>The provider instance.</returns>
    public static TProvider UseTransformerManager<TProvider>(this TProvider provider,
        ITransformerManager transformerManager)
        where TProvider : IParameterProvider
    {
        ((ParameterProvider)(object)provider).Handler.SetTransformerManager(transformerManager);
        return provider;
    }

    /// <summary>
    /// Registers a new transformer instance by name.
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <param name="name">The transformer name.</param>
    /// <param name="transformer">The transformer instance.</param>
    /// <typeparam name="TProvider">The provider type.</typeparam>
    /// <returns>The provider instance.</returns>
    public static TProvider AddTransformer<TProvider>(this TProvider provider, string name, ITransformer transformer)
        where TProvider : IParameterProvider
    {
        ((ParameterProvider)(object)provider).Handler.AddCustomTransformer(name, transformer);
        return provider;
    }
    
    /// <summary>
    /// Configure the transformer to raise exception on transformation error
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <typeparam name="TProvider">The provider type.</typeparam>
    /// <returns>The provider instance.</returns>
    public static TProvider RaiseTransformationError<TProvider>(this TProvider provider)
        where TProvider : IParameterProvider
    {
        RaiseTransformationError(provider, true);
        return provider;
    }
    
    /// <summary>
    /// Configure the transformer to raise exception or return Null on transformation error
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <param name="raiseError">true for raise error, false for return Null.</param>
    /// <typeparam name="TProvider">The provider type.</typeparam>
    /// <returns>The provider instance.</returns>
    public static TProvider RaiseTransformationError<TProvider>(this TProvider provider, bool raiseError)
        where TProvider : IParameterProvider
    {
        ((ParameterProvider)(object)provider).Handler.SetRaiseTransformationError(raiseError);
        return provider;
    }
}