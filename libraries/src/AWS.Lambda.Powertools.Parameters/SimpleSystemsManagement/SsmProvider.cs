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

using Amazon;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Internal.SimpleSystemsManagement;

namespace AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

public class SsmProvider : ParameterProvider<SsmProviderConfigurationBuilder>, ISsmProvider
{
    private IAmazonSimpleSystemsManagement? _client;

    private IAmazonSimpleSystemsManagement Client => _client ??= new AmazonSimpleSystemsManagementClient();

    public ISsmProvider UseClient(IAmazonSimpleSystemsManagement client)
    {
        _client = client;
        return this;
    }

    public ISsmProvider ConfigureClient(RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(region);
        return this;
    }

    public ISsmProvider ConfigureClient(AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(config);
        return this;
    }

    public ISsmProvider ConfigureClient(AWSCredentials credentials)
    {
        _client = new AmazonSimpleSystemsManagementClient(credentials);
        return this;
    }

    public ISsmProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(credentials, region);
        return this;
    }

    public ISsmProvider ConfigureClient(AWSCredentials credentials, AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(credentials, config);
        return this;
    }

    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey);
        return this;
    }

    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, region);
        return this;
    }

    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, config);
        return this;
    }

    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken);
        return this;
    }

    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region);
        return this;
    }

    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config);
        return this;
    }

    public SsmProviderConfigurationBuilder WithDecryption()
    {
        return NewConfigurationBuilder().WithDecryption();
    }

    public SsmProviderConfigurationBuilder Recursive()
    {
        return NewConfigurationBuilder().Recursive();
    }

    protected override SsmProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new SsmProviderConfigurationBuilder(this);
    }

    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        var configuration = config as SsmProviderConfiguration;
        var response = await Client.GetParameterAsync(
            new GetParameterRequest
            {
                Name = key,
                WithDecryption = (configuration?.WithDecryption).GetValueOrDefault()
            }).ConfigureAwait(false);

        return response?.Parameter?.Value;
    }

    protected override async Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config)
    {
        var configuration = config as SsmProviderConfiguration;
        var retValues = new Dictionary<string, string>();

        string? nextToken = default;
        do
        {
            // Query AWS Parameter Store
            var response = await Client.GetParametersByPathAsync(
                new GetParametersByPathRequest
                {
                    Path = path,
                    WithDecryption = (configuration?.WithDecryption).GetValueOrDefault(),
                    Recursive = (configuration?.Recursive).GetValueOrDefault(),
                    NextToken = nextToken
                }).ConfigureAwait(false);

            var maxAge = GetMaxAge(config);
            foreach (var parameter in response.Parameters)
            {
                if (retValues.TryAdd(parameter.Name, parameter.Value))
                    Cache.Set(parameter.Name, parameter.Value, maxAge);
            }

            // Possibly get more
            nextToken = response.NextToken;
        } while (!string.IsNullOrEmpty(nextToken));

        return retValues;
    }
}