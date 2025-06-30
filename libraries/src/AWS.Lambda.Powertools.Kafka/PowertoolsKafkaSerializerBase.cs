/*
 * Copyright JsonCons.Net authors. All Rights Reserved.
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

using Amazon.Lambda.Core;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using AWS.Lambda.Powertools.Common;

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
        
        SystemWrapper.Instance.SetExecutionEnvironment(this);
    }

    /// <summary>
    /// Deserializes the Lambda input stream into the specified type.
    /// Handles Kafka events with various serialization formats.
    /// </summary>
    public T Deserialize<T>(Stream requestStream)
    {
        if (SerializerContext != null && typeof(T) != typeof(ConsumerRecords<,>))
        {
            // Fast path for regular JSON types when serializer context is provided
            var typeInfo = GetJsonTypeInfo<T>();
            if (typeInfo != null)
            {
                return JsonSerializer.Deserialize(requestStream, typeInfo) ?? throw new InvalidOperationException();
            }
        }

        using var reader = new StreamReader(requestStream);
        var json = reader.ReadToEnd();

        var targetType = typeof(T);

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(ConsumerRecords<,>))
        {
            return DeserializeConsumerRecords<T>(json);
        }

        if (SerializerContext != null)
        {
            var typeInfo = SerializerContext.GetTypeInfo(targetType);
            if (typeInfo != null)
            {
                return (T)JsonSerializer.Deserialize(json, typeInfo)!;
            }
        }

#pragma warning disable IL2026, IL3050
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
#pragma warning restore IL2026, IL3050

        return result ?? throw new InvalidOperationException($"Failed to deserialize to type {typeof(T).Name}");
    }

    /// <summary>
    /// Deserializes a Kafka ConsumerRecords event from JSON string.
    /// </summary>
    /// <typeparam name="T">The ConsumerRecords type with key and value generics.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized ConsumerRecords object.</returns>
    [RequiresUnreferencedCode("ConsumerRecords deserialization uses reflection and may be incompatible with trimming.")]
    [RequiresDynamicCode(
        "ConsumerRecords deserialization dynamically creates generic types and may be incompatible with NativeAOT.")]
    private T DeserializeConsumerRecords<T>(string json)
    {
        var targetType = typeof(T);
        var typeArgs = targetType.GetGenericArguments();
        var keyType = typeArgs[0];
        var valueType = typeArgs[1];

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Create the typed instance and set basic properties
        var typedEvent = CreateConsumerRecordsInstance(targetType);
        SetBasicProperties(root, typedEvent, targetType);

        // Create and populate records dictionary
        if (root.TryGetProperty("records", out var recordsElement))
        {
            var records = CreateRecordsDictionary(recordsElement, keyType, valueType);
            targetType.GetProperty("Records", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(typedEvent, records);
        }

        return (T)typedEvent;
    }

    private object CreateConsumerRecordsInstance(Type targetType)
    {
        return Activator.CreateInstance(targetType) ??
               throw new InvalidOperationException($"Failed to create instance of {targetType.Name}");
    }

    private void SetBasicProperties(JsonElement root, object instance, Type targetType)
    {
        if (root.TryGetProperty("eventSource", out var eventSource))
            targetType.GetProperty("EventSource", BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(instance, eventSource.GetString());

        if (root.TryGetProperty("eventSourceArn", out var eventSourceArn))
            targetType.GetProperty("EventSourceArn")?.SetValue(instance, eventSourceArn.GetString());

        if (root.TryGetProperty("bootstrapServers", out var bootstrapServers))
            targetType.GetProperty("BootstrapServers")?.SetValue(instance, bootstrapServers.GetString());
    }

    private object CreateRecordsDictionary(JsonElement recordsElement, Type keyType, Type valueType)
    {
        // Create dictionary with correct generic types
        var dictType = typeof(Dictionary<,>).MakeGenericType(
            typeof(string),
            typeof(List<>).MakeGenericType(typeof(ConsumerRecord<,>).MakeGenericType(keyType, valueType))
        );
        var records = Activator.CreateInstance(dictType) ??
                      throw new InvalidOperationException($"Failed to create dictionary of type {dictType.Name}");
        var dictAddMethod = dictType.GetMethod("Add") ??
                            throw new InvalidOperationException("Add method not found on dictionary type");

        // Process each topic partition
        foreach (var topicPartition in recordsElement.EnumerateObject())
        {
            var topicName = topicPartition.Name;
            var recordsList = ProcessTopicPartition(topicPartition.Value, keyType, valueType);
            dictAddMethod.Invoke(records, new[] { topicName, recordsList });
        }

        return records;
    }

    private object ProcessTopicPartition(JsonElement partitionData, Type keyType, Type valueType)
    {
        // Create list type with correct generics
        var listType = typeof(List<>).MakeGenericType(
            typeof(ConsumerRecord<,>).MakeGenericType(keyType, valueType));
        var recordsList = Activator.CreateInstance(listType) ??
                          throw new InvalidOperationException($"Failed to create list of type {listType.Name}");
        var listAddMethod = listType.GetMethod("Add") ??
                            throw new InvalidOperationException("Add method not found on list type");

        // Process each record
        foreach (var recordElement in partitionData.EnumerateArray())
        {
            var record = CreateAndPopulateRecord(recordElement, keyType, valueType);
            if (record != null)
            {
                listAddMethod.Invoke(recordsList, new[] { record });
            }
        }

        return recordsList;
    }

    private object? CreateAndPopulateRecord(JsonElement recordElement, Type keyType, Type valueType)
    {
        // Create record instance
        var recordType = typeof(ConsumerRecord<,>).MakeGenericType(keyType, valueType);
        var record = Activator.CreateInstance(recordType);
        if (record == null)
            return null;

        // Set basic properties
        SetProperty(recordType, record, "Topic", recordElement, "topic");
        SetProperty(recordType, record, "Partition", recordElement, "partition");
        SetProperty(recordType, record, "Offset", recordElement, "offset");
        SetProperty(recordType, record, "Timestamp", recordElement, "timestamp");
        SetProperty(recordType, record, "TimestampType", recordElement, "timestampType");

        // Process schema metadata for both key and value FIRST
        SchemaMetadata? keySchemaMetadata = null;
        SchemaMetadata? valueSchemaMetadata = null;
        
        ProcessSchemaMetadata(recordElement, record, recordType, "keySchemaMetadata", "KeySchemaMetadata");
        ProcessSchemaMetadata(recordElement, record, recordType, "valueSchemaMetadata", "ValueSchemaMetadata");
        
        // Get the schema metadata for use in deserialization
        if (recordElement.TryGetProperty("keySchemaMetadata", out var keyMetadataElement))
        {
            keySchemaMetadata = ExtractSchemaMetadata(keyMetadataElement);
        }
        
        if (recordElement.TryGetProperty("valueSchemaMetadata", out var valueMetadataElement))
        {
            valueSchemaMetadata = ExtractSchemaMetadata(valueMetadataElement);
        }

        // Process key with schema metadata context
        ProcessKey(recordElement, record, recordType, keyType, keySchemaMetadata);

        // Process value with schema metadata context
        ProcessValue(recordElement, record, recordType, valueType, valueSchemaMetadata);

        // Process headers
        ProcessHeaders(recordElement, record, recordType);

        return record;
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

    private void ProcessKey(JsonElement recordElement, object record, Type recordType, Type keyType, SchemaMetadata? keySchemaMetadata)
    {
        if (recordElement.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.String)
        {
            var base64Key = keyElement.GetString();
            if (!string.IsNullOrEmpty(base64Key))
            {
                try
                {
                    var keyBytes = Convert.FromBase64String(base64Key);
                    var decodedKey = DeserializeKey(keyBytes, keyType, keySchemaMetadata);
                    recordType.GetProperty("Key")?.SetValue(record, decodedKey);
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Failed to deserialize key data: {ex.Message}", ex);
                }
            }
        }
    }

    private void ProcessValue(JsonElement recordElement, object record, Type recordType, Type valueType, SchemaMetadata? valueSchemaMetadata)
    {
        if (recordElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.String)
        {
            var base64Value = valueElement.GetString();
            var valueProperty = recordType.GetProperty("Value");

            if (base64Value != null && valueProperty != null)
            {
                try
                {
                    var deserializedValue = DeserializeValue(base64Value, valueType, valueSchemaMetadata);
                    valueProperty.SetValue(record, deserializedValue);
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Failed to deserialize value data: {ex.Message}", ex);
                }
            }
        }
    }

    /// <summary>
    /// Deserializes a key from bytes based on the specified key type.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The target type for the key.</param>
    /// <param name="keySchemaMetadata">Optional schema metadata for the key.</param>
    /// <returns>The deserialized key object.</returns>
    private object? DeserializeKey(byte[] keyBytes, Type keyType, SchemaMetadata? keySchemaMetadata)
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
    /// Sets a property value on an object instance from a JsonElement.
    /// </summary>
    /// <param name="type">The type of the object.</param>
    /// <param name="instance">The object instance.</param>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="element">The JsonElement containing the source data.</param>
    /// <param name="jsonPropertyName">The property name within the JsonElement.</param>
    [RequiresDynamicCode("Dynamically accesses properties which might be trimmed.")]
    [RequiresUnreferencedCode("Dynamically accesses properties which might be trimmed.")]
    private void SetProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type, object instance, string propertyName,
        JsonElement element, string jsonPropertyName)
    {
        if (!element.TryGetProperty(jsonPropertyName, out var jsonValue) ||
            jsonValue.ValueKind == JsonValueKind.Null)
            return;

        // Add BindingFlags to find internal properties too
        var property = type.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        if (property == null) return;
        var propertyType = property.PropertyType;

        object value;
        if (propertyType == typeof(int)) value = jsonValue.GetInt32();
        else if (propertyType == typeof(long)) value = jsonValue.GetInt64();
        else if (propertyType == typeof(double)) value = jsonValue.GetDouble();
        else if (propertyType == typeof(string)) value = jsonValue.GetString()!;
        else return;

        property.SetValue(instance, value);
    }

    /// <summary>
    /// Serializes an object to JSON and writes it to the provided stream.
    /// </summary>
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

        if (SerializerContext != null)
        {
            var typeInfo = SerializerContext.GetTypeInfo(response.GetType()) ?? 
                          SerializerContext.GetTypeInfo(typeof(T));
            if (typeInfo != null)
            {
                JsonSerializer.Serialize(responseStream, response, typeInfo);
                return;
            }
        }

        using var writer = new StreamWriter(responseStream, encoding: Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
#pragma warning disable IL2026, IL3050
        var jsonResponse = JsonSerializer.Serialize(response, JsonOptions);
#pragma warning restore IL2026, IL3050
        writer.Write(jsonResponse);
        writer.Flush();
    }

    // Helper to get non-generic JsonTypeInfo from context based on a Type argument
    private JsonTypeInfo? GetJsonTypeInfoFromContext(Type type)
    {
        if (SerializerContext == null)
            return null;

        return SerializerContext.GetTypeInfo(type);
    }

    private JsonTypeInfo<T>? GetJsonTypeInfo<T>()
    {
        if (SerializerContext == null) return null;

        foreach (var prop in SerializerContext.GetType().GetProperties())
        {
            if (prop.PropertyType == typeof(JsonTypeInfo<T>))
            {
                return prop.GetValue(SerializerContext) as JsonTypeInfo<T>;
            }
        }
        return null;
    }

    /// <summary>
    /// Deserializes a base64-encoded value into an object using the appropriate format.
    /// </summary>
    [RequiresDynamicCode("Deserializing values might require runtime code generation.")]
    [RequiresUnreferencedCode("Deserializing values might require types that cannot be statically analyzed.")]
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
    [RequiresDynamicCode("Format-specific deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Format-specific deserialization might require types that cannot be statically analyzed.")]
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
    [RequiresDynamicCode("Format-specific deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Format-specific deserialization might require types that cannot be statically analyzed.")]
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
            return valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
        }
    }

    private void ProcessSchemaMetadata(JsonElement recordElement, object record, Type recordType, 
        string jsonPropertyName, string recordPropertyName)
    {
        if (recordElement.TryGetProperty(jsonPropertyName, out var metadataElement))
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

            recordType.GetProperty(recordPropertyName)?.SetValue(record, schemaMetadata);
        }
    }

    private void ProcessHeaders(JsonElement recordElement, object record, Type recordType)
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

            var headersProperty = recordType.GetProperty("Headers",
                BindingFlags.Public | BindingFlags.Instance);
            headersProperty?.SetValue(record, headers);
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

