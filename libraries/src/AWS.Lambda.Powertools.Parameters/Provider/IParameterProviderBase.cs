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

namespace AWS.Lambda.Powertools.Parameters.Provider;

public interface IParameterProviderBase
{
    string? Get(string key);
    
    Task<string?> GetAsync(string key);

    T? Get<T>(string key) where T : class;

    Task<T?> GetAsync<T>(string key) where T : class;

    IDictionary<string, string?> GetMultiple(string key);

    Task<IDictionary<string, string?>> GetMultipleAsync(string key);
    
    IDictionary<string, T?> GetMultiple<T>(string key) where T : class;

    Task<IDictionary<string, T?>> GetMultipleAsync<T>(string key) where T : class;
}