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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Serializers;

/// <summary>
/// Provides serialization functionality for Powertools logging.
/// </summary>
internal static class PowertoolsLoggingSerializer
{
    private static LoggerOutputCase _currentOutputCase;
    private static JsonSerializerOptions _jsonOptions;

    private static readonly ConcurrentBag<JsonSerializerContext> AdditionalContexts =
        new ConcurrentBag<JsonSerializerContext>();

    /// <summary>
    /// Gets the JsonSerializerOptions instance.
    /// </summary>
    internal static JsonSerializerOptions GetSerializerOptions()
    {
        return _jsonOptions ?? BuildJsonSerializerOptions();
    }

    /// <summary>
    /// Configures the naming policy for the serializer.
    /// </summary>
    /// <param name="loggerOutputCase">The case to use for serialization.</param>
    internal static void ConfigureNamingPolicy(LoggerOutputCase loggerOutputCase)
    {
        _currentOutputCase = loggerOutputCase;
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="inputType">The type of the object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input type is not known to the serializer.</exception>
    internal static string Serialize(object value, Type inputType)
    {
#if NET6_0
        var options = GetSerializerOptions();
        return JsonSerializer.Serialize(value, options);
#else
        if (RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
            var options = GetSerializerOptions();
#pragma warning disable
            return JsonSerializer.Serialize(value, options);
        }

        var typeInfo = GetTypeInfo(inputType);
        if (typeInfo == null)
        {
            throw new JsonSerializerException(
                $"Type {inputType} is not known to the serializer. Ensure it's included in the JsonSerializerContext.");
        }

        return JsonSerializer.Serialize(value, typeInfo);
#endif
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Adds a JsonSerializerContext to the serializer options.
    /// </summary>
    /// <param name="context">The JsonSerializerContext to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the context is null.</exception>
    internal static void AddSerializerContext(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!AdditionalContexts.Contains(context))
        {
            AdditionalContexts.Add(context);
        }
    }

    /// <summary>
    /// Gets the JsonTypeInfo for a given type.
    /// </summary>
    /// <param name="type">The type to get information for.</param>
    /// <returns>The JsonTypeInfo for the specified type, or null if not found.</returns>
    internal static JsonTypeInfo GetTypeInfo(Type type)
    {
        var options = GetSerializerOptions();
        return options.TypeInfoResolver?.GetTypeInfo(type, options);
    }
#endif

    /// <summary>
    /// Builds and configures the JsonSerializerOptions.
    /// </summary>
    /// <returns>A configured JsonSerializerOptions instance.</returns>
    private static JsonSerializerOptions BuildJsonSerializerOptions()
    {
        _jsonOptions = new JsonSerializerOptions();

        switch (_currentOutputCase)
        {
            case LoggerOutputCase.CamelCase:
                _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                _jsonOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                break;
            case LoggerOutputCase.PascalCase:
                _jsonOptions.PropertyNamingPolicy = PascalCaseNamingPolicy.Instance;
                _jsonOptions.DictionaryKeyPolicy = PascalCaseNamingPolicy.Instance;
                break;
            default: // Snake case
#if NET8_0_OR_GREATER
                _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                _jsonOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
#else
                _jsonOptions.PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance;
                _jsonOptions.DictionaryKeyPolicy = SnakeCaseNamingPolicy.Instance;
#endif
                break;
        }

        _jsonOptions.Converters.Add(new ByteArrayConverter());
        _jsonOptions.Converters.Add(new ExceptionConverter());
        _jsonOptions.Converters.Add(new MemoryStreamConverter());
        _jsonOptions.Converters.Add(new ConstantClassConverter());
        _jsonOptions.Converters.Add(new DateOnlyConverter());
        _jsonOptions.Converters.Add(new TimeOnlyConverter());

#if NET8_0_OR_GREATER
        _jsonOptions.Converters.Add(new LogLevelJsonConverter());
#elif NET6_0
        _jsonOptions.Converters.Add(new LogLevelJsonConverter());
#endif

        _jsonOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        _jsonOptions.PropertyNameCaseInsensitive = true;

#if NET8_0_OR_GREATER

        // Only add TypeInfoResolver if AOT mode
        if (!RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
            _jsonOptions.TypeInfoResolverChain.Add(PowertoolsLoggingSerializationContext.Default);
            foreach (var context in AdditionalContexts)
            {
                _jsonOptions.TypeInfoResolverChain.Add(context);
            }
        }
#endif
        return _jsonOptions;
    }

#if NET8_0_OR_GREATER
    internal static bool HasContext(JsonSerializerContext customContext)
    {
        return AdditionalContexts.Contains(customContext);
    }

    internal static void ClearContext()
    {
        AdditionalContexts.Clear();
    }
#endif

    /// <summary>
    /// Clears options for tests
    /// </summary>
    internal static void ClearOptions()
    {
        _jsonOptions = null;
    }
}