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

using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SecretsManager;

/// <summary>
/// Provider to retrieve parameter values from SAWS Secrets Manager.
/// </summary>
public class SecretsProvider : ParameterProvider, ISecretsProvider
{
    /// <summary>
    /// Latest version constant
    /// </summary>
    private const string CurrentVersionStage = "AWSCURRENT";
    
    #region IParameterProviderConfigurableClient implementation

    /// <summary>
    /// The client instance.
    /// </summary>
    private IAmazonSecretsManager? _client;
    
    /// <summary>
    /// Gets the client instance.
    /// </summary>
    private IAmazonSecretsManager Client => _client ??= new AmazonSecretsManagerClient();

    /// <summary>
    /// Use a custom client
    /// </summary>
    /// <param name="client">The custom client</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider UseClient(IAmazonSecretsManager client)
    {
        _client = client;
        return this;
    }
    
    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(RegionEndpoint region)
    {
        _client = new AmazonSecretsManagerClient(region);
        return this;
    }
    
    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(AmazonSecretsManagerConfig config)
    {
        _client = new AmazonSecretsManagerClient(config);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(AWSCredentials credentials)
    {
        _client = new AmazonSecretsManagerClient(credentials);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region)
    {
        _client = new AmazonSecretsManagerClient(credentials, region);
        return this;
    }

    /// <summary>
    /// Configure client with AWS credentials and a client configuration object.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(AWSCredentials credentials, AmazonSecretsManagerConfig config)
    {
        _client = new AmazonSecretsManagerClient(credentials, config);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey)
    {
        _client = new AmazonSecretsManagerClient(awsAccessKeyId, awsSecretAccessKey);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
    {
        _client = new AmazonSecretsManagerClient(awsAccessKeyId, awsSecretAccessKey, region);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, AmazonSecretsManagerConfig config)
    {
        _client = new AmazonSecretsManagerClient(awsAccessKeyId, awsSecretAccessKey, config);
        return this;
    }
    
    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <returns>Provider instance</returns>
    public ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
    {
        _client = new AmazonSecretsManagerClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken);
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
    public ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, RegionEndpoint region)
    {
        _client = new AmazonSecretsManagerClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region);
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
    public ISecretsProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, AmazonSecretsManagerConfig config)
    {
        _client = new AmazonSecretsManagerClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config);
        return this;
    }
    
    #endregion

    /// <summary>
    /// Get parameter value for the provided key. 
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>The parameter value.</returns>
    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        var response = await Client.GetSecretValueAsync(
            new GetSecretValueRequest
            {
                SecretId = key,
                VersionStage = CurrentVersionStage
            }).ConfigureAwait(false);

        if (response.SecretString is not null)
            return response.SecretString;

        var memoryStream = response.SecretBinary;
        var reader = new StreamReader(memoryStream);
        var base64String = await reader.ReadToEndAsync();
        var decodedBinarySecret = Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
        return decodedBinarySecret;
    }

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="config">The parameter provider configuration</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    protected override Task<IDictionary<string, string?>> GetMultipleAsync(string key,
        ParameterProviderConfiguration? config)
    {
        throw new NotSupportedException("Impossible to get multiple values from AWS Secrets Manager");
    }
}