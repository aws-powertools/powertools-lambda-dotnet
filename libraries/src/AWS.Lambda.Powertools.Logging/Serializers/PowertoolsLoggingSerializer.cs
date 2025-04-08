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

namespace AWS.Lambda.Powertools.Logging.Serializers;

/// <summary>
/// Provides serialization functionality for Powertools logging.
/// </summary>
internal class PowertoolsLoggingSerializer
{
    private JsonSerializerOptions _currentOptions;
    private LoggerOutputCase _currentOutputCase;
    private JsonSerializerOptions _jsonOptions;
    private readonly object _lock = new();

#if NET8_0_OR_GREATER
    private readonly ConcurrentBag<JsonSerializerContext> _additionalContexts = new();
    private static JsonSerializerContext _staticAdditionalContexts;
    private IJsonTypeInfoResolver _customTypeInfoResolver;
#endif

    /// <summary>
    /// Gets the JsonSerializerOptions instance.
    /// </summary>
    internal JsonSerializerOptions GetSerializerOptions()
    {
        // Double-checked locking pattern for thread safety while ensuring we only build once
        if (_jsonOptions == null)
        {
            lock (_lock)
            {
                if (_jsonOptions == null)
                {
                    BuildJsonSerializerOptions(_currentOptions);
                }
            }
        }

        return _jsonOptions;
    }

    /// <summary>
    /// Configures the naming policy for the serializer.
    /// </summary>
    /// <param name="loggerOutputCase">The case to use for serialization.</param>
    internal void ConfigureNamingPolicy(LoggerOutputCase loggerOutputCase)
    {
        if (_currentOutputCase != loggerOutputCase)
        {
            lock (_lock)
            {
                _currentOutputCase = loggerOutputCase;

                // Only rebuild options if they already exist
                if (_jsonOptions != null)
                {
                    SetOutputCase();
                }
            }
        }
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="inputType">The type of the object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input type is not known to the serializer.</exception>
    internal string Serialize(object value, Type inputType)
    {
#if NET6_0
        var options = GetSerializerOptions();
        return JsonSerializer.Serialize(value, options);
#else
        if (RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
            var jsonSerializerOptions = GetSerializerOptions();
#pragma warning disable
            return JsonSerializer.Serialize(value, jsonSerializerOptions);
        }

        // Try to serialize using the configured TypeInfoResolver
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
    internal void AddSerializerContext(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Don't add duplicates
        if (!_additionalContexts.Contains(context))
        {
            _additionalContexts.Add(context);

            // If we have existing JSON options, update their type resolver
            if (_jsonOptions != null && !RuntimeFeatureWrapper.IsDynamicCodeSupported)
            {
                // Reset the type resolver chain to rebuild it
                _jsonOptions.TypeInfoResolver = GetCompositeResolver();
            }
        }
    }

    internal static void AddStaticSerializerContext(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _staticAdditionalContexts = context;
    }

    /// <summary>
    /// Get a composite resolver that includes all configured resolvers
    /// </summary>
    private IJsonTypeInfoResolver GetCompositeResolver()
    {
        var resolvers = new List<IJsonTypeInfoResolver>();

        // Add custom resolver if provided
        if (_customTypeInfoResolver != null)
        {
            resolvers.Add(_customTypeInfoResolver);
        }

        // add any static resolvers
        if (_staticAdditionalContexts != null)
        {
            resolvers.Add(_staticAdditionalContexts);
        }

        // Add default context
        resolvers.Add(PowertoolsLoggingSerializationContext.Default);

        // Add additional contexts
        foreach (var context in _additionalContexts)
        {
            resolvers.Add(context);
        }

        return new CompositeJsonTypeInfoResolver(resolvers.ToArray());
    }

    /// <summary>
    /// Gets the JsonTypeInfo for a given type.
    /// </summary>
    /// <param name="type">The type to get information for.</param>
    /// <returns>The JsonTypeInfo for the specified type, or null if not found.</returns>
    private JsonTypeInfo GetTypeInfo(Type type)
    {
        var options = GetSerializerOptions();
        return options.TypeInfoResolver?.GetTypeInfo(type, options);
    }

#endif

    /// <summary>
    /// Builds and configures the JsonSerializerOptions.
    /// </summary>
    /// <returns>A configured JsonSerializerOptions instance.</returns>
    private void BuildJsonSerializerOptions(JsonSerializerOptions options = null)
    {
        lock (_lock)
        {
            // Create a completely new options instance regardless
            _jsonOptions = new JsonSerializerOptions();

            // Copy any properties from the original options if provided
            if (options != null)
            {
                // Copy standard properties
                _jsonOptions.DefaultIgnoreCondition = options.DefaultIgnoreCondition;
                _jsonOptions.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
                _jsonOptions.PropertyNamingPolicy = options.PropertyNamingPolicy;
                _jsonOptions.DictionaryKeyPolicy = options.DictionaryKeyPolicy;
                _jsonOptions.WriteIndented = options.WriteIndented;
                _jsonOptions.ReferenceHandler = options.ReferenceHandler;
                _jsonOptions.MaxDepth = options.MaxDepth;
                _jsonOptions.IgnoreReadOnlyFields = options.IgnoreReadOnlyFields;
                _jsonOptions.IgnoreReadOnlyProperties = options.IgnoreReadOnlyProperties;
                _jsonOptions.IncludeFields = options.IncludeFields;
                _jsonOptions.NumberHandling = options.NumberHandling;
                _jsonOptions.ReadCommentHandling = options.ReadCommentHandling;
                _jsonOptions.UnknownTypeHandling = options.UnknownTypeHandling;
                _jsonOptions.AllowTrailingCommas = options.AllowTrailingCommas;

#if NET8_0_OR_GREATER
                // Handle type resolver extraction without setting it yet
                if (options.TypeInfoResolver != null)
                {
                    _customTypeInfoResolver = options.TypeInfoResolver;

                    // If it's a JsonSerializerContext, also add it to our contexts
                    if (_customTypeInfoResolver is JsonSerializerContext jsonContext)
                    {
                        AddSerializerContext(jsonContext);
                    }
                }
#endif
            }

            // Set output case and other properties
            SetOutputCase();
            AddConverters();
            _jsonOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            _jsonOptions.PropertyNameCaseInsensitive = true;

#if NET8_0_OR_GREATER
            // Set TypeInfoResolver last, as this makes options read-only
            if (!RuntimeFeatureWrapper.IsDynamicCodeSupported)
            {
                _jsonOptions.TypeInfoResolver = GetCompositeResolver();
            }
#endif
        }
    }

    internal void SetOutputCase()
    {
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
                // If is default (Not Set) and JsonOptions provided with DictionaryKeyPolicy or PropertyNamingPolicy, use it
                _jsonOptions.DictionaryKeyPolicy ??= JsonNamingPolicy.SnakeCaseLower;
                _jsonOptions.PropertyNamingPolicy ??= JsonNamingPolicy.SnakeCaseLower;
#else
                _jsonOptions.PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance;
                _jsonOptions.DictionaryKeyPolicy = SnakeCaseNamingPolicy.Instance;
#endif
                break;
        }
    }

    private void AddConverters()
    {
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
    }

    internal void SetOptions(JsonSerializerOptions options)
    {
        _currentOptions = options;
    }
}