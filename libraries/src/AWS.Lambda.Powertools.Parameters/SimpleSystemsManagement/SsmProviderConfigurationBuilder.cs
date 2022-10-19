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
using AWS.Lambda.Powertools.Parameters.Internal.SimpleSystemsManagement;

namespace AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class SsmProviderConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    /// <summary>
    /// Fetches the latest value from the store regardless if already available in cache.
    /// </summary>
    private bool? _recursive;
    
    /// <summary>
    /// Fetches the latest value from the store regardless if already available in cache.
    /// </summary>
    private bool? _withDecryption;

    /// <summary>
    /// SsmProviderConfigurationBuilder constructor.
    /// </summary>
    /// <param name="parameterProvider"></param>
    public SsmProviderConfigurationBuilder(ParameterProviderBase parameterProvider) :
        base(parameterProvider)
    {
    }

    /// <summary>
    /// Automatically decrypt the parameter.
    /// </summary>
    /// <returns>The provider configuration builder.</returns>
    public SsmProviderConfigurationBuilder WithDecryption()
    {
        _withDecryption = true;
        return this;
    }
    
    /// <summary>
    /// Fetches all parameter values recursively based on a path prefix.
    /// For GetMultiple() only.
    /// </summary>
    /// <returns>The provider configuration builder.</returns>
    public SsmProviderConfigurationBuilder Recursive()
    {
        _recursive = true;
        return this;
    }

    /// <summary>
    /// Creates and configures a new instance of SsmProviderConfiguration.
    /// </summary>
    /// <returns>A new instance of SsmProviderConfiguration.</returns>
    protected override ParameterProviderConfiguration NewConfiguration()
    {
        return new SsmProviderConfiguration
        {
            WithDecryption = _withDecryption,
            Recursive = _recursive
        };
    }
}