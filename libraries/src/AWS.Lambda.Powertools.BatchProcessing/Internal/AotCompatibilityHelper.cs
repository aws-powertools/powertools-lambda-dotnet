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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

/// <summary>
/// Helper class for AOT (Ahead-of-Time) compilation compatibility features.
/// </summary>
internal static class AotCompatibilityHelper
{
    /// <summary>
    /// Determines if the current runtime environment is AOT compiled.
    /// </summary>
    /// <returns>True if running in AOT mode, false otherwise.</returns>
    public static bool IsAotMode()
    {
        // In AOT mode, RuntimeFeature.IsDynamicCodeSupported returns false
        return !RuntimeFeature.IsDynamicCodeSupported;
    }

    /// <summary>
    /// Validates that the JsonSerializerContext contains type information for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <param name="context">The JsonSerializerContext to validate.</param>
    /// <param name="throwOnMissing">Whether to throw an exception if type information is missing.</param>
    /// <returns>True if type information is available, false otherwise.</returns>
    /// <exception cref="AotTypeValidationException">Thrown when type information is missing and throwOnMissing is true.</exception>
    public static bool ValidateTypeInContext<T>(JsonSerializerContext context, bool throwOnMissing = true)
    {
        if (context == null)
        {
            if (throwOnMissing)
            {
                throw new AotTypeValidationException(typeof(T), "JsonSerializerContext is null. AOT compilation requires a JsonSerializerContext with type information.");
            }
            return false;
        }

        try
        {
            var typeInfo = context.GetTypeInfo(typeof(T));
            if (typeInfo == null)
            {
                if (throwOnMissing)
                {
                    throw new AotTypeValidationException(typeof(T), 
                        $"Type '{typeof(T).FullName}' is not registered in the provided JsonSerializerContext. " +
                        $"Add [JsonSerializable(typeof({typeof(T).Name}))] to your JsonSerializerContext class.");
                }
                return false;
            }
            return true;
        }
        catch (NotSupportedException ex)
        {
            if (throwOnMissing)
            {
                throw new AotTypeValidationException(typeof(T), 
                    $"Type '{typeof(T).FullName}' is not supported by the provided JsonSerializerContext. " +
                    $"Ensure the type is properly registered with [JsonSerializable(typeof({typeof(T).Name}))].", ex);
            }
            return false;
        }
    }

    /// <summary>
    /// Provides a fallback deserialization strategy when JsonSerializerContext is not available in AOT mode.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="data">The JSON data to deserialize.</param>
    /// <param name="options">The deserialization options.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="AotCompatibilityException">Thrown when AOT mode requires JsonSerializerContext but none is provided.</exception>

    public static T FallbackDeserialize<T>(string data, DeserializationOptions options)
    {
        if (IsAotMode() && options?.JsonSerializerContext == null)
        {
            throw new AotCompatibilityException(typeof(T), 
                "AOT compilation detected but no JsonSerializerContext provided. " +
                "For AOT compatibility, provide a JsonSerializerContext with type information using DeserializationOptions.");
        }

        // Use reflection-based deserialization as fallback
        return System.Text.Json.JsonSerializer.Deserialize<T>(data, options?.JsonSerializerOptions);
    }

    /// <summary>
    /// Gets a user-friendly error message for AOT compatibility issues.
    /// </summary>
    /// <param name="targetType">The type that caused the issue.</param>
    /// <param name="contextProvided">Whether a JsonSerializerContext was provided.</param>
    /// <returns>A descriptive error message with guidance.</returns>
    public static string GetAotCompatibilityErrorMessage(Type targetType, bool contextProvided)
    {
        if (!contextProvided)
        {
            return $"AOT compilation requires a JsonSerializerContext for type '{targetType.FullName}'. " +
                   $"Create a JsonSerializerContext with [JsonSerializable(typeof({targetType.Name}))] and provide it via DeserializationOptions.";
        }

        return $"The provided JsonSerializerContext does not contain type information for '{targetType.FullName}'. " +
               $"Add [JsonSerializable(typeof({targetType.Name}))] to your JsonSerializerContext class.";
    }

    /// <summary>
    /// Validates AOT compatibility for the given type and options.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <param name="options">The deserialization options.</param>
    /// <exception cref="AotCompatibilityException">Thrown when AOT requirements are not met.</exception>
    /// <exception cref="AotTypeValidationException">Thrown when type is not registered in JsonSerializerContext.</exception>
    public static void ValidateAotCompatibility<T>(DeserializationOptions options)
    {
        // If we're in AOT mode and no context is provided, that's an error
        if (IsAotMode() && options?.JsonSerializerContext == null)
        {
            throw new AotCompatibilityException(typeof(T), GetAotCompatibilityErrorMessage(typeof(T), false));
        }

        // If a JsonSerializerContext is provided, always validate the type is registered
        // This provides early validation regardless of runtime mode
        if (options?.JsonSerializerContext != null)
        {
            ValidateTypeInContext<T>(options.JsonSerializerContext, true);
        }
    }
}