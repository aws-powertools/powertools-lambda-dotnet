using Amazon.Lambda.Core;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AWS.Lambda.Powertools.Kafka;

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
    /// Deserializes the Lambda input stream into the specified type.
    /// Handles Kafka events with various serialization formats.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to. For Kafka events, typically ConsumerRecords&lt;TKey,TValue&gt;.</typeparam>
    /// <param name="requestStream">The stream containing the serialized Lambda event.</param>
    /// <returns>The deserialized object of type T.</returns>
    [RequiresUnreferencedCode("Kafka serializer uses reflection and may be incompatible with trimming. Use an overload that accepts a JsonTypeInfo or JsonSerializerContext for AOT compatibility.")]
    [RequiresDynamicCode("Kafka serializer dynamically creates generic types and may be incompatible with NativeAOT. Use an overload that accepts a JsonTypeInfo or JsonSerializerContext for AOT compatibility.")]
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
            // Try to find type info in context
            var typeInfo = SerializerContext.GetTypeInfo(targetType);
            if (typeInfo != null)
            {
                return (T)JsonSerializer.Deserialize(json, typeInfo)!;
            }
        }

        // Fallback to regular deserialization with warning
        #pragma warning disable IL2026, IL3050
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
        #pragma warning restore IL2026, IL3050
        
        return result != null
            ? result
            : throw new InvalidOperationException($"Failed to deserialize to type {typeof(T).Name}");
    }

    /// <summary>
    /// Deserializes a Kafka ConsumerRecords event from JSON string.
    /// </summary>
    /// <typeparam name="T">The ConsumerRecords type with key and value generics.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized ConsumerRecords object.</returns>
    [RequiresUnreferencedCode("ConsumerRecords deserialization uses reflection and may be incompatible with trimming.")]
    [RequiresDynamicCode("ConsumerRecords deserialization dynamically creates generic types and may be incompatible with NativeAOT.")]
    private T DeserializeConsumerRecords<T>(string json)
    {
        var targetType = typeof(T);
        var typeArgs = targetType.GetGenericArguments();
        var keyType = typeArgs[0];
        var valueType = typeArgs[1];

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Create the correctly typed instance
        var typedEvent = Activator.CreateInstance(targetType) ??
                         throw new InvalidOperationException($"Failed to create instance of {targetType.Name}");

        // Set basic properties
        if (root.TryGetProperty("eventSource", out var eventSource))
            targetType.GetProperty("EventSource",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(typedEvent, eventSource.GetString());

        if (root.TryGetProperty("eventSourceArn", out var eventSourceArn))
            targetType.GetProperty("EventSourceArn")?.SetValue(typedEvent, eventSourceArn.GetString());

        if (root.TryGetProperty("bootstrapServers", out var bootstrapServers))
            targetType.GetProperty("BootstrapServers")?.SetValue(typedEvent, bootstrapServers.GetString());

        // Create records dictionary with correct generic types
        var dictType = typeof(Dictionary<,>).MakeGenericType(
            typeof(string),
            typeof(List<>).MakeGenericType(typeof(ConsumerRecord<,>).MakeGenericType(keyType, valueType))
        );
        var records = Activator.CreateInstance(dictType) ??
                      throw new InvalidOperationException($"Failed to create dictionary of type {dictType.Name}");
        var dictAddMethod = dictType.GetMethod("Add") ??
                            throw new InvalidOperationException("Add method not found on dictionary type");

        if (root.TryGetProperty("records", out var recordsElement))
        {
            foreach (var topicPartition in recordsElement.EnumerateObject())
            {
                var topicName = topicPartition.Name;

                // Create list of records with correct generic types
                var listType =
                    typeof(List<>).MakeGenericType(typeof(ConsumerRecord<,>).MakeGenericType(keyType, valueType));
                var recordsList = Activator.CreateInstance(listType) ??
                                  throw new InvalidOperationException(
                                      $"Failed to create list of type {listType.Name}");
                var listAddMethod = listType.GetMethod("Add") ??
                                    throw new InvalidOperationException("Add method not found on list type");

                foreach (var recordElement in topicPartition.Value.EnumerateArray())
                {
                    // Create record instance of correct type
                    var recordType = typeof(ConsumerRecord<,>).MakeGenericType(keyType, valueType);
                    var record = Activator.CreateInstance(recordType);
                    if (record == null)
                        continue;

                    // Set basic properties
                    SetProperty(recordType, record, "Topic", recordElement, "topic");
                    SetProperty(recordType, record, "Partition", recordElement, "partition");
                    SetProperty(recordType, record, "Offset", recordElement, "offset");
                    SetProperty(recordType, record, "Timestamp", recordElement, "timestamp");
                    SetProperty(recordType, record, "TimestampType", recordElement, "timestampType");

                    // Handle key - base64 decode and convert to the correct type
                    if (recordElement.TryGetProperty("key", out var keyElement) &&
                        keyElement.ValueKind == JsonValueKind.String)
                    {
                        var base64Key = keyElement.GetString();
                        if (!string.IsNullOrEmpty(base64Key))
                        {
                            try
                            {
                                var keyBytes = Convert.FromBase64String(base64Key);
                                var decodedKey = DeserializeKey(keyBytes, keyType);

                                var keyProperty = recordType.GetProperty("Key");
                                keyProperty?.SetValue(record, decodedKey);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to deserialize data: {ex.Message}", ex);
                            }
                        }
                    }

                    // Handle value
                    if (recordElement.TryGetProperty("value", out var valueElement) &&
                        valueElement.ValueKind == JsonValueKind.String)
                    {
                        var base64Value = valueElement.GetString();
                        var valueProperty = recordType.GetProperty("Value");

                        if (base64Value != null && valueProperty != null)
                        {
                            try
                            {
                                var deserializedValue = DeserializeValue(base64Value, valueType);
                                valueProperty.SetValue(record, deserializedValue);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to deserialize data: {ex.Message}", ex);
                            }
                        }
                    }

                    // Process headers
                    

                    if (recordElement.TryGetProperty("headers", out var headersElement) && 
                        headersElement.ValueKind == JsonValueKind.Array)
                    {
                        var headers = new Dictionary<string, byte[]>();
    
                        foreach (var headerObj in headersElement.EnumerateArray())
                        {
                            foreach (var header in headerObj.EnumerateObject())
                            {
                                var headerKey = header.Name;
                                if (header.Value.ValueKind == JsonValueKind.Array)
                                {
                                    var headerBytes = new byte[header.Value.GetArrayLength()];
                                    var i = 0;
                                    foreach (var byteVal in header.Value.EnumerateArray())
                                    {
                                        headerBytes[i++] = (byte)byteVal.GetInt32();
                                    }
                                    headers[headerKey] = headerBytes;
                                }
                            }
                        }
    
                        var headersProperty = recordType.GetProperty("Headers",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        headersProperty?.SetValue(record, headers);
                    }

                    // Add to records list
                    listAddMethod.Invoke(recordsList, new[] { record });
                }

                // Add topic records to dictionary
                dictAddMethod.Invoke(records, new[] { topicName, recordsList });
            }
        }

        targetType.GetProperty("Records", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(typedEvent, records);
        return (T)typedEvent;
    }

    /// <summary>
    /// Deserializes a key from bytes based on the specified key type.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The target type for the key.</param>
    /// <returns>The deserialized key object.</returns>
    private object? DeserializeKey(byte[] keyBytes, Type keyType)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (keyBytes == null || keyBytes.Length == 0)
            return null;

        if (keyType == typeof(int))
        {
            // First try to interpret as a string representation and parse
            var stringValue = Encoding.UTF8.GetString(keyBytes);
            if (int.TryParse(stringValue, out var parsedValue))
                return parsedValue;

            return keyBytes.Length switch
            {
                // Fall back to binary representation if parsing fails
                >= 4 => BitConverter.ToInt32(keyBytes, 0),
                1 => keyBytes[0],
                _ => 0
            };
        }

        if (keyType == typeof(long))
        {
            // Try string parsing first
            var stringValue = Encoding.UTF8.GetString(keyBytes);
            if (long.TryParse(stringValue, out var parsedValue))
                return parsedValue;

            return keyBytes.Length switch
            {
                // Fall back to binary
                >= 8 => BitConverter.ToInt64(keyBytes, 0),
                >= 4 => BitConverter.ToInt32(keyBytes, 0),
                _ => 0L
            };
        }

        if (keyType == typeof(string))
        {
            // String conversion is safe regardless of length
            return Encoding.UTF8.GetString(keyBytes);
        }

        if (keyType == typeof(double))
        {
            return keyBytes.Length >= 8 ? BitConverter.ToDouble(keyBytes, 0) : 0.0;
        }

        if (keyType == typeof(bool) && keyBytes.Length >= 1)
        {
            return keyBytes[0] != 0;
        }

        if (keyType == typeof(Guid) && keyBytes.Length >= 16)
        {
            return new Guid(keyBytes);
        }

        // For complex types, try format-specific deserialization
        return DeserializeComplexKey(keyBytes, keyType);
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type, object instance, string propertyName,
        JsonElement element, string jsonPropertyName)
    {
        if (!element.TryGetProperty(jsonPropertyName, out var jsonValue) ||
            jsonValue.ValueKind == JsonValueKind.Null)
            return;

        // Add BindingFlags to find internal properties too
        var property = type.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="response">The object to serialize.</param>
    /// <param name="responseStream">The stream to write the serialized data to.</param>
    [RequiresDynamicCode(
        "JSON serialization might require types that cannot be statically analyzed and might need runtime code generation.")]
    [RequiresUnreferencedCode("JSON serialization might require types that cannot be statically analyzed.")]
    public void Serialize<T>(T response, Stream responseStream)
    {
        if (response == null)
        {
            // According to ILambdaSerializer contract, if response is null, an empty stream or "null" should be written.
            // AWS's default System.Text.Json serializer writes "null".
            // Let's ensure the stream is written to, as HandlerWrapper might expect some output.
            if (responseStream.CanWrite)
            {
                var nullBytes = Encoding.UTF8.GetBytes("null");
                responseStream.Write(nullBytes, 0, nullBytes.Length);
            }
            return;
        }
        
        if (SerializerContext != null)
        {
            // Attempt to get TypeInfo for the actual type of the response.
            // This is important if T is object or an interface.
            JsonTypeInfo? typeInfo = SerializerContext.GetTypeInfo(response.GetType()); 

            if (typeInfo != null)
            {
                // JsonSerializer.Serialize to a stream does not close it by default.
                JsonSerializer.Serialize(responseStream, response, typeInfo);
                return;
            }
            // Fallback: if specific type info not found, try with typeof(T) from context
            // This might be useful if T is concrete and response.GetType() is the same.
            typeInfo = GetJsonTypeInfoFromContext(typeof(T));
            if (typeInfo != null)
            {
                 // Need to cast typeInfo to non-generic JsonTypeInfo for the Serialize overload
                JsonSerializer.Serialize(responseStream, response, typeInfo);
                return;
            }
        }

        // Fallback to default JsonSerializer with options, ensuring the stream is left open.
        // StreamWriter by default uses UTF-8 encoding. We specify it explicitly for clarity.
        // The buffer size -1 can be used for default, or a specific size like 1024.
        // Crucially, leaveOpen: true prevents the StreamWriter from disposing responseStream.
        using (var writer = new StreamWriter(responseStream, encoding: Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
        {
            string jsonResponse = JsonSerializer.Serialize(response, JsonOptions);
            writer.Write(jsonResponse);
            writer.Flush(); // Ensure all data is written to the stream before writer is disposed.
        }
    }

    // Helper to get non-generic JsonTypeInfo from context based on a Type argument
    private JsonTypeInfo? GetJsonTypeInfoFromContext(Type type)
    {
        if (SerializerContext == null)
            return null;
        
        return SerializerContext.GetTypeInfo(type);
    }

    // Adjusted GetJsonTypeInfo<T> to return non-generic JsonTypeInfo for consistency,
    // or keep it if it's used elsewhere for JsonTypeInfo<T> specifically.
    // For Serialize, GetJsonTypeInfoFromContext(typeof(T)) is more direct.
    private JsonTypeInfo<T>? GetJsonTypeInfo<T>() // This is the original generic helper
    {
        if (SerializerContext == null)
            return null;

        // Use reflection to find the right JsonTypeInfo<T> property
        // This is specific to how a user might structure their JsonSerializerContext.
        // A more robust way for general types is SerializerContext.GetTypeInfo(typeof(T)).
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
    /// <param name="base64Value">The base64-encoded binary data.</param>
    /// <param name="valueType">The target type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Deserializing values might require runtime code generation depending on format.")]
    [RequiresUnreferencedCode("Deserializing values might require types that cannot be statically analyzed.")]
    protected abstract object DeserializeValue(string base64Value,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type valueType);

    /// <summary>
    /// Deserializes complex key types using the appropriate format.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The type to deserialize to.</param>
    /// <returns>The deserialized key object.</returns>
    [RequiresDynamicCode("Deserializing complex keys might require runtime code generation depending on format.")]
    [RequiresUnreferencedCode("Deserializing complex keys might require types that cannot be statically analyzed.")]
    protected abstract object? DeserializeComplexKey(byte[] keyBytes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type keyType);
}