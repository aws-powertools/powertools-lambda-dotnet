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

public interface IAmazonConfigurableClient<out TProvider, in TClient, in TConfig>
{
    TProvider UseClient(TClient client);
    
    TProvider ConfigureClient(RegionEndpoint region);

    TProvider ConfigureClient(TConfig config);

    TProvider ConfigureClient(AWSCredentials credentials);

    TProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region);

    TProvider ConfigureClient(AWSCredentials credentials, TConfig config);

    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey);

    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region);

    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, TConfig config);

    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken);

    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, RegionEndpoint region);

    TProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, TConfig config);
}
