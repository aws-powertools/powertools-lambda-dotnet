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

using AWS.Lambda.Powertools.Parameters.Internal.AppConfig;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

public class AppConfigProviderConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    private string? _environmentId;
    private string? _applicationId;
    private string? _configProfileId;

    public AppConfigProviderConfigurationBuilder(ParameterProviderBase parameterProvider) :
        base(parameterProvider)
    {
    }

    public AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId)
    {
        _environmentId = environmentId;
        return this;
    }

    public AppConfigProviderConfigurationBuilder WithApplication(string applicationId)
    {
        _applicationId = applicationId;
        return this;
    }

    public AppConfigProviderConfigurationBuilder WithConfigProfile(string configProfileId)
    {
        _configProfileId = configProfileId;
        return this;
    }
    
    protected override ParameterProviderConfiguration NewConfiguration()
    {
        return new AppConfigProviderConfiguration
        {
            EnvironmentId = _environmentId,
            ApplicationId = _applicationId,
            ConfigProfileId = _configProfileId
        };
    }

    public IDictionary<string, string> Get()
    {
        return GetAsync().GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, string>> GetAsync()
    {
        return await GetMultipleAsync(
                AppConfigProviderCacheHelper.GetCacheKey(_applicationId, _environmentId,
                    _configProfileId))
            .ConfigureAwait(false);
    }
}