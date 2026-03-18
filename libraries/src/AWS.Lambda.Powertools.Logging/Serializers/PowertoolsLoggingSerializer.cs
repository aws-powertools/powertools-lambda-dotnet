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
    private volatile JsonSerializerOptions _jsonOptions;
    private readonly object _lock = new();

    private readonly ConcurrentBag<JsonSerializerContext> _additionalContexts = new();
    private static JsonSerializerContext _staticAdditionalContexts;
    private IJsonTypeInfoResolver _customTypeInfoResolver;

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

                // Force a full rebuild on next access instead of mutating existing options,
                // because JsonSerializerOptions becomes read-only after first serialization.
                _jsonOptions = null;
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
    }

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

    /// <summary>
    /// Builds and configures the JsonSerializerOptions.
    /// </summary>
    /// <returns>A configured JsonSerializerOptions instance.</returns>
    private void BuildJsonSerializerOptions(JsonSerializerOptions options = null)
    {
        lock (_lock)
        {
            // Build into a local variable so _jsonOptions is never visible in a partially-configured state.
            // Another thread doing a lock-free read in GetSerializerOptions() could see a non-null _jsonOptions,
            // use it for serialization (making it read-only), and then subsequent mutations here would throw.
            var newOptions = new JsonSerializerOptions();

            // Copy any properties from the original options if provided
            if (options != null)
            {
                // Copy standard properties
                newOptions.DefaultIgnoreCondition = options.DefaultIgnoreCondition;
                newOptions.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
                newOptions.PropertyNamingPolicy = options.PropertyNamingPolicy;
                newOptions.DictionaryKeyPolicy = options.DictionaryKeyPolicy;
                newOptions.WriteIndented = options.WriteIndented;
                newOptions.ReferenceHandler = options.ReferenceHandler;
                newOptions.MaxDepth = options.MaxDepth;
                newOptions.IgnoreReadOnlyFields = options.IgnoreReadOnlyFields;
                newOptions.IgnoreReadOnlyProperties = options.IgnoreReadOnlyProperties;
                newOptions.IncludeFields = options.IncludeFields;
                newOptions.NumberHandling = options.NumberHandling;
                newOptions.ReadCommentHandling = options.ReadCommentHandling;
                newOptions.UnknownTypeHandling = options.UnknownTypeHandling;
                newOptions.AllowTrailingCommas = options.AllowTrailingCommas;

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
            }

            // Set output case and other properties
            SetOutputCase(newOptions);
            AddConverters(newOptions);
            newOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            newOptions.PropertyNameCaseInsensitive = true;

            // Set TypeInfoResolver last, as this makes options read-only
            if (!RuntimeFeatureWrapper.IsDynamicCodeSupported)
            {
                newOptions.TypeInfoResolver = GetCompositeResolver();
            }

            // Publish fully-configured options in a single atomic assignment
            _jsonOptions = newOptions;
        }
    }

    internal void SetOutputCase(JsonSerializerOptions target)
    {
        switch (_currentOutputCase)
        {
            case LoggerOutputCase.CamelCase:
                target.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                target.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                break;
            case LoggerOutputCase.PascalCase:
                target.PropertyNamingPolicy = PascalCaseNamingPolicy.Instance;
                target.DictionaryKeyPolicy = PascalCaseNamingPolicy.Instance;
                break;
            default: // Snake case
                // If is default (Not Set) and JsonOptions provided with DictionaryKeyPolicy or PropertyNamingPolicy, use it
                target.DictionaryKeyPolicy ??= JsonNamingPolicy.SnakeCaseLower;
                target.PropertyNamingPolicy ??= JsonNamingPolicy.SnakeCaseLower;
                break;
        }
    }

    private static void AddConverters(JsonSerializerOptions target)
    {
        target.Converters.Add(new ByteArrayConverter());
        target.Converters.Add(new ExceptionConverter());
        target.Converters.Add(new MemoryStreamConverter());
        target.Converters.Add(new ConstantClassConverter());
        target.Converters.Add(new DateOnlyConverter());
        target.Converters.Add(new TimeOnlyConverter());

        target.Converters.Add(new LogLevelJsonConverter());
    }

    internal void SetOptions(JsonSerializerOptions options)
    {
        _currentOptions = options;
    }
}