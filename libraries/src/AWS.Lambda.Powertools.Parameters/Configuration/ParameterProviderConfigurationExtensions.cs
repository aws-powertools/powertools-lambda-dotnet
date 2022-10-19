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

namespace AWS.Lambda.Powertools.Parameters.Configuration;

/// <summary>
/// ParameterProviderConfigurationExtensions class extensions.
/// </summary>
public static class ParameterProviderConfigurationExtensions
{
    /// <summary>
    /// Forces provider to fetch the latest value from the store regardless if already available in cache.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <typeparam name="TConfigurationBuilder">The configuration builder type.</typeparam>
    /// <returns>The configuration builder instance.</returns>
    public static TConfigurationBuilder ForceFetch<TConfigurationBuilder>(this TConfigurationBuilder builder)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        builder.SetForceFetch(true);
        return builder;
    }

    /// <summary>
    /// Set the cache maximum age.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="maxAge">The maximum cache age</param>
    /// <typeparam name="TConfigurationBuilder">The configuration builder type.</typeparam>
    /// <returns>The configuration builder instance.</returns>
    public static TConfigurationBuilder WithMaxAge<TConfigurationBuilder>(this TConfigurationBuilder builder,
        TimeSpan maxAge)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        builder.SetMaxAge(maxAge);
        return builder;
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="transformation">The transformation type.</param>
    /// <typeparam name="TConfigurationBuilder">The configuration builder type.</typeparam>
    /// <returns>The configuration builder instance.</returns>
    public static TConfigurationBuilder WithTransformation<TConfigurationBuilder>(this TConfigurationBuilder builder,
        Transformation transformation)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        builder.SetTransformation(transformation);
        return builder;
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="transformer">The instance of the transformer.</param>
    /// <typeparam name="TConfigurationBuilder">The configuration builder type.</typeparam>
    /// <returns>The configuration builder instance.</returns>
    public static TConfigurationBuilder WithTransformation<TConfigurationBuilder>(this TConfigurationBuilder builder,
        ITransformer transformer)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        builder.SetTransformer(transformer);
        return builder;
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="transformerName">The name of the registered transformer.</param>
    /// <typeparam name="TConfigurationBuilder">The configuration builder type.</typeparam>
    /// <returns>The configuration builder instance.</returns>
    public static TConfigurationBuilder WithTransformation<TConfigurationBuilder>(this TConfigurationBuilder builder,
        string transformerName)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        builder.SetTransformerName(transformerName);
        return builder;
    }
}