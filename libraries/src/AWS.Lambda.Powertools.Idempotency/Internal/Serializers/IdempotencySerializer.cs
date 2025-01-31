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
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using AWS.Lambda.Powertools.Common.Utils;

namespace AWS.Lambda.Powertools.Idempotency.Internal.Serializers;

/// <summary>
/// Serializer for Idempotency.
/// </summary>
internal static class IdempotencySerializer
{
    private static JsonSerializerOptions _jsonOptions;

    static IdempotencySerializer()
    {
        BuildDefaultOptions();
    }

    /// <summary>
    /// Builds the default JsonSerializerOptions.
    /// </summary>
    private static void BuildDefaultOptions()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
#if NET8_0_OR_GREATER
        if (!RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
            _jsonOptions.TypeInfoResolverChain.Add(IdempotencySerializationContext.Default);
        }
#endif
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Adds a JsonTypeInfoResolver to the JsonSerializerOptions.
    /// </summary>
    /// <param name="context">The JsonTypeInfoResolver to add.</param>
    /// <remarks>
    /// This method is only available in .NET 8.0 and later versions.
    /// </remarks>
    internal static void AddTypeInfoResolver(JsonSerializerContext context)
    {
        BuildDefaultOptions();
        _jsonOptions.TypeInfoResolverChain.Add(context);
    }

    /// <summary>
    /// Gets the JsonTypeInfo for a given type.
    /// </summary>
    /// <param name="type">The type to get information for.</param>
    /// <returns>The JsonTypeInfo for the specified type, or null if not found.</returns>
    internal static JsonTypeInfo GetTypeInfo(Type type)
    {
        var typeInfo = _jsonOptions.TypeInfoResolver?.GetTypeInfo(type, _jsonOptions);
        if (typeInfo == null)
        {
            throw new SerializationException(
                $"Type {type} is not known to the serializer. Ensure it's included in the JsonSerializerContext.");
        }

        return typeInfo;
    }
#endif

    /// <summary>
    /// Serializes the specified object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="inputType">The type of the object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    internal static string Serialize(object value, Type inputType)
    {
#if NET6_0
        return JsonSerializer.Serialize(value, _jsonOptions);
#else
        if (RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable
            return JsonSerializer.Serialize(value, _jsonOptions);
        }

        return JsonSerializer.Serialize(value, GetTypeInfo(inputType));
#endif
    }

    /// <summary>
    /// Deserializes the specified JSON string to an object of type T.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="value">The JSON string to deserialize.</param>
    /// <returns>An object of type T represented by the JSON string.</returns>
    internal static T Deserialize<T>(string value)
    {
#if NET6_0
        return JsonSerializer.Deserialize<T>(value,_jsonOptions);
#else
        if (RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable
            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }

        return (T)JsonSerializer.Deserialize(value, GetTypeInfo(typeof(T)));
#endif
    }
}