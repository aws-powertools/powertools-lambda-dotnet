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
using System.Text.Json.Serialization;

namespace HelloWorld;

/// <summary>
/// EnvironmentVariable names
/// </summary>
public static class EnvironmentVariableNames
{
    public const string SsmSingleParameterNameVariableName = "SSM_SINGLE_PARAM_NAME";
    public const string SsmMultipleParametersPathPrefixVariableName = "SSM_MULTI_PARAM_PREFIX";
    public const string SecretsManagerSecretName = "SECRET_NAME";
    public const string DynamoDBSingleParameterTableName = "DYNAMO_SINGLE_PARAM_TABLE_NAME";
    public const string DynamoDBMultipleParametersTableName = "DYNAMO_MULTI_PARAM_TABLE_NAME";
    public const string DynamoDBSingleParameterId = "DYNAMO_SINGLE_PARAM_ID";
    public const string DynamoDBMultipleParametersParameterId = "DYNAMO_MULTI_PARAM_ID";
}

/// <summary>
/// Parameter provider types
/// </summary>
public enum ParameterProviderType
{
    None = 0,
    SsmProvider = 1,
    SecretsProvider =2,
    DynamoDBProvider = 3,
}

/// <summary>
/// Specifies the lookup method
/// </summary>
public enum ParameterLookupMethod
{
    Get = 0,
    GetMultiple = 1
}


/// <summary>
/// Record to represent the data structure of Parameter Lookup
/// </summary>
[Serializable]
public class ParameterLookupRecord
{
    /// <summary>
    /// Provider type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ParameterProviderType Provider { get; set; }
    
    /// <summary>
    /// Lookup method Get/GetMultiple
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ParameterLookupMethod Method { get; set; }
    
    /// <summary>
    /// Parameter Key
    /// </summary>
    public string? Key { get; set; }
    
    /// <summary>
    /// Parameter value
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Record to represent JSON secret object
/// </summary>
[Serializable]
public class SecretRecord
{
    /// <summary>
    /// Secret Username
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// Secret password
    /// </summary>
    public string? Password { get; set; }
}