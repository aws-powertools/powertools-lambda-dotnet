using System;
using System.Diagnostics.CodeAnalysis;
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
    ///     Whether reflection-based serialization is enabled. <see cref="JsonSerializer.IsReflectionEnabledByDefault" />
    ///     is a feature switch that is false in trimmed and Native AOT apps, where the source-generated contexts are used.
    /// </summary>
    /// <remarks>
    ///     Excluded from coverage because the switch is always true in the test process; its false side is verified
    ///     with Native AOT publishes. The reflection-based calls below check the switch again themselves, because the
    ///     trimmer and ILC only treat it as a guard right next to the call.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsReflectionSerializationEnabled() => JsonSerializer.IsReflectionEnabledByDefault;

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
        // RuntimeFeatureWrapper lets tests force the source-generated path
        var useSourceGeneratedContext = !RuntimeFeatureWrapper.IsDynamicCodeSupported
                                        || !IsReflectionSerializationEnabled();
        if (useSourceGeneratedContext)
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

        // RuntimeFeatureWrapper lets tests force the source-generated path
        if (RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
            AddReflectionFallback();
        }
    }

    /// <summary>
    ///     Adds reflection-based metadata after the user's context. A non-empty resolver chain replaces the default
    ///     reflection resolver, so without this, types missing from the user's context fail even under JIT.
    /// </summary>
    /// <remarks>Excluded from coverage: see <see cref="IsReflectionSerializationEnabled" />.</remarks>
    [ExcludeFromCodeCoverage]
    private static void AddReflectionFallback()
    {
        if (!JsonSerializer.IsReflectionEnabledByDefault)
            return;

#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault.
        _jsonOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
#pragma warning restore IL2026, IL3050
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
        // RuntimeFeatureWrapper lets tests force the source-generated path
        return RuntimeFeatureWrapper.IsDynamicCodeSupported
            ? SerializeWithReflection(value, inputType)
            : SerializeWithTypeInfo(value, inputType);
    }

    /// <remarks>Excluded from coverage: see <see cref="IsReflectionSerializationEnabled" />.</remarks>
    [ExcludeFromCodeCoverage]
    private static string SerializeWithReflection(object value, Type inputType)
    {
        if (!JsonSerializer.IsReflectionEnabledByDefault)
            return SerializeWithTypeInfo(value, inputType);

#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault.
        return JsonSerializer.Serialize(value, _jsonOptions);
#pragma warning restore IL2026, IL3050
    }

    private static string SerializeWithTypeInfo(object value, Type inputType)
    {
        return JsonSerializer.Serialize(value, GetTypeInfo(inputType));
    }

    /// <summary>
    /// Deserializes the specified JSON string to an object of type T.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="value">The JSON string to deserialize.</param>
    /// <returns>An object of type T represented by the JSON string.</returns>
    internal static T Deserialize<T>(string value)
    {
        // RuntimeFeatureWrapper lets tests force the source-generated path
        return RuntimeFeatureWrapper.IsDynamicCodeSupported
            ? DeserializeWithReflection<T>(value)
            : DeserializeWithTypeInfo<T>(value);
    }

    /// <remarks>Excluded from coverage: see <see cref="IsReflectionSerializationEnabled" />.</remarks>
    [ExcludeFromCodeCoverage]
    private static T DeserializeWithReflection<T>(string value)
    {
        if (!JsonSerializer.IsReflectionEnabledByDefault)
            return DeserializeWithTypeInfo<T>(value);

#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault.
        return JsonSerializer.Deserialize<T>(value, _jsonOptions);
#pragma warning restore IL2026, IL3050
    }

    private static T DeserializeWithTypeInfo<T>(string value)
    {
        return (T)JsonSerializer.Deserialize(value, GetTypeInfo(typeof(T)));
    }
}