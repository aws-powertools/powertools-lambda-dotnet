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

/// <summary>
/// Provider to retrieve parameter values from AWS Systems Manager Parameter Store.
/// </summary>
public class SsmProvider : ParameterProvider<SsmProviderConfigurationBuilder>, ISsmProvider
{
    #region IParameterProviderConfigurableClient implementation
    
    /// <summary>
    /// The client instance.
    /// </summary>
    private IAmazonSimpleSystemsManagement? _client;

    /// <summary>
    /// Gets the client instance.
    /// </summary>
    private IAmazonSimpleSystemsManagement Client => _client ??= new AmazonSimpleSystemsManagementClient();

    /// <summary>
    /// Use a custom client
    /// </summary>
    /// <param name="client">The custom client</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider UseClient(IAmazonSimpleSystemsManagement client)
    {
        _client = client;
        return this;
    }

    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(region);
        return this;
    }

    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(config);
        return this;
    }

    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(AWSCredentials credentials)
    {
        _client = new AmazonSimpleSystemsManagementClient(credentials);
        return this;
    }

    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(credentials, region);
        return this;
    }

    /// <summary>
    /// Configure client with AWS credentials and a client configuration object.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(AWSCredentials credentials, AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(credentials, config);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, region);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, config);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region);
        return this;
    }

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public ISsmProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonSimpleSystemsManagementConfig config)
    {
        _client = new AmazonSimpleSystemsManagementClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config);
        return this;
    }
    
    #endregion

    /// <summary>
    /// Automatically decrypt the parameter.
    /// </summary>
    /// <returns>The provider configuration builder.</returns>
    public SsmProviderConfigurationBuilder WithDecryption()
    {
        return NewConfigurationBuilder().WithDecryption();
    }

    /// <summary>
    /// Fetches all parameter values recursively based on a path prefix.
    /// For GetMultiple() only.
    /// </summary>
    /// <returns>The provider configuration builder.</returns>
    public SsmProviderConfigurationBuilder Recursive()
    {
        return NewConfigurationBuilder().Recursive();
    }

    /// <summary>
    /// Creates and configures a new instance of SsmProviderConfigurationBuilder.
    /// </summary>
    /// <returns>A new instance of SsmProviderConfigurationBuilder.</returns>
    protected override SsmProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new SsmProviderConfigurationBuilder(this);
    }

    /// <summary>
    /// Get parameter value for the provided key. 
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>The parameter value.</returns>
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

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string key,
        ParameterProviderConfiguration? config)
    {
        var configuration = config as SsmProviderConfiguration;
        var retValues = new Dictionary<string, string?>();

        string? nextToken = default;
        do
        {
            // Query AWS Parameter Store
            var response = await Client.GetParametersByPathAsync(
                new GetParametersByPathRequest
                {
                    Path = key,
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