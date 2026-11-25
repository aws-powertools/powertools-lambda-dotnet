using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using AWS.Lambda.Powertools.Common.Utils;

namespace AWS.Lambda.Powertools.Kafka.Json;

/// <summary>
/// A Lambda serializer for Kafka events that handles JSON-formatted data.
/// This serializer automatically deserializes the JSON format from base64-encoded strings
/// in Kafka records and converts them to strongly-typed objects.
/// </summary>
public class PowertoolsKafkaJsonSerializer : PowertoolsKafkaSerializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaJsonSerializer"/> class
    /// with default JSON serialization options.
    /// </summary>
    public PowertoolsKafkaJsonSerializer() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaJsonSerializer"/> class
    /// with custom JSON serialization options.
    /// </summary>
    /// <param name="jsonOptions">Custom JSON serializer options to use during deserialization.</param>
    public PowertoolsKafkaJsonSerializer(JsonSerializerOptions jsonOptions) : base(jsonOptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaJsonSerializer"/> class
    /// with a JSON serializer context for AOT-compatible serialization.
    /// </summary>
    /// <param name="serializerContext">JSON serializer context for AOT compatibility.</param>
    public PowertoolsKafkaJsonSerializer(JsonSerializerContext serializerContext) : base(serializerContext)
    {
    }

    /// <summary>
    /// Deserializes complex (non-primitive) types using JSON format.
    /// </summary>
    /// <remarks>
    /// Uses the <see cref="JsonSerializerContext"/> passed to the constructor, then reflection-based
    /// deserialization when it's enabled. When it isn't, as under Native AOT, it uses the <c>TypeInfoResolver</c>
    /// of the <see cref="JsonSerializerOptions"/>, and throws an <see cref="InvalidOperationException"/> if that
    /// doesn't know the type either.
    /// </remarks>
    protected override object? DeserializeComplexTypeFormat(byte[] data,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
    {
        if (data == null || data.Length == 0)
        {
            return targetType.IsValueType ? CreateDefaultValue(targetType) : null;
        }

        var jsonStr = Encoding.UTF8.GetString(data);

        // Try context-based deserialization first
        var contextTypeInfo = SerializerContext?.GetTypeInfo(targetType);
        if (contextTypeInfo != null)
        {
            return JsonSerializer.Deserialize(jsonStr, contextTypeInfo);
        }

        // JsonSerializer.IsReflectionEnabledByDefault is a feature switch that is false in trimmed and Native AOT
        // apps. The trimmer and ILC know it's false there, so they don't report the guarded reflection-based call.
        // RuntimeFeatureWrapper lets tests simulate Native AOT.
        if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault; needed for the net8.0 analyzer.
            return DeserializeWithReflection(jsonStr, targetType);
#pragma warning restore IL2026, IL3050
        }

        var typeInfo = GetTypeInfoFromOptions(targetType) ?? throw new InvalidOperationException(
            $"Can't deserialize '{targetType.FullName}' because reflection-based JSON serialization is disabled, " +
            "for example in Native AOT and trimmed apps. Pass a JsonSerializerContext that includes " +
            $"[JsonSerializable(typeof({targetType.Name}))] to the PowertoolsKafkaJsonSerializer constructor.");
        return JsonSerializer.Deserialize(jsonStr, typeInfo);
    }

    /// <summary>
    /// Creates the default value of a value type for an empty payload.
    /// </summary>
    private object? CreateDefaultValue(Type valueType)
    {
        var typeInfo = SerializerContext?.GetTypeInfo(valueType) ?? GetTypeInfoFromOptions(valueType);
        if (typeInfo?.CreateObject != null)
        {
            return typeInfo.CreateObject();
        }

        // See DeserializeComplexTypeFormat for why the feature switch is checked here.
        if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable IL2026 // Guarded by JsonSerializer.IsReflectionEnabledByDefault; needed for the net8.0 analyzer.
            return CreateInstanceWithReflection(valueType);
#pragma warning restore IL2026
        }

        // A ConsumerRecord turns null into the default value of its key or value type.
        return null;
    }

    [RequiresUnreferencedCode("Reflection-based JSON deserialization of types that can't be statically analyzed.")]
    [RequiresDynamicCode("Reflection-based JSON deserialization might require runtime code generation.")]
    private object? DeserializeWithReflection(string json, Type targetType)
    {
        return JsonSerializer.Deserialize(json, targetType, JsonOptions);
    }

    [RequiresUnreferencedCode("Creates an instance of a type that can't be statically analyzed.")]
    private static object? CreateInstanceWithReflection(Type valueType)
    {
        return Activator.CreateInstance(valueType);
    }

    private JsonTypeInfo? GetTypeInfoFromOptions(Type type)
    {
        if (JsonOptions.TypeInfoResolver == null)
            return null;

        return JsonOptions.TryGetTypeInfo(type, out var typeInfo) ? typeInfo : null;
    }
}
