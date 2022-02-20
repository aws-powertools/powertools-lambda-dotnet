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

using System.Linq;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
/// Class StringUtils
/// </summary>
public static class StringUtils
{
    /// <summary>
    /// Extension method to convert string to snake case
    /// </summary>
    /// <param name="str">string</param>
    /// <returns>Snake case formatted string</returns>
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }

    /// <summary>
    /// Extension method to convert string to pascal case
    /// </summary>
    /// <param name="str">string</param>
    /// <returns>Pascal case formatted string</returns>
    public static string ToPascalCase(this string str)
    {
        return string.Concat(char.ToUpper(str[0]), str.Substring(1));
    }
}