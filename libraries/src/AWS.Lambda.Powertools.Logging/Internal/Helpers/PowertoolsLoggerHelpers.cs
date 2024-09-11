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
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Logging.Internal.Helpers;

/// <summary>
/// Class PowertoolsLoggerHelpers.
/// </summary>
internal static class PowertoolsLoggerHelpers
{
    /// <summary>
    /// Converts an object to a dictionary.
    /// </summary>
    /// <param name="anonymousObject">The object to convert.</param>
    /// <returns>
    /// If the object has a namespace, returns the object as-is.
    /// Otherwise, returns a dictionary representation of the object's properties.
    /// </returns>
    internal static object ObjectToDictionary(object anonymousObject)
    {
        if (anonymousObject.GetType().Namespace is not null)
        {
            return anonymousObject;
        }

        return anonymousObject.GetType().GetProperties()
            .ToDictionary(prop => prop.Name, prop => ObjectToDictionary(prop.GetValue(anonymousObject, null)));
    }
    
    /// <summary>
    /// Converts the input string to the configured output case.
    /// </summary>
    /// <param name="correlationIdPath">The string to convert.</param>
    /// <returns>
    /// The input string converted to the configured case (camel, pascal, or snake case).
    /// </returns>
    internal static string ConvertToOutputCase(string correlationIdPath)
    {
        return PowertoolsConfigurations.Instance.GetLoggerOutputCase() switch
        {
            LoggerOutputCase.CamelCase => ToCamelCase(correlationIdPath),
            LoggerOutputCase.PascalCase => ToPascalCase(correlationIdPath),
            _ => ToSnakeCase(correlationIdPath), // default snake_case
        };
    }

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    /// <param name="correlationIdPath">The string to convert.</param>
    /// <returns>The input string converted to snake_case.</returns>
    private static string ToSnakeCase(string correlationIdPath)
    {
        return string.Concat(correlationIdPath.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString()))
            .ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="correlationIdPath">The string to convert.</param>
    /// <returns>The input string converted to PascalCase.</returns>
    private static string ToPascalCase(string correlationIdPath)
    {
        return char.ToUpperInvariant(correlationIdPath[0]) + correlationIdPath.Substring(1);
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    /// <param name="correlationIdPath">The string to convert.</param>
    /// <returns>The input string converted to camelCase.</returns>
    private static string ToCamelCase(string correlationIdPath)
    {
        return char.ToLowerInvariant(correlationIdPath[0]) + correlationIdPath.Substring(1);
    }
}