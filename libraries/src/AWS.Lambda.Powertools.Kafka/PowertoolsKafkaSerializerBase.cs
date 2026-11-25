using Amazon.Lambda.Core;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using AWS.Lambda.Powertools.Common.Utils;

#if KAFKA_JSON
namespace AWS.Lambda.Powertools.Kafka.Json;
#elif KAFKA_AVRO
namespace AWS.Lambda.Powertools.Kafka.Avro;
#elif KAFKA_PROTOBUF
namespace AWS.Lambda.Powertools.Kafka.Protobuf;
#else
namespace AWS.Lambda.Powertools.Kafka;
#endif

/// <summary>
/// Base class for Kafka event serializers that provides common functionality
/// for deserializing Kafka event structures in Lambda functions.
/// </summary>
/// <example>
/// Inherit from this class to implement specific formats like Avro, Protobuf or JSON.
/// </example>
public abstract class PowertoolsKafkaSerializerBase : ILambdaSerializer
{
    /// <summary>
    /// JSON serializer options used for deserialization.
    /// </summary>
    protected readonly JsonSerializerOptions JsonOptions;

    /// <summary>
    /// JSON serializer context used for AOT-compatible serialization/deserialization.
    /// </summary>
    protected readonly JsonSerializerContext? SerializerContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaSerializerBase"/> class
    /// with default JSON serialization options.
    /// </summary>
    protected PowertoolsKafkaSerializerBase() : this(new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaSerializerBase"/> class
    /// with custom JSON serialization options.
    /// </summary>
    /// <param name="jsonOptions">Custom JSON serializer options to use during deserialization.</param>
    protected PowertoolsKafkaSerializerBase(JsonSerializerOptions jsonOptions) : this(jsonOptions, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaSerializerBase"/> class
    /// with a JSON serializer context for AOT-compatible serialization/deserialization.
    /// </summary>
    /// <param name="serializerContext">The JSON serializer context for AOT compatibility.</param>
    protected PowertoolsKafkaSerializerBase(JsonSerializerContext serializerContext) : this(serializerContext.Options,
        serializerContext)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaSerializerBase"/> class
    /// with custom JSON serialization options and an optional serializer context.
    /// </summary>
    /// <param name="jsonOptions">Custom JSON serializer options to use during deserialization.</param>
    /// <param name="serializerContext">Optional JSON serializer context for AOT compatibility.</param>
    protected PowertoolsKafkaSerializerBase(JsonSerializerOptions jsonOptions, JsonSerializerContext? serializerContext)
    {
        JsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        SerializerContext = serializerContext;
    }

    /// <summary>
    /// The members of Kafka key and value types that the serializers read: the parameterless constructor,
    /// public properties (such as the Protobuf <c>Parser</c>) and public fields (such as the Avro <c>_SCHEMA</c>).
    /// </summary>
    internal const DynamicallyAccessedMemberTypes DeserializedMemberTypes =
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields;

    /// <summary>
    /// Deserializes the Lambda input stream into the specified type.
    /// Handles Kafka events with various serialization formats.
    /// </summary>
    /// <remarks>
    /// <see cref="ConsumerRecords{TK,T}"/> is built without reflection, so it works under Native AOT.
    /// Other types are deserialized with the <see cref="JsonSerializerContext"/> passed to the constructor,
    /// or with the <c>TypeInfoResolver</c> of the <see cref="JsonSerializerOptions"/>. Reflection-based
    /// deserialization is only used when it's enabled, which isn't the case under Native AOT.
    /// </remarks>
    public T Deserialize<T>(Stream requestStream)
    {
        using var reader = new StreamReader(requestStream);
        var json = reader.ReadToEnd();

        var targetType = typeof(T);

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(ConsumerRecords<,>))
        {
            return DeserializeConsumerRecords<T>(json);
        }

        var contextTypeInfo = SerializerContext?.GetTypeInfo(targetType);
        if (contextTypeInfo != null)
        {
            return (T)JsonSerializer.Deserialize(json, contextTypeInfo)!;
        }

        // JsonSerializer.IsReflectionEnabledByDefault is a feature switch that is false in trimmed and Native AOT
        // apps. The trimmer and ILC know it's false there, so they don't report the guarded reflection-based call.
        // RuntimeFeatureWrapper lets tests simulate Native AOT.
        if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault; needed for the net8.0 analyzer.
            return DeserializeWithReflection<T>(json);
#pragma warning restore IL2026, IL3050
        }

        var typeInfo = GetTypeInfoFromOptions(targetType) ?? throw ReflectionDisabledException("deserialize", targetType);
        return JsonSerializer.Deserialize(json, typeInfo) is T result
            ? result
            : throw new InvalidOperationException($"Failed to deserialize to type {targetType.Name}");
    }

    [RequiresUnreferencedCode("Reflection-based JSON deserialization of types that can't be statically analyzed.")]
    [RequiresDynamicCode("Reflection-based JSON deserialization might require runtime code generation.")]
    private T DeserializeWithReflection<T>(string json)
    {
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
        return result ?? throw new InvalidOperationException($"Failed to deserialize to type {typeof(T).Name}");
    }

    /// <summary>
    /// Deserializes a Kafka ConsumerRecords event from JSON string.
    /// </summary>
    /// <typeparam name="T">The ConsumerRecords type with key and value generics.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized ConsumerRecords object.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ConsumerRecords<,>))]
    [UnconditionalSuppressMessage("Trimming", "IL2091",
        Justification = "T is always a ConsumerRecords<,> instantiation (checked by the caller). Its public " +
                        "parameterless constructor is kept by the DynamicDependency above. Native AOT compiles " +
                        "Activator.CreateInstance<T>() to a lookup of the constructor of the instantiation that " +
                        "Deserialize<T> is called with, so no reflection metadata is needed.")]
    private T DeserializeConsumerRecords<T>(string json)
    {
        using var document = JsonDocument.Parse(json);

        // ILambdaSerializer.Deserialize<T> doesn't give us the key and value types, so the instance fills itself
        // in ConsumerRecords<TK, T>, where they are known statically. See PopulateConsumerRecords.
        var consumerRecords = Activator.CreateInstance<T>();
        ((IPopulatableConsumerRecords)consumerRecords!).Populate(document.RootElement, this);
        return consumerRecords;
    }

    /// <summary>
    /// Populates <paramref name="consumerRecords"/> from the Lambda Kafka event.
    /// </summary>
    internal void PopulateConsumerRecords<
        [DynamicallyAccessedMembers(DeserializedMemberTypes)] TKey,
        [DynamicallyAccessedMembers(DeserializedMemberTypes)] TValue>(
        ConsumerRecords<TKey, TValue> consumerRecords, JsonElement root)
    {
        if (root.TryGetProperty("eventSource", out var eventSource))
            consumerRecords.EventSource = eventSource.GetString()!;

        if (root.TryGetProperty("eventSourceArn", out var eventSourceArn))
            consumerRecords.EventSourceArn = eventSourceArn.GetString()!;

        if (root.TryGetProperty("bootstrapServers", out var bootstrapServers))
            consumerRecords.BootstrapServers = bootstrapServers.GetString()!;

        if (!root.TryGetProperty("records", out var recordsElement))
            return;

        var records = new Dictionary<string, List<ConsumerRecord<TKey, TValue>>>();

        // Process each topic partition
        foreach (var topicPartition in recordsElement.EnumerateObject())
        {
            var recordsList = new List<ConsumerRecord<TKey, TValue>>();
            foreach (var recordElement in topicPartition.Value.EnumerateArray())
            {
                recordsList.Add(CreateAndPopulateRecord<TKey, TValue>(recordElement));
            }

            records.Add(topicPartition.Name, recordsList);
        }

        consumerRecords.Records = records;
    }

    private ConsumerRecord<TKey, TValue> CreateAndPopulateRecord<
        [DynamicallyAccessedMembers(DeserializedMemberTypes)] TKey,
        [DynamicallyAccessedMembers(DeserializedMemberTypes)] TValue>(JsonElement recordElement)
    {
        var record = new ConsumerRecord<TKey, TValue>();

        // Set basic properties
        if (TryGetNonNullProperty(recordElement, "topic", out var topic))
            record.Topic = topic.GetString()!;
        if (TryGetNonNullProperty(recordElement, "partition", out var partition))
            record.Partition = partition.GetInt32();
        if (TryGetNonNullProperty(recordElement, "offset", out var offset))
            record.Offset = offset.GetInt64();
        if (TryGetNonNullProperty(recordElement, "timestamp", out var timestamp))
            record.Timestamp = timestamp.GetInt64();
        if (TryGetNonNullProperty(recordElement, "timestampType", out var timestampType))
            record.TimestampType = timestampType.GetString()!;

        // Process schema metadata for both key and value FIRST
        SchemaMetadata? keySchemaMetadata = null;
        SchemaMetadata? valueSchemaMetadata = null;

        if (recordElement.TryGetProperty("keySchemaMetadata", out var keyMetadataElement))
        {
            record.KeySchemaMetadata = ReadSchemaMetadata(keyMetadataElement);
            keySchemaMetadata = ExtractSchemaMetadata(keyMetadataElement);
        }

        if (recordElement.TryGetProperty("valueSchemaMetadata", out var valueMetadataElement))
        {
            record.ValueSchemaMetadata = ReadSchemaMetadata(valueMetadataElement);
            valueSchemaMetadata = ExtractSchemaMetadata(valueMetadataElement);
        }

        // Process key with schema metadata context
        ProcessKey(recordElement, record, keySchemaMetadata);

        // Process value with schema metadata context
        ProcessValue(recordElement, record, valueSchemaMetadata);

        // Process headers
        ProcessHeaders(recordElement, record);

        return record;
    }

    private static bool TryGetNonNullProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        return element.TryGetProperty(propertyName, out value) && value.ValueKind != JsonValueKind.Null;
    }

    private SchemaMetadata? ExtractSchemaMetadata(JsonElement metadataElement)
    {
        var schemaMetadata = new SchemaMetadata();
        var hasData = false;

        if (metadataElement.TryGetProperty("dataFormat", out var dataFormatElement))
        {
            schemaMetadata.DataFormat = dataFormatElement.GetString() ?? string.Empty;
            hasData = true;
        }

        if (metadataElement.TryGetProperty("schemaId", out var schemaIdElement))
        {
            schemaMetadata.SchemaId = schemaIdElement.GetString() ?? string.Empty;
            hasData = true;
        }

        return hasData ? schemaMetadata : null;
    }

    private void ProcessKey<[DynamicallyAccessedMembers(DeserializedMemberTypes)] TKey, TValue>(
        JsonElement recordElement, ConsumerRecord<TKey, TValue> record, SchemaMetadata? keySchemaMetadata)
    {
        if (recordElement.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.String)
        {
            var base64Key = keyElement.GetString();
            if (!string.IsNullOrEmpty(base64Key))
            {
                try
                {
                    var keyBytes = Convert.FromBase64String(base64Key);
                    var decodedKey = DeserializeKey(keyBytes, typeof(TKey), keySchemaMetadata);
                    record.Key = ConvertDeserialized<TKey>(decodedKey);
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Failed to deserialize key data: {ex.Message}", ex);
                }
            }
        }
    }

    private void ProcessValue<TKey, [DynamicallyAccessedMembers(DeserializedMemberTypes)] TValue>(
        JsonElement recordElement, ConsumerRecord<TKey, TValue> record, SchemaMetadata? valueSchemaMetadata)
    {
        if (recordElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.String)
        {
            var base64Value = valueElement.GetString();

            if (base64Value != null)
            {
                try
                {
                    var deserializedValue = DeserializeValue(base64Value, typeof(TValue), valueSchemaMetadata);
                    record.Value = ConvertDeserialized<TValue>(deserializedValue);
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Failed to deserialize value data: {ex.Message}", ex);
                }
            }
        }
    }

    /// <summary>
    /// Converts a deserialized key or value to its static type. Null becomes the default value, as it did when
    /// the property was set with reflection.
    /// </summary>
    private static T ConvertDeserialized<T>(object? value)
    {
        return value is null ? default! : (T)value;
    }

    /// <summary>
    /// Deserializes a key from bytes based on the specified key type.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The target type for the key.</param>
    /// <param name="keySchemaMetadata">Optional schema metadata for the key.</param>
    /// <returns>The deserialized key object.</returns>
    private object? DeserializeKey(byte[] keyBytes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type keyType, SchemaMetadata? keySchemaMetadata)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (keyBytes == null || keyBytes.Length == 0)
            return null;

        if (IsPrimitiveOrSimpleType(keyType))
        {
            return DeserializePrimitiveValue(keyBytes, keyType);
        }

        // For complex types, use format-specific deserialization
        return DeserializeFormatSpecific(keyBytes, keyType, isKey: true, keySchemaMetadata);
    }

    /// <summary>
    /// Serializes an object to JSON and writes it to the provided stream.
    /// </summary>
    /// <remarks>
    /// Uses the <see cref="JsonSerializerContext"/> passed to the constructor when it includes the type.
    /// When reflection-based serialization is disabled, as under Native AOT, it uses the <c>TypeInfoResolver</c>
    /// of the <see cref="JsonSerializerOptions"/>, and serializes <see cref="string"/> and common primitive types
    /// without a context.
    /// </remarks>
    public void Serialize<T>(T response, Stream responseStream)
    {
        if (EqualityComparer<T>.Default.Equals(response, default(T)))
        {
            if (responseStream.CanWrite)
            {
                var nullBytes = Encoding.UTF8.GetBytes("null");
                responseStream.Write(nullBytes, 0, nullBytes.Length);
            }
            return;
        }

        var responseType = response!.GetType();

        if (SerializerContext != null)
        {
            var typeInfo = SerializerContext.GetTypeInfo(responseType) ??
                          SerializerContext.GetTypeInfo(typeof(T));
            if (typeInfo != null)
            {
                JsonSerializer.Serialize(responseStream, response, typeInfo);
                return;
            }
        }

        // See Deserialize<T> for why the feature switch is checked here.
        if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
        {
#pragma warning disable IL2026, IL3050 // Guarded by JsonSerializer.IsReflectionEnabledByDefault; needed for the net8.0 analyzer.
            SerializeWithReflection(response, responseStream);
#pragma warning restore IL2026, IL3050
            return;
        }

        var fallbackTypeInfo = GetTypeInfoFromOptions(responseType) ??
                               GetTypeInfoFromOptions(typeof(T)) ??
                               KafkaBuiltInJsonSerializerContext.Default.GetTypeInfo(responseType) ??
                               throw ReflectionDisabledException("serialize", responseType);
        JsonSerializer.Serialize(responseStream, response, fallbackTypeInfo);
    }

    [RequiresUnreferencedCode("Reflection-based JSON serialization of types that can't be statically analyzed.")]
    [RequiresDynamicCode("Reflection-based JSON serialization might require runtime code generation.")]
    private void SerializeWithReflection<T>(T response, Stream responseStream)
    {
        using var writer = new StreamWriter(responseStream, encoding: Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
        var jsonResponse = JsonSerializer.Serialize(response, JsonOptions);
        writer.Write(jsonResponse);
        writer.Flush();
    }

    /// <summary>
    /// Gets the metadata for <paramref name="type"/> from the <c>TypeInfoResolver</c> of the JSON options,
    /// without falling back to reflection.
    /// </summary>
    private JsonTypeInfo? GetTypeInfoFromOptions(Type type)
    {
        if (JsonOptions.TypeInfoResolver == null)
            return null;

        return JsonOptions.TryGetTypeInfo(type, out var typeInfo) ? typeInfo : null;
    }

    private static InvalidOperationException ReflectionDisabledException(string operation, Type type)
    {
        return new InvalidOperationException(
            $"Can't {operation} '{type.FullName}' because reflection-based JSON serialization is disabled, " +
            "for example in Native AOT and trimmed apps. Pass a JsonSerializerContext that includes " +
            $"[JsonSerializable(typeof({type.Name}))] to the serializer constructor.");
    }

    /// <summary>
    /// Deserializes a base64-encoded value into an object using the appropriate format.
    /// </summary>
    protected virtual object DeserializeValue(string base64Value,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type valueType, SchemaMetadata? valueSchemaMetadata = null)
    {
        if (IsPrimitiveOrSimpleType(valueType))
        {
            var bytes = Convert.FromBase64String(base64Value);
            return DeserializePrimitiveValue(bytes, valueType);
        }

        var data = Convert.FromBase64String(base64Value);
        return DeserializeFormatSpecific(data, valueType, isKey: false, valueSchemaMetadata);
    }

    /// <summary>
    /// Deserializes binary data using format-specific implementation.
    /// </summary>
    protected virtual object? DeserializeFormatSpecific(byte[] data, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
    {
        if (IsPrimitiveOrSimpleType(targetType))
        {
            return DeserializePrimitiveValue(data, targetType);
        }

        return DeserializeComplexTypeFormat(data, targetType, isKey, schemaMetadata);
    }

    /// <summary>
    /// Deserializes complex (non-primitive) types using format-specific implementation.
    /// Each derived class must implement this method to handle its specific format.
    /// </summary>
    protected abstract object? DeserializeComplexTypeFormat(byte[] data, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                                   DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null);

    /// <summary>
    /// Checks if the specified type is a primitive or simple type.
    /// </summary>
    protected bool IsPrimitiveOrSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid);
    }

    /// <summary>
    /// Deserializes a primitive value from bytes based on the specified type.
    /// </summary>
    protected object? DeserializePrimitiveValue(byte[] bytes, Type valueType)
    {
        if (bytes == null! || bytes.Length == 0)
            return null!;

        if (valueType == typeof(string))
            return Encoding.UTF8.GetString(bytes);

        var stringValue = Encoding.UTF8.GetString(bytes);

        return valueType.Name switch
        {
            nameof(Int32) => DeserializeIntValue(bytes, stringValue),
            nameof(Int64) => DeserializeLongValue(bytes, stringValue),
            nameof(Double) => DeserializeDoubleValue(bytes, stringValue),
            nameof(Boolean) => DeserializeBoolValue(bytes, stringValue),
            nameof(Guid) => DeserializeGuidValue(bytes, stringValue),
            _ => DeserializeGenericValue(stringValue, valueType)
        };
    }
    
    private object DeserializeIntValue(byte[] bytes, string stringValue)
    {
        // Try string parsing first
        if (int.TryParse(stringValue, out var parsedValue))
            return parsedValue;
            
        // Fall back to binary representation
        return bytes.Length switch
        {
            >= 4 => BitConverter.ToInt32(bytes, 0),
            1 => bytes[0],
            _ => 0
        };
    }
    
    private object DeserializeLongValue(byte[] bytes, string stringValue)
    {
        if (long.TryParse(stringValue, out var parsedValue))
            return parsedValue;
            
        return bytes.Length switch
        {
            >= 8 => BitConverter.ToInt64(bytes, 0),
            >= 4 => BitConverter.ToInt32(bytes, 0),
            _ => 0L
        };
    }
    
    private object DeserializeDoubleValue(byte[] bytes, string stringValue)
    {
        if (double.TryParse(stringValue, out var doubleValue))
            return doubleValue;
            
        return bytes.Length >= 8 ? BitConverter.ToDouble(bytes, 0) : 0.0;
    }
    
    private object DeserializeBoolValue(byte[] bytes, string stringValue)
    {
        if (bool.TryParse(stringValue, out var boolValue))
            return boolValue;
            
        return bytes[0] != 0;
    }
    
    private object? DeserializeGuidValue(byte[] bytes, string stringValue)
    {
        if (bytes.Length < 16)
            return Guid.Empty;
            
        try
        {
            return new Guid(bytes);
        }
        catch
        {
            // If binary parsing fails, try string parsing
            return Guid.TryParse(stringValue, out var guidValue) ? guidValue : Guid.Empty;
        }
    }
    
    private object? DeserializeGenericValue(string stringValue, Type valueType)
    {
        try
        {
            return Convert.ChangeType(stringValue, valueType);
        }
        catch
        {
            return valueType.IsValueType ? GetDefaultValue(valueType) : null;
        }
    }

    /// <summary>
    /// Gets the default value of a primitive or simple value type without reflection.
    /// </summary>
    private static object? GetDefaultValue(Type valueType)
    {
        if (valueType == typeof(IntPtr)) return IntPtr.Zero;
        if (valueType == typeof(UIntPtr)) return UIntPtr.Zero;
        if (valueType == typeof(Guid)) return Guid.Empty;

        return Type.GetTypeCode(valueType) switch
        {
            TypeCode.Boolean => false,
            TypeCode.Char => '\0',
            TypeCode.SByte => (sbyte)0,
            TypeCode.Byte => (byte)0,
            TypeCode.Int16 => (short)0,
            TypeCode.UInt16 => (ushort)0,
            TypeCode.Int32 => 0,
            TypeCode.UInt32 => 0u,
            TypeCode.Int64 => 0L,
            TypeCode.UInt64 => 0ul,
            TypeCode.Single => 0f,
            TypeCode.Double => 0d,
            TypeCode.Decimal => 0m,
            TypeCode.DateTime => default(DateTime),
            // Only primitive and simple types get here. Anything else becomes null, which a
            // ConsumerRecord turns into the default value of its key or value type.
            _ => null
        };
    }

    private static SchemaMetadata ReadSchemaMetadata(JsonElement metadataElement)
    {
        var schemaMetadata = new SchemaMetadata();

        if (metadataElement.TryGetProperty("dataFormat", out var dataFormatElement))
        {
            schemaMetadata.DataFormat = dataFormatElement.GetString() ?? string.Empty;
        }

        if (metadataElement.TryGetProperty("schemaId", out var schemaIdElement))
        {
            schemaMetadata.SchemaId = schemaIdElement.GetString() ?? string.Empty;
        }

        return schemaMetadata;
    }

    private void ProcessHeaders<TKey, TValue>(JsonElement recordElement, ConsumerRecord<TKey, TValue> record)
    {
        if (recordElement.TryGetProperty("headers", out var headersElement) &&
            headersElement.ValueKind == JsonValueKind.Array)
        {
            var headers = new Dictionary<string, byte[]>();

            foreach (var headerObj in headersElement.EnumerateArray())
            {
                foreach (var header in headerObj.EnumerateObject())
                {
                    if (header.Value.ValueKind == JsonValueKind.Array)
                    {
                        headers[header.Name] = ExtractHeaderBytes(header.Value);
                    }
                }
            }

            record.Headers = headers;
        }
    }

    private byte[] ExtractHeaderBytes(JsonElement headerArray)
    {
        var headerBytes = new byte[headerArray.GetArrayLength()];
        var i = 0;
        foreach (var byteVal in headerArray.EnumerateArray())
        {
            headerBytes[i++] = (byte)byteVal.GetInt32();
        }

        return headerBytes;
    }
}

