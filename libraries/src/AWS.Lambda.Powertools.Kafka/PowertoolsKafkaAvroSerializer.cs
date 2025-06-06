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

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(ConsumerRecords<>))
        {
            var payloadType = targetType.GetGenericArguments()[0];
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Create the correctly typed instance
            var typedEvent = Activator.CreateInstance(targetType);

            // Set basic properties
            if (root.TryGetProperty("eventSource", out var eventSource))
                targetType.GetProperty("EventSource",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(typedEvent, eventSource.GetString());

            if (root.TryGetProperty("eventSourceArn", out var eventSourceArn))
                targetType.GetProperty("EventSourceArn").SetValue(typedEvent, eventSourceArn.GetString());

            if (root.TryGetProperty("bootstrapServers", out var bootstrapServers))
                targetType.GetProperty("BootstrapServers").SetValue(typedEvent, bootstrapServers.GetString());

            // Get the schema for Avro deserialization
            Schema schema = GetAvroSchema(payloadType);

            // Create records dictionary with correct generic type
            var dictType = typeof(Dictionary<,>).MakeGenericType(
                typeof(string),
                typeof(List<>).MakeGenericType(typeof(ConsumerRecord<>).MakeGenericType(payloadType))
            );
            var records = Activator.CreateInstance(dictType);
            var dictAddMethod = dictType.GetMethod("Add");

            if (root.TryGetProperty("records", out var recordsElement))
            {
                foreach (var topicPartition in recordsElement.EnumerateObject())
                {
                    string topicName = topicPartition.Name;

                    // Create list of records with correct generic type
                    var listType =
                        typeof(List<>).MakeGenericType(typeof(ConsumerRecord<>).MakeGenericType(payloadType));
                    var recordsList = Activator.CreateInstance(listType);
                    var listAddMethod = listType.GetMethod("Add");

                    foreach (var recordElement in topicPartition.Value.EnumerateArray())
                    {
                        // Create record instance of correct type
                        var recordType = typeof(ConsumerRecord<>).MakeGenericType(payloadType);
                        var record = Activator.CreateInstance(recordType);

                        // Set basic properties
                        SetProperty(recordType, record, "Topic", recordElement, "topic");
                        SetProperty(recordType, record, "Partition", recordElement, "partition");
                        SetProperty(recordType, record, "Offset", recordElement, "offset");
                        SetProperty(recordType, record, "Timestamp", recordElement, "timestamp");
                        SetProperty(recordType, record, "TimestampType", recordElement, "timestampType");

                        // Handle key - base64 decode if present
                        if (recordElement.TryGetProperty("key", out var keyElement) &&
                            keyElement.ValueKind == JsonValueKind.String)
                        {
                            string base64Key = keyElement.GetString();
                            recordType.GetProperty("Key").SetValue(record, base64Key);

                            // Base64 decode the key
                            if (!string.IsNullOrEmpty(base64Key))
                            {
                                try
                                {
                                    byte[] keyBytes = Convert.FromBase64String(base64Key);
                                    string decodedKey = Encoding.UTF8.GetString(keyBytes);
                                    recordType.GetProperty("Key").SetValue(record, decodedKey);
                                }
                                catch (Exception)
                                {
                                    // If decoding fails, leave it as is
                                }
                            }
                        }

                        // Handle Avro value
                        if (recordElement.TryGetProperty("value", out var value) &&
                            value.ValueKind == JsonValueKind.String)
                        {
                            string base64Value = value.GetString();
                            // recordType.GetProperty("Value").SetValue(record, base64Value);

                            // Deserialize Avro data
                            try
                            {
                                var deserializedValue = DeserializeAvroValue(base64Value, schema);
                                recordType.GetProperty("Value").SetValue(record, deserializedValue);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to deserialize Avro data: {ex.Message}", ex);
                            }
                        }

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
                                        // Convert integer array to byte array
                                        byte[] headerBytes = new byte[header.Value.GetArrayLength()];
                                        int i = 0;
                                        foreach (var byteVal in header.Value.EnumerateArray())
                                        {
                                            headerBytes[i++] = (byte)byteVal.GetInt32();
                                        }

                                        // Decode as UTF-8 string
                                        string headerValue = Encoding.UTF8.GetString(headerBytes);
                                        decodedHeaders[headerKey] = headerValue;
                                    }
                                }
                            }

                            var headersProperty = recordType.GetProperty("Headers",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (headersProperty != null)
                            {
                                headersProperty.SetValue(record, decodedHeaders);
                            }
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

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
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
        else if (propertyType == typeof(string)) value = jsonValue.GetString();
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

        return schemaField.GetValue(null) as Schema;
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
        return reader.Read(null, decoder);
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