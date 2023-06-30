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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.DynamoDB;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace HelloWorld;

/// <summary>
/// A helper to showcase Powertools for AWS Lambda (.NET) parameters utility
/// </summary>
public interface IParameterLookupHelper
{
    /// <summary>
    /// Get a single parameter value from SSM Parameter store
    /// </summary>
    /// <returns>ParameterLookupRecord</returns>
    Task<ParameterLookupRecord> GetSingleParameterWithSsmProvider();
    
    /// <summary>
    /// Get multiple parameter values from SSM Parameter store
    /// </summary>
    /// <returns>ParameterLookupRecord</returns>
    Task<ParameterLookupRecord> GetMultipleParametersWithSsmProvider();

    /// <summary>
    /// Get a single secret value from Secret Manger
    /// </summary>
    /// <returns>ParameterLookupRecord</returns>
    Task<ParameterLookupRecord> GetSingleSecretWithSecretsProvider();

    /// <summary>
    /// Get a single parameter value from DynamoDB table
    /// </summary>
    /// <returns>ParameterLookupRecord</returns>
    Task<ParameterLookupRecord> GetSingleParameterWithDynamoDBProvider();

    /// <summary>
    /// Get a multiple parameter values from DynamoDB table
    /// </summary>
    /// <returns>ParameterLookupRecord</returns>
    Task<ParameterLookupRecord> GetMultipleParametersWithDynamoDBProvider();
}

/// <summary>
/// A helper to showcase Powertools for AWS Lambda (.NET) parameters utility
/// </summary>
public class ParameterLookupHelper : IParameterLookupHelper
{
    /// <summary>
    /// Current parameter provider instance
    /// </summary>
    private IParameterProvider? _currentProvider;

    /// <summary>
    /// Current parameter provider instance type
    /// </summary>
    private ParameterProviderType _currentProviderType = ParameterProviderType.None;

    /// <summary>
    /// Get or create an instance of provider for the specified provider type
    /// </summary>
    /// <param name="providerType">The specified provider type</param>
    /// <returns>An instance of ParameterProvider</returns>
    private IParameterProvider GetParameterProvider(ParameterProviderType providerType)
    {
        if (_currentProvider is not null && providerType == _currentProviderType)
            return _currentProvider!;

        _currentProviderType = providerType;
        _currentProvider = providerType switch
        {
            ParameterProviderType.SsmProvider => ParametersManager.SsmProvider,
            ParameterProviderType.SecretsProvider => ParametersManager.SecretsProvider,
            ParameterProviderType.DynamoDBProvider => ParametersManager.DynamoDBProvider,
            _ => _currentProvider
        };

        return _currentProvider!;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public ParameterLookupHelper()
    {

    }

    /// <summary>
    /// Test constructor
    /// </summary>
    public ParameterLookupHelper(IParameterProvider currentProvider, ParameterProviderType currentProviderType)
    {
        _currentProvider = currentProvider;
        _currentProviderType = currentProviderType;
    }

    /// <inheritdoc />
    public async Task<ParameterLookupRecord> GetSingleParameterWithSsmProvider()
    {
        var parameterProviderType = ParameterProviderType.SsmProvider;

        // Get SSM Provider instance
        IParameterProvider ssmProvider = GetParameterProvider(parameterProviderType);

        // Get SSM parameter name
        string parameterName =
            Environment.GetEnvironmentVariable(EnvironmentVariableNames.SsmSingleParameterNameVariableName) ?? "";

        // Retrieve a single parameter
        string? value = await ssmProvider
            .GetAsync(parameterName)
            .ConfigureAwait(false);

        return new ParameterLookupRecord
        {
            Provider = parameterProviderType,
            Method = ParameterLookupMethod.Get,
            Key = parameterName,
            Value = value
        };
    }

    /// <inheritdoc />
    public async Task<ParameterLookupRecord> GetMultipleParametersWithSsmProvider()
    {
        var parameterProviderType = ParameterProviderType.SsmProvider;

        // Get SSM Provider instance
        IParameterProvider ssmProvider = GetParameterProvider(parameterProviderType);

        // Get SSM parameter path prefix
        string parameterPathPrefix =
            Environment.GetEnvironmentVariable(EnvironmentVariableNames.SsmMultipleParametersPathPrefixVariableName) ??
            "";

        // Retrieve multiple parameters from a path prefix
        // This returns a Dictionary with the parameter name as key
        IDictionary<string, string?> values = await ssmProvider
            .GetMultipleAsync(parameterPathPrefix)
            .ConfigureAwait(false);

        return new ParameterLookupRecord
        {
            Provider = parameterProviderType,
            Method = ParameterLookupMethod.GetMultiple,
            Key = parameterPathPrefix,
            Value = values
        };
    }

    /// <inheritdoc />
    public async Task<ParameterLookupRecord> GetSingleSecretWithSecretsProvider()
    {
        var parameterProviderType = ParameterProviderType.SecretsProvider;

        // Get SSM Provider instance
        IParameterProvider secretsProvider = GetParameterProvider(parameterProviderType);

        // Get SSM parameter name
        string secretName = Environment.GetEnvironmentVariable(EnvironmentVariableNames.SecretsManagerSecretName) ?? "";

        // Retrieve a single parameter
        var value = await secretsProvider
            .WithTransformation(Transformation.Json)
            .GetAsync<SecretRecord>(secretName)
            .ConfigureAwait(false);

        return new ParameterLookupRecord
        {
            Provider = parameterProviderType,
            Method = ParameterLookupMethod.Get,
            Key = secretName,
            Value = value
        };
    }

    /// <inheritdoc />
    public async Task<ParameterLookupRecord> GetSingleParameterWithDynamoDBProvider()
    {
        var parameterProviderType = ParameterProviderType.DynamoDBProvider;

        // Get DynamoDB partition key  
        string tableName =
            Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBSingleParameterTableName) ?? "";

        // Get DynamoDB Provider instance
        IParameterProvider dynamoDbProvider = ((IDynamoDBProvider)GetParameterProvider(parameterProviderType))
            .UseTable(tableName);

        // Get DynamoDB partition key  
        string hashKey = Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBSingleParameterId) ?? "";

        // Retrieve a single parameter
        string? value = await dynamoDbProvider
            .GetAsync(hashKey)
            .ConfigureAwait(false);

        return new ParameterLookupRecord
        {
            Provider = parameterProviderType,
            Method = ParameterLookupMethod.Get,
            Key = hashKey,
            Value = value
        };
    }

    /// <inheritdoc />
    public async Task<ParameterLookupRecord> GetMultipleParametersWithDynamoDBProvider()
    {
        var parameterProviderType = ParameterProviderType.DynamoDBProvider;

        // Get DynamoDB partition key  
        string tableName =
            Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBMultipleParametersTableName) ?? "";

        // Get DynamoDB Provider instance
        IParameterProvider dynamoDbProvider = ((IDynamoDBProvider)GetParameterProvider(parameterProviderType))
            .UseTable(tableName);

        // Get DynamoDB partition key  
        string hashKey =
            Environment.GetEnvironmentVariable(EnvironmentVariableNames.DynamoDBMultipleParametersParameterId) ?? "";

        // Retrieve multiple parameters
        IDictionary<string, string?> values = await dynamoDbProvider
            .GetMultipleAsync(hashKey)
            .ConfigureAwait(false);

        return new ParameterLookupRecord
        {
            Provider = ParameterProviderType.DynamoDBProvider,
            Method = ParameterLookupMethod.GetMultiple,
            Key = hashKey,
            Value = values
        };
    }
}