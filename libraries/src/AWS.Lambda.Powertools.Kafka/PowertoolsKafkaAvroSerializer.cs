using Amazon.Lambda.Core;
using Avro;
using Avro.IO;
using Avro.Specific;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.Kafka;

/// <summary>
/// A Lambda serializer for Kafka events that handles Avro-formatted data.
/// This serializer automatically deserializes the Avro binary format from base64-encoded strings
/// in Kafka records and converts them to strongly-typed objects.
/// </summary>
/// <example>
/// <code>
/// [assembly: LambdaSerializer(typeof(PowertoolsKafkaAvroSerializer))]
/// 
/// // Your Lambda handler will receive properly deserialized objects
/// public class Function
/// {
///     public void Handler(ConsumerRecords&lt;Customer&gt; records, ILambdaContext context)
///     {
///         foreach (var record in records)
///         {
///             Customer customer = record.Value;
///             context.Logger.LogInformation($"Processed customer {customer.Name}, age {customer.Age}");
///         }
///     }
/// }
/// </code>
/// </example>
public class PowertoolsKafkaAvroSerializer : ILambdaSerializer
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes the Lambda input stream into the specified type.
    /// Specializes in handling Kafka events with Avro-serialized payloads.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to. For Kafka events, typically ConsumerRecords&lt;TPayload&gt;.</typeparam>
    /// <param name="requestStream">The stream containing the serialized Lambda event.</param>
    /// <returns>The deserialized object of type T.</returns>
    public T Deserialize<T>(Stream requestStream)
    {
        using var reader = new StreamReader(requestStream);
        var json = reader.ReadToEnd();

        var targetType = typeof(T);

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(ConsumerRecords<,>))
        {
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

            // Get the schema for Avro deserialization (for value)
            Schema schema = GetAvroSchema(valueType);

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
                    string topicName = topicPartition.Name;

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
                            string? base64Key = keyElement.GetString();
                            if (!string.IsNullOrEmpty(base64Key))
                            {
                                try
                                {
                                    byte[] keyBytes = Convert.FromBase64String(base64Key);
                                    object? decodedKey = DeserializeKey(keyBytes, keyType);

                                    var keyProperty = recordType.GetProperty("Key");
                                    keyProperty?.SetValue(record, decodedKey);
                                }
                                catch (Exception ex)
                                {
                                    // Log or handle key deserialization failures
                                }
                            }
                        }

                        // Handle Avro value
                        if (recordElement.TryGetProperty("value", out var valueElement) &&
                            valueElement.ValueKind == JsonValueKind.String)
                        {
                            string? base64Value = valueElement.GetString();
                            var valueProperty = recordType.GetProperty("Value");

                            // Deserialize Avro data
                            if (base64Value != null && valueProperty != null)
                            {
                                try
                                {
                                    var deserializedValue = DeserializeAvroValue(base64Value, schema);
                                    valueProperty.SetValue(record, deserializedValue);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception($"Failed to deserialize Avro data: {ex.Message}", ex);
                                }
                            }
                        }

                        // Process headers
                        if (recordElement.TryGetProperty("headers", out var headersElement) &&
                            headersElement.ValueKind == JsonValueKind.Array)
                        {
                            var decodedHeaders = new Dictionary<string, string>();

                            foreach (var headerObj in headersElement.EnumerateArray())
                            {
                                foreach (var header in headerObj.EnumerateObject())
                                {
                                    string headerKey = header.Name;
                                    if (header.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        byte[] headerBytes = new byte[header.Value.GetArrayLength()];
                                        int i = 0;
                                        foreach (var byteVal in header.Value.EnumerateArray())
                                        {
                                            headerBytes[i++] = (byte)byteVal.GetInt32();
                                        }

                                        string headerValue = Encoding.UTF8.GetString(headerBytes);
                                        decodedHeaders[headerKey] = headerValue;
                                    }
                                }
                            }

                            var headersProperty = recordType.GetProperty("Headers",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            headersProperty?.SetValue(record, decodedHeaders);
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

        var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        return result != null
            ? result
            : throw new InvalidOperationException($"Failed to deserialize to type {typeof(T).Name}");
    }

    private object? DeserializeKey(byte[] keyBytes, Type keyType)
    {
        if (keyBytes == null || keyBytes.Length == 0)
            return null;

        if (keyType == typeof(int))
        {
            // First try to interpret as a string representation and parse
            string stringValue = Encoding.UTF8.GetString(keyBytes);
            if (int.TryParse(stringValue, out int parsedValue))
                return parsedValue;
            
            // Fall back to binary representation if parsing fails
            if (keyBytes.Length >= 4)
                return BitConverter.ToInt32(keyBytes, 0);
            else if (keyBytes.Length == 1)
                return (int)keyBytes[0];
        
            return 0;
        }
        else if (keyType == typeof(long))
        {
            // Try string parsing first
            string stringValue = Encoding.UTF8.GetString(keyBytes);
            if (long.TryParse(stringValue, out long parsedValue))
                return parsedValue;
            
            // Fall back to binary
            if (keyBytes.Length >= 8)
                return BitConverter.ToInt64(keyBytes, 0);
            else if (keyBytes.Length >= 4)
                return (long)BitConverter.ToInt32(keyBytes, 0);
            
            return 0L;
        }
        else if (keyType == typeof(string))
        {
            // String conversion is safe regardless of length
            return Encoding.UTF8.GetString(keyBytes);
        }
        else if (keyType == typeof(double))
        {
            if (keyBytes.Length >= 8)
                return BitConverter.ToDouble(keyBytes, 0);
            else
                return 0.0;
        }
        else if (keyType == typeof(bool) && keyBytes.Length >= 1)
        {
            return keyBytes[0] != 0;
        }
        else if (keyType == typeof(Guid) && keyBytes.Length >= 16)
        {
            return new Guid(keyBytes);
        }

        // For complex types - try Avro or JSON deserialization
        try
        {
            // Try to get Avro schema for the key type
            var schemaField = keyType.GetField("_SCHEMA",
                BindingFlags.Public | BindingFlags.Static);

            if (schemaField != null)
            {
                var schema = schemaField.GetValue(null) as Schema;
                if (schema != null)
                {
                    using var stream = new MemoryStream(keyBytes);
                    var decoder = new BinaryDecoder(stream);
                    var reader = new SpecificDatumReader<object>(schema, schema);
                    return reader.Read(null!, decoder);
                }
            }

            // As a fallback, try JSON deserialization
            string jsonStr = Encoding.UTF8.GetString(keyBytes);
            return JsonSerializer.Deserialize(jsonStr, keyType, _jsonOptions);
        }
        catch
        {
            // If all deserialization attempts fail, return null
            return null;
        }
    }

    /// <summary>
    /// Sets a property value on an object instance from a JsonElement.
    /// </summary>
    /// <param name="type">The type of the object.</param>
    /// <param name="instance">The object instance.</param>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="element">The JsonElement containing the source data.</param>
    /// <param name="jsonPropertyName">The property name within the JsonElement.</param>
    private void SetProperty(Type type, object instance, string propertyName,
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
    /// Gets the Avro schema for the specified type.
    /// The type must have a public static _SCHEMA field defined.
    /// </summary>
    /// <param name="payloadType">The type to get the Avro schema for.</param>
    /// <returns>The Avro Schema object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no schema is found for the type.</exception>
    private Schema GetAvroSchema(Type payloadType)
    {
        var schemaField = payloadType.GetField("_SCHEMA",
            BindingFlags.Public | BindingFlags.Static);

        if (schemaField == null)
            throw new InvalidOperationException($"No Avro schema found for type {payloadType.Name}");

        var schema = schemaField.GetValue(null) as Schema;
        if (schema == null)
            throw new InvalidOperationException($"Avro schema for type {payloadType.Name} is null");

        return schema;
    }

    /// <summary>
    /// Deserializes a base64-encoded Avro binary value into an object.
    /// </summary>
    /// <param name="base64Value">The base64-encoded Avro binary data.</param>
    /// <param name="schema">The Avro schema to use for deserialization.</param>
    /// <returns>The deserialized object.</returns>
    private object DeserializeAvroValue(string base64Value, Schema schema)
    {
        byte[] avroBytes = Convert.FromBase64String(base64Value);
        using var stream = new MemoryStream(avroBytes);
        var decoder = new BinaryDecoder(stream);
        var reader = new SpecificDatumReader<object>(schema, schema);
        var result = reader.Read(null!, decoder);
        return result ?? throw new InvalidOperationException("Failed to deserialize Avro value");
    }

    /// <summary>
    /// Serializes an object to JSON and writes it to the provided stream.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="response">The object to serialize.</param>
    /// <param name="responseStream">The stream to write the serialized data to.</param>
    public void Serialize<T>(T response, Stream responseStream)
    {
        using var writer = new StreamWriter(responseStream);
        writer.Write(JsonSerializer.Serialize(response, _jsonOptions));
    }
}