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

public abstract class ParameterProvider : ParameterProvider<ParameterProviderConfigurationBuilder>
{
    protected override ParameterProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new ParameterProviderConfigurationBuilder(this);
    }
}

public abstract class ParameterProvider<TConfigurationBuilder> : ParameterProviderBase
    where TConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    protected abstract TConfigurationBuilder NewConfigurationBuilder();

    private TConfigurationBuilder CreateConfigurationBuilder()
    {
        var configBuilder = NewConfigurationBuilder();
        var maxAge = Handler.GetDefaultMaxAge();
        if (maxAge is not null)
            configBuilder = configBuilder.WithMaxAge(maxAge.Value);
        return configBuilder;
    }

    public TConfigurationBuilder WithMaxAge(TimeSpan age)
    {
        return CreateConfigurationBuilder().WithMaxAge(age);
    }

    public TConfigurationBuilder ForceFetch()
    {
        return CreateConfigurationBuilder().ForceFetch();
    }

    public TConfigurationBuilder WithTransformation(Transformation transformation)
    {
        return CreateConfigurationBuilder().WithTransformation(transformation);
    }

    public TConfigurationBuilder WithTransformation(ITransformer transformer)
    {
        return CreateConfigurationBuilder().WithTransformation(transformer);
    }

    public TConfigurationBuilder WithTransformation(string transformerName)
    {
        return CreateConfigurationBuilder().WithTransformation(transformerName);
    }
}