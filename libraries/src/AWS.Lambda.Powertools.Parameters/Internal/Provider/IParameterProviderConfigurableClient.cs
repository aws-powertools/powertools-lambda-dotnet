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

namespace AWS.Lambda.Powertools.Parameters.Internal.Provider;

/// <summary>
/// Represents a type that has an internal configurable client to retrieve data from AWS services.
/// </summary>
public interface IParameterProviderConfigurableClient<out TProvider, in TClient, in TConfig>
{
    /// <summary>
    /// Use a custom client
    /// </summary>
    /// <param name="client">The custom client</param>
    /// <returns>Provider instance</returns>
    TProvider UseClient(TClient client);
    
    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(RegionEndpoint region);

    /// <summary>
    /// Configure client with the credentials loaded from the application's default configuration.
    /// </summary>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(TConfig config);

    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(AWSCredentials credentials);

    /// <summary>
    /// Configure client with AWS credentials.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region);

    /// <summary>
    /// Configure client with AWS credentials and a client configuration object.
    /// </summary>
    /// <param name="credentials">AWS credentials.</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(AWSCredentials credentials, TConfig config);

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey);

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region);

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, TConfig config);

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken);

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key. 
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="region">The region to connect.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, RegionEndpoint region);

    /// <summary>
    /// Configure client with AWS Access Key ID and AWS Secret Key and a client configuration object.
    /// </summary>
    /// <param name="awsAccessKeyId">AWS Access Key ID</param>
    /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
    /// <param name="awsSessionToken">AWS Session Token</param>
    /// <param name="config">The client configuration object.</param>
    /// <returns>Provider instance</returns>
    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, TConfig config);
}
