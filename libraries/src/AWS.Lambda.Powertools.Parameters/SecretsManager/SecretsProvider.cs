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

using System.Net.Mime;
using System.Text;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.SecretsManager;

public class SecretsProvider : ParameterProvider
{
    private const string CurrentVersionStage = "AWSCURRENT";
    
    private IAmazonSecretsManager? _client;
    private IAmazonSecretsManager Client => _client ??= new AmazonSecretsManagerClient();
    
    public SecretsProvider UseClient(IAmazonSecretsManager client)
    {
        _client = client;
        return this;
    }

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

    protected override Task<IDictionary<string, string>> GetMultipleAsync(string path, ParameterProviderConfiguration? config)
    {
        throw new NotSupportedException("Impossible to get multiple values from AWS Secrets Manager");
    }
}