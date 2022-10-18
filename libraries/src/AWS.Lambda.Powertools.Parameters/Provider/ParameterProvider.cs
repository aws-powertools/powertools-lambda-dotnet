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
using AWS.Lambda.Powertools.Parameters.Configuration;

namespace AWS.Lambda.Powertools.Parameters.Provider;

/// <summary>
/// Provide a generic view of a parameter provider. This is an abstract class.  
/// </summary>
public abstract class ParameterProvider : ParameterProvider<ParameterProviderConfigurationBuilder>
{
    /// <summary>
    /// Creates a new instance of ParameterProviderConfigurationBuilder.
    /// </summary>
    /// <returns>A new instance of ParameterProviderConfigurationBuilder.</returns>
    protected override ParameterProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new ParameterProviderConfigurationBuilder(this);
    }
}

/// <summary>
/// Provide a generic view of a parameter provider. This is an abstract class.  
/// </summary>
/// <typeparam name="TConfigurationBuilder">Type of the ConfigurationBuilder for the parameter provider.</typeparam>
public abstract class ParameterProvider<TConfigurationBuilder> : ParameterProviderBase
    where TConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    /// <summary>
    /// Creates a new instance of the specified type ConfigurationBuilder.
    /// </summary>
    /// <returns>A new instance of ConfigurationBuilder.</returns>
    protected abstract TConfigurationBuilder NewConfigurationBuilder();

    /// <summary>
    /// Creates and configures a new instance of the specified type ConfigurationBuilder.
    /// </summary>
    /// <returns>A new instance of ConfigurationBuilder.</returns>
    private TConfigurationBuilder CreateConfigurationBuilder()
    {
        var configBuilder = NewConfigurationBuilder();
        var maxAge = Handler.GetDefaultMaxAge();
        if (maxAge is not null)
            configBuilder = configBuilder.WithMaxAge(maxAge.Value);
        return configBuilder;
    }

    /// <summary>
    /// Set the cache maximum age.
    /// </summary>
    /// <param name="maxAge"></param>
    /// <returns>Provider Configuration Builder instance</returns>
    public TConfigurationBuilder WithMaxAge(TimeSpan maxAge)
    {
        return CreateConfigurationBuilder().WithMaxAge(maxAge);
    }

    /// <summary>
    /// Forces provider to fetch the latest value from the store regardless if already available in cache.
    /// </summary>
    /// <returns>Provider Configuration Builder instance</returns>
    public TConfigurationBuilder ForceFetch()
    {
        return CreateConfigurationBuilder().ForceFetch();
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformation">The transformation type.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    public TConfigurationBuilder WithTransformation(Transformation transformation)
    {
        return CreateConfigurationBuilder().WithTransformation(transformation);
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformer">The instance of the transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    public TConfigurationBuilder WithTransformation(ITransformer transformer)
    {
        return CreateConfigurationBuilder().WithTransformation(transformer);
    }

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformerName">The name of the registered transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    public TConfigurationBuilder WithTransformation(string transformerName)
    {
        return CreateConfigurationBuilder().WithTransformation(transformerName);
    }
}