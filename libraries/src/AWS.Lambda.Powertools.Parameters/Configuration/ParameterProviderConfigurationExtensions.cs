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

public static class ParameterProviderConfigurationExtensions
{
    public static TConfigurationBuilder ForceFetch<TConfigurationBuilder>(this TConfigurationBuilder instance)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        instance.SetForceFetch(true);
        return instance;
    }

    public static TConfigurationBuilder WithMaxAge<TConfigurationBuilder>(this TConfigurationBuilder instance,
        TimeSpan maxAge)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        instance.SetMaxAge(maxAge);
        return instance;
    }

    public static TConfigurationBuilder WithTransformation<TConfigurationBuilder>(this TConfigurationBuilder instance,
        Transformation transformation)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        instance.SetTransformation(transformation);
        return instance;
    }

    public static TConfigurationBuilder WithTransformation<TConfigurationBuilder>(this TConfigurationBuilder instance,
        ITransformer transformer)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        instance.SetTransformer(transformer);
        return instance;
    }

    public static TConfigurationBuilder WithTransformation<TConfigurationBuilder>(this TConfigurationBuilder instance,
        string transformerName)
        where TConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        instance.SetTransformerName(transformerName);
        return instance;
    }
}