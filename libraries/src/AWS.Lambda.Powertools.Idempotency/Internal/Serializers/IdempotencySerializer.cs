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

    /// <summary>
    ///     Whether to use reflection-based serialization. <see cref="JsonSerializer.IsReflectionEnabledByDefault" /> is
    ///     a feature switch that is false in trimmed and Native AOT apps, where the source-generated contexts are used.
    ///     <see cref="RuntimeFeatureWrapper" /> lets tests force the source-generated path.
    /// </summary>
    private static bool UseReflectionSerialization =>
        JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported;

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
        if (!UseReflectionSerialization)
        {
            _jsonOptions.TypeInfoResolverChain.Add(IdempotencySerializationContext.Default);
        }
    }

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
    
    internal static void SetJsonOptions(JsonSerializerOptions options)
    {
        _jsonOptions = options;
    }

    /// <summary>
    /// Serializes the specified object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="inputType">The type of the object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    internal static string Serialize(object value, Type inputType)
    {
        // Inline rather than UseReflectionSerialization: the trimmer and ILC only recognize the feature switch
        // as a guard when it is checked at the call site.
        if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault.
            return JsonSerializer.Serialize(value, _jsonOptions);
#pragma warning restore IL2026, IL3050
        }


        var typeInfo = GetTypeInfo(inputType);
        if (typeInfo == null)
        {
            throw new SerializationException(
                $"Type {inputType} is not known to the serializer. Ensure it's included in the JsonSerializerContext.");
        }

        return JsonSerializer.Serialize(value, typeInfo);
    }

    /// <summary>
    /// Deserializes the specified JSON string to an object of type T.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="value">The JSON string to deserialize.</param>
    /// <returns>An object of type T represented by the JSON string.</returns>
    internal static T Deserialize<T>(string value)
    {
        if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault.
            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
#pragma warning restore IL2026, IL3050
        }

        return (T)JsonSerializer.Deserialize(value, GetTypeInfo(typeof(T)));
    }
}