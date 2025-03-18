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
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Serializers;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class PowertoolsLoggerConfiguration.
///     Implements the <see cref="T:Microsoft.Extensions.Options.IOptions{PowertoolsLoggerConfiguration}" />
/// </summary>
public class PowertoolsLoggerConfiguration : IOptions<PowertoolsLoggerConfiguration>
{
    public const string ConfigurationSectionName = "PowertoolsLogger";

    /// <summary>
    ///     Service name is used for logging.
    ///     This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
    /// </summary>
    public string? Service { get; set; } = null;

    /// <summary>
    ///     Specify the minimum log level for logging (Information, by default).
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOG_LEVEL</c>.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.None;

    /// <summary>
    ///     Dynamically set a percentage of logs to DEBUG level.
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
    /// </summary>
    public double SamplingRate { get; set; }

    /// <summary>
    /// Whether sampling was enabled for the current request
    /// </summary>
    public bool SamplingEnabled { get; set; }

    /// <summary>
    ///     The logger output case.
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_CASE</c>.
    /// </summary>
    public LoggerOutputCase LoggerOutputCase { get; set; } = LoggerOutputCase.Default;

    /// <summary>
    /// Internal key used for log level in output
    /// </summary>
    internal string LogLevelKey { get; set; } = "level";

    /// <summary>
    /// JSON serializer options to use for log serialization
    /// </summary>
    private JsonSerializerOptions? _jsonOptions;
    public JsonSerializerOptions? JsonOptions
    {
        get => _jsonOptions;
        set
        {
            _jsonOptions = value;
            if (_jsonOptions != null)
            {
#if NET8_0_OR_GREATER
                HandleJsonOptionsTypeResolver(_jsonOptions);
#endif
                ApplyJsonOptions();
            }
        }
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Default JSON serializer context
    /// </summary>
    private JsonSerializerContext? _jsonContext = PowertoolsLoggingSerializationContext.Default;
    private readonly List<JsonSerializerContext> _additionalContexts = new();

    /// <summary>
    /// Main JSON context to use for serialization
    /// </summary>
    public JsonSerializerContext? JsonContext
    {
        get => _jsonContext;
        set 
        { 
            _jsonContext = value;
            ApplyJsonContext();
        }
    }

    /// <summary>
    /// Add additional JsonSerializerContext for client types
    /// </summary>
    public void AddJsonContext(JsonSerializerContext context)
    {
        if (context != null && !_additionalContexts.Contains(context))
        {
            _additionalContexts.Add(context);
            ApplyAdditionalJsonContext(context);
        }
    }

    /// <summary>
    /// Get all additional contexts
    /// </summary>
    public IReadOnlyList<JsonSerializerContext> GetAdditionalContexts()
    {
        return _additionalContexts.AsReadOnly();
    }

    private IJsonTypeInfoResolver? _customTypeInfoResolver = null;
    private List<IJsonTypeInfoResolver>? _customTypeInfoResolvers;

    /// <summary>
    /// Process JSON options type resolver information
    /// </summary>
    public void HandleJsonOptionsTypeResolver(JsonSerializerOptions options)
    {
        if (options == null) return;

        // Check for TypeInfoResolver
        if (options.TypeInfoResolver != null && 
            options.TypeInfoResolver != GetCompositeResolver())
        {
            _customTypeInfoResolver = options.TypeInfoResolver;
        }

        // Check for TypeInfoResolverChain
        if (options.TypeInfoResolverChain != null && options.TypeInfoResolverChain.Count > 0)
        {
            foreach (var resolver in options.TypeInfoResolverChain)
            {
                // If it's a JsonSerializerContext, add it to our additional contexts
                if (resolver is JsonSerializerContext context)
                {
                    AddJsonContext(context);
                }
                // Otherwise store it as a custom resolver
                else if (resolver is IJsonTypeInfoResolver customResolver &&
                         customResolver != GetCompositeResolver() &&
                         _customTypeInfoResolver != customResolver)
                {
                    // If we already have a different custom resolver, we need to store multiple
                    if (_customTypeInfoResolver != null)
                    {
                        _customTypeInfoResolvers ??= new List<IJsonTypeInfoResolver>();
                        if (!_customTypeInfoResolvers.Contains(customResolver))
                        {
                            _customTypeInfoResolvers.Add(customResolver);
                        }
                    }
                    else
                    {
                        _customTypeInfoResolver = customResolver;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get a composite resolver that includes all configured resolvers
    /// </summary>
    public IJsonTypeInfoResolver GetCompositeResolver()
    {
        var resolvers = new List<IJsonTypeInfoResolver>();

        // Add custom resolver if provided
        if (_customTypeInfoResolver != null)
        {
            resolvers.Add(_customTypeInfoResolver);
        }

        // Add additional custom resolvers if any
        if (_customTypeInfoResolvers != null)
        {
            foreach (var resolver in _customTypeInfoResolvers)
            {
                resolvers.Add(resolver);
            }
        }

        // Add default context
        if (_jsonContext != null)
        {
            resolvers.Add(_jsonContext);
        }

        // Add additional contexts
        foreach (var context in _additionalContexts)
        {
            resolvers.Add(context);
        }

        return new CompositeJsonTypeInfoResolver(resolvers.ToArray());
    }

    /// <summary>
    /// Apply JSON context to serializer
    /// </summary>
    private void ApplyJsonContext()
    {
        if (_jsonContext != null)
        {
            PowertoolsLoggingSerializer.SetDefaultContext(_jsonContext);
        }
    }

    /// <summary>
    /// Apply additional JSON context to serializer
    /// </summary>
    private void ApplyAdditionalJsonContext(JsonSerializerContext context)
    {
        PowertoolsLoggingSerializer.AddSerializerContext(context);
    }
#endif

    /// <summary>
    /// Apply JSON options to the serializer
    /// </summary>
    private void ApplyJsonOptions()
    {
        if (_jsonOptions != null)
        {
            PowertoolsLoggingSerializer.ConfigureJsonOptions(_jsonOptions);
        }
    }

    /// <summary>
    /// Apply output case configuration
    /// </summary>
    public void ApplyOutputCase()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase);
    }

    /// <summary>
    /// Clone this configuration
    /// </summary>
    public PowertoolsLoggerConfiguration Clone()
    {
        var clone = new PowertoolsLoggerConfiguration
        {
            Service = Service,
            MinimumLevel = MinimumLevel,
            SamplingRate = SamplingRate,
            SamplingEnabled = SamplingEnabled, // Copy the sampling decision
            LoggerOutputCase = LoggerOutputCase,
            LogLevelKey = LogLevelKey,
            JsonOptions = JsonOptions
        };

#if NET8_0_OR_GREATER
        clone._jsonContext = _jsonContext;
        foreach (var context in _additionalContexts)
        {
            clone._additionalContexts.Add(context);
        }
        
        clone._customTypeInfoResolver = _customTypeInfoResolver;
        
        if (_customTypeInfoResolvers != null)
        {
            clone._customTypeInfoResolvers = new List<IJsonTypeInfoResolver>(_customTypeInfoResolvers);
        }
#endif

        return clone;
    }

    // IOptions implementation
    PowertoolsLoggerConfiguration IOptions<PowertoolsLoggerConfiguration>.Value => this;
}