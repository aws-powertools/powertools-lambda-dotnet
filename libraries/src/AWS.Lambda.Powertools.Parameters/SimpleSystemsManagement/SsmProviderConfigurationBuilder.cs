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

using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class SsmProviderConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    private bool? _recursive;
    private bool? _withDecryption;

    public SsmProviderConfigurationBuilder(ParameterProviderBase parameterProvider) :
        base(parameterProvider)
    {
    }

    public SsmProviderConfigurationBuilder Recursive()
    {
        _recursive = true;
        return this;
    }

    public SsmProviderConfigurationBuilder WithDecryption()
    {
        _withDecryption = true;
        return this;
    }

    protected override ParameterProviderConfiguration NewConfiguration()
    {
        return new SsmProviderConfiguration
        {
            WithDecryption = _withDecryption,
            Recursive = _recursive
        };
    }
}