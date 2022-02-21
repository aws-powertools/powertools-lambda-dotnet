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


using System.Text.Json;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
/// Class PascalCaseNamingPolicy
/// Implements the <see cref="System.Text.Json.JsonNamingPolicy" />
/// </summary>
public class PascalCaseNamingPolicy : JsonNamingPolicy
{
    /// <summary>
    /// Instance of PascalCaseNamingPolicy
    /// </summary>
    public static PascalCaseNamingPolicy Instance { get; } = new PascalCaseNamingPolicy();


    /// <summary>
    /// Converts key to pascal case
    /// </summary>
    /// <param name="name">Name of key</param>
    /// <returns></returns>
    public override string ConvertName(string name)
    {
        return name.ToPascalCase();
    }
}