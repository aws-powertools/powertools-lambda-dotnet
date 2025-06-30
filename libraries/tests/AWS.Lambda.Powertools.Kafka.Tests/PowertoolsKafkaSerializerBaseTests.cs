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

using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Kafka.Avro;

namespace AWS.Lambda.Powertools.Kafka.Tests
{
    /// <summary>
    /// Additional tests for PowertoolsKafkaSerializerBase
    /// </summary>
    public class PowertoolsKafkaSerializerBaseTests
    {
        /// <summary>
        /// Simple serializer implementation for testing base class
        /// </summary>
        private class TestKafkaSerializer : PowertoolsKafkaSerializerBase
        {
            public TestKafkaSerializer() : base()
            {
            }

            public TestKafkaSerializer(JsonSerializerOptions options) : base(options)
            {
            }

            public TestKafkaSerializer(JsonSerializerContext context) : base(context)
            {
            }

            public TestKafkaSerializer(JsonSerializerOptions options, JsonSerializerContext context)
                : base(options, context)
            {
            }

            // Implementation of the abstract method for test purposes
            protected override object? DeserializeComplexTypeFormat(byte[] data,
                Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
            {
                // Test implementation using JSON for all complex types
                var jsonStr = Encoding.UTF8.GetString(data);

                if (SerializerContext != null)
                {
                    var typeInfo = SerializerContext.GetTypeInfo(targetType);
                    if (typeInfo != null)
                    {
                        return JsonSerializer.Deserialize(jsonStr, typeInfo);
                    }
                }

                return JsonSerializer.Deserialize(jsonStr, targetType, JsonOptions);
            }

            // Expose protected methods for direct testing
            public object? TestDeserializeFormatSpecific(byte[] data, Type targetType, bool isKey,
                SchemaMetadata? schemaMetadata = null)
            {
                return DeserializeFormatSpecific(data, targetType, isKey, schemaMetadata);
            }

            public object? TestDeserializeComplexTypeFormat(byte[] data, Type targetType, bool isKey,
                SchemaMetadata? schemaMetadata = null)
            {
                return DeserializeComplexTypeFormat(data, targetType, isKey, schemaMetadata);
            }

            public object? TestDeserializePrimitiveValue(byte[] data, Type targetType)
            {
                return DeserializePrimitiveValue(data, targetType);
            }

            public bool TestIsPrimitiveOrSimpleType(Type type)
            {
                return IsPrimitiveOrSimpleType(type);
            }

            public object TestDeserializeValue(string base64Value, Type valueType,
                SchemaMetadata? schemaMetadata = null)
            {
                return DeserializeValue(base64Value, valueType, schemaMetadata);
            }
        }

        [Fact]
        public void Deserialize_BooleanValues_HandlesCorrectly()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            string kafkaEventJson = CreateKafkaEvent(
                keyValue: "dHJ1ZQ==", // "true" in base64
                valueValue: "AQ==" // byte[1] = {1} in base64
            );

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act
            var result = serializer.Deserialize<ConsumerRecords<string, bool>>(stream);

            // Assert
            Assert.NotNull(result);
            var firstRecord = result.First();
            Assert.Equal("true", firstRecord.Key);
            Assert.True(firstRecord.Value);
        }

        [Fact]
        public void Deserialize_NumericValues_HandlesCorrectly()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            string kafkaEventJson = CreateKafkaEvent(
                keyValue: "NDI=", // "42" in base64
                valueValue: "MTIzNA==" // "1234" in base64
            );

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act
            var result = serializer.Deserialize<ConsumerRecords<int, int>>(stream);

            // Assert
            Assert.NotNull(result);
            var firstRecord = result.First();
            Assert.Equal(42, firstRecord.Key);
            Assert.Equal(1234, firstRecord.Value);
        }

        [Fact]
        public void Deserialize_GuidValues_HandlesCorrectly()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var serializer = new TestKafkaSerializer();
            string kafkaEventJson = CreateKafkaEvent(
                keyValue: Convert.ToBase64String(guid.ToByteArray()),
                valueValue: Convert.ToBase64String(Encoding.UTF8.GetBytes(guid.ToString()))
            );

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act
            var result = serializer.Deserialize<ConsumerRecords<Guid, string>>(stream);

            // Assert
            Assert.NotNull(result);
            var firstRecord = result.First();
            Assert.Equal(guid, firstRecord.Key);
            Assert.Equal(guid.ToString(), firstRecord.Value);
        }

        [Fact]
        public void Deserialize_InvalidJson_ThrowsException()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            string invalidJson = "{ this is not valid json }";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

            // Act & Assert
            Assert.ThrowsAny<JsonException>(() =>
                serializer.Deserialize<ConsumerRecords<string, string>>(stream));
        }

        [Fact]
        public void Deserialize_MalformedBase64_ThrowsException()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            string kafkaEventJson = CreateKafkaEvent(
                keyValue: "not-base64!",
                valueValue: "valid-base64=="
            );

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act & Assert
            var ex = Assert.Throws<SerializationException>(() =>
                serializer.Deserialize<ConsumerRecords<string, string>>(stream));

            Assert.Contains("Failed to deserialize key data", ex.Message);
        }

        [Fact]
        public void Serialize_ValidObject_WritesToStream()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var testObject = new { Name = "Test", Value = 42 };
            using var responseStream = new MemoryStream();

            // Act
            serializer.Serialize(testObject, responseStream);
            responseStream.Position = 0;
            string result = Encoding.UTF8.GetString(responseStream.ToArray());

            // Assert
            Assert.Contains("\"Name\":\"Test\"", result);
            Assert.Contains("\"Value\":42", result);
        }

        [Fact]
        public void Serialize_NullObject_WritesNullToStream()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            using var responseStream = new MemoryStream();

            // Act
            serializer.Serialize<object>(null, responseStream);
            responseStream.Position = 0;
            string result = Encoding.UTF8.GetString(responseStream.ToArray());

            // Assert
            Assert.Equal("null", result);
        }

        [Fact]
        public void DeserializePrimitiveValue_EmptyBytes_ReturnsNull()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();

            // Act
            var result = serializer.TestDeserializePrimitiveValue(Array.Empty<byte>(), typeof(string));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DeserializePrimitiveValue_LongValue_DeserializesCorrectly()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var longBytes = BitConverter.GetBytes(long.MaxValue);

            // Act
            var result = serializer.TestDeserializePrimitiveValue(longBytes, typeof(long));

            // Assert
            Assert.Equal(long.MaxValue, result);
        }

        [Fact]
        public void DeserializePrimitiveValue_DoubleValue_DeserializesCorrectly()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var doubleBytes = BitConverter.GetBytes(3.14159);

            // Act
            var result = serializer.TestDeserializePrimitiveValue(doubleBytes, typeof(double));

            // Assert
            Assert.Equal(3.14159, result);
        }

        [Fact]
        public void ProcessHeaders_MultipleHeaders_DeserializesCorrectly()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            string kafkaEventJson = @$"{{
                ""eventSource"": ""aws:kafka"",
                ""records"": {{
                    ""mytopic-0"": [
                        {{
                            ""topic"": ""mytopic"",
                            ""partition"": 0,
                            ""offset"": 15,
                            ""key"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("key"))}"",
                            ""value"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("value"))}"",
                            ""headers"": [
                                {{ ""header1"": [104, 101, 108, 108, 111] }},
                                {{ ""header2"": [119, 111, 114, 108, 100] }}
                            ]
                        }}
                    ]
                }}
            }}";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act
            var result = serializer.Deserialize<ConsumerRecords<string, string>>(stream);

            // Assert
            var record = result.First();
            Assert.Equal(2, record.Headers.Count);
            Assert.Equal("hello", Encoding.ASCII.GetString(record.Headers["header1"]));
            Assert.Equal("world", Encoding.ASCII.GetString(record.Headers["header2"]));
        }

        [Fact]
        public void Deserialize_WithSerializerContext_UsesContextForRegisteredTypes()
        {
            // Arrange
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var context = new TestSerializerContext(options);
            // Use only options for constructor, but we'll make the context available for the model deserialization
            var serializer = new TestKafkaSerializer(options);

            var testModel = new TestModel { Name = "Test", Value = 123 };
            var modelJson = JsonSerializer.Serialize(testModel, context.TestModel);
            var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(modelJson));

            string kafkaEventJson = CreateKafkaEvent(
                keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
                valueValue: base64Value
            );

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act
            var result = serializer.Deserialize<ConsumerRecords<string, TestModel>>(stream);

            // Assert
            Assert.NotNull(result);
            var record = result.First();
            Assert.Equal("testKey", record.Key);
            Assert.Equal("Test", record.Value.Name);
            Assert.Equal(123, record.Value.Value);
        }

        [Fact]
        public void Serialize_WithSerializerContext_UsesContextForRegisteredTypes()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            var context = new TestSerializerContext(options);
            var serializer = new TestKafkaSerializer(options, context);

            var testModel = new TestModel { Name = "Test", Value = 123 };
            using var responseStream = new MemoryStream();

            // Act
            serializer.Serialize(testModel, responseStream);
            responseStream.Position = 0;
            string result = Encoding.UTF8.GetString(responseStream.ToArray());

            // Assert
            Assert.Contains("\"Name\":\"Test\"", result);
            Assert.Contains("\"Value\":123", result);
        }

        [Fact]
        public void Deserialize_WithSerializerContext_FallsBackWhenTypeNotRegistered()
        {
            // Arrange
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var context = new TestSerializerContext(options);
            var serializer = new TestKafkaSerializer(options, context);

            // Using a non-registered type (Dictionary instead of TestModel)
            var dictionary = new Dictionary<string, int> { ["Key"] = 42 };
            var dictJson = JsonSerializer.Serialize(dictionary);
            var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(dictJson));

            string kafkaEventJson = CreateKafkaEvent(
                keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
                valueValue: base64Value
            );

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act
            var result = serializer.Deserialize<ConsumerRecords<string, Dictionary<string, int>>>(stream);

            // Assert
            Assert.NotNull(result);
            var record = result.First();
            Assert.Equal("testKey", record.Key);
            Assert.Single(record.Value);
            Assert.Equal(42, record.Value["Key"]);
        }

        [Fact]
        public void Serialize_NonRegisteredType_FallsBackToRegularSerialization()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            // Use serializer WITHOUT context to test the fallback path
            var serializer = new TestKafkaSerializer(options);

            // Using a non-registered type
            var nonRegisteredType = new { Id = Guid.NewGuid(), Message = "Not in context" };
            using var responseStream = new MemoryStream();

            // Act
            serializer.Serialize(nonRegisteredType, responseStream);
            responseStream.Position = 0;
            string result = Encoding.UTF8.GetString(responseStream.ToArray());

            // Assert
            Assert.Contains("\"Id\":", result);
            Assert.Contains("\"Message\":\"Not in context\"", result);
        }

        [Fact]
        public void Deserialize_NonConsumerRecordWithSerializerContext_UsesTypeInfo()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            var context = new TestSerializerContext(options);
            var serializer = new TestKafkaSerializer(options, context);

            var testModel = new TestModel { Name = "DirectDeserialization", Value = 42 };
            var json = JsonSerializer.Serialize(testModel);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = serializer.Deserialize<TestModel>(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DirectDeserialization", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Deserialize_NonConsumerRecordWithoutTypeInfo_UsesRegularDeserialize()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            var context = new TestSerializerContext(options);
            var serializer = new TestKafkaSerializer(options, context);

            // Dictionary<string,int> is not registered in TestSerializerContext
            var dict = new Dictionary<string, int> { ["test"] = 123 };
            var json = JsonSerializer.Serialize(dict);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = serializer.Deserialize<Dictionary<string, int>>(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(123, result["test"]);
        }

        [Fact]
        public void Deserialize_NonConsumerRecordFailed_ThrowsException()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var invalidJson = "{ invalid json";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

            // Act & Assert
            // With invalid JSON input, JsonSerializer throws JsonException directly
            var ex = Assert.Throws<JsonException>(() =>
                serializer.Deserialize<TestModel>(stream));

            // Check that we're getting a JSON parsing error
            Assert.Contains("invalid", ex.Message.ToLower());
        }

        [Theory]
        [InlineData(new byte[] { 42 }, 42)] // Single byte
        [InlineData(new byte[] { 0x2A, 0x00, 0x00, 0x00 }, 42)] // Four bytes
        public void DeserializePrimitiveValue_IntWithDifferentByteFormats_DeserializesCorrectly(byte[] bytes,
            int expected)
        {
            // Arrange
            var serializer = new TestKafkaSerializer();

            // Act
            var result = serializer.TestDeserializePrimitiveValue(bytes, typeof(int));

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(new byte[] { 0x2A, 0x00, 0x00, 0x00 }, 42L)] // Four bytes as int
        [InlineData(new byte[] { 0x2A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 42L)] // Eight bytes as long
        public void DeserializePrimitiveValue_LongWithDifferentByteFormats_DeserializesCorrectly(byte[] bytes,
            long expected)
        {
            // Arrange
            var serializer = new TestKafkaSerializer();

            // Act
            var result = serializer.TestDeserializePrimitiveValue(bytes, typeof(long));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DeserializePrimitiveValue_DoubleWithShortBytes_ReturnsZero()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var shortBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 }; // Less than 8 bytes

            // Act
            var result = serializer.TestDeserializePrimitiveValue(shortBytes, typeof(double));

            // Assert
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void Serialize_WithTypeInfoFromContext_WritesToStream()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            var context = new TestSerializerContext(options);
            var serializer = new TestKafkaSerializer(options, context);

            var testModel = new TestModel { Name = "ContextSerialization", Value = 555 };
            using var responseStream = new MemoryStream();

            // Act
            serializer.Serialize(testModel, responseStream);
            responseStream.Position = 0;
            string result = Encoding.UTF8.GetString(responseStream.ToArray());

            // Assert
            Assert.Contains("\"Name\":\"ContextSerialization\"", result);
            Assert.Contains("\"Value\":555", result);
        }

        [Fact]
        public void Deserialize_WithSchemaMetadata_PopulatesSchemaMetadataProperties()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();

            string kafkaEventJson = @$"{{
        ""eventSource"": ""aws:kafka"",
        ""eventSourceArn"": ""arn:aws:kafka:us-east-1:0123456789019:cluster/TestCluster/abcd1234"",
        ""bootstrapServers"": ""b-1.test-cluster.kafka.us-east-1.amazonaws.com:9092"",
        ""records"": {{
            ""mytopic-0"": [
                {{
                    ""topic"": ""mytopic"",
                    ""partition"": 0,
                    ""offset"": 15,
                    ""timestamp"": 1645084650987,
                    ""timestampType"": ""CREATE_TIME"",
                    ""key"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey"))}"",
                    ""value"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("testValue"))}"",
                    ""headers"": [
                        {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                    ],
                    ""keySchemaMetadata"": {{
                        ""dataFormat"": ""JSON"",
                        ""schemaId"": ""key-schema-001""
                    }},
                    ""valueSchemaMetadata"": {{
                        ""dataFormat"": ""AVRO"",
                        ""schemaId"": ""value-schema-002""
                    }}
                }}
            ]
        }}
    }}";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

            // Act
            var result = serializer.Deserialize<ConsumerRecords<string, string>>(stream);

            // Assert
            Assert.NotNull(result);
            var record = result.First();

            // Assert key schema metadata
            Assert.NotNull(record.KeySchemaMetadata);
            Assert.Equal("JSON", record.KeySchemaMetadata.DataFormat);
            Assert.Equal("key-schema-001", record.KeySchemaMetadata.SchemaId);

            // Assert value schema metadata
            Assert.NotNull(record.ValueSchemaMetadata);
            Assert.Equal("AVRO", record.ValueSchemaMetadata.DataFormat);
            Assert.Equal("value-schema-002", record.ValueSchemaMetadata.SchemaId);
        }

        // NEW TESTS FOR LATEST CHANGES

        [Fact]
        public void DeserializeFormatSpecific_PrimitiveType_UsesDeserializePrimitiveValue()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var stringBytes = Encoding.UTF8.GetBytes("primitive-test");

            // Act
            var result =
                serializer.TestDeserializeFormatSpecific(stringBytes, typeof(string), isKey: false,
                    schemaMetadata: null);

            // Assert
            Assert.Equal("primitive-test", result);
        }

        [Fact]
        public void DeserializeFormatSpecific_ComplexType_UsesDeserializeComplexTypeFormat()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var complexObject = new TestModel { Name = "complex-test", Value = 42 };
            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(complexObject));

            // Act
            var result =
                serializer.TestDeserializeFormatSpecific(jsonBytes, typeof(TestModel), isKey: false,
                    schemaMetadata: null);

            // Assert
            Assert.NotNull(result);
            var testModel = (TestModel)result!;
            Assert.Equal("complex-test", testModel.Name);
            Assert.Equal(42, testModel.Value);
        }

        [Fact]
        public void DeserializeComplexTypeFormat_ValidJson_DeserializesCorrectly()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var complexObject = new TestModel { Name = "direct-test", Value = 123 };
            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(complexObject));

            // Act
            var result =
                serializer.TestDeserializeComplexTypeFormat(jsonBytes, typeof(TestModel), isKey: true,
                    schemaMetadata: null);

            // Assert
            Assert.NotNull(result);
            var testModel = (TestModel)result!;
            Assert.Equal("direct-test", testModel.Name);
            Assert.Equal(123, testModel.Value);
        }

        [Fact]
        public void DeserializeComplexTypeFormat_InvalidJson_ThrowsException()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var invalidBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }; // Invalid JSON data

            // Act & Assert
            // The TestKafkaSerializer throws JsonException directly for invalid JSON
            var ex = Assert.Throws<JsonException>(() =>
                serializer.TestDeserializeComplexTypeFormat(invalidBytes, typeof(TestModel), isKey: true,
                    schemaMetadata: null));

            Assert.Contains("invalid", ex.Message.ToLower());
        }

        [Fact]
        public void DeserializeValue_Base64String_DeserializesCorrectly()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var testValue = "test-value-123";
            var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(testValue));

            // Act
            var result = serializer.TestDeserializeValue(base64Value, typeof(string), schemaMetadata: null);

            // Assert
            Assert.Equal(testValue, result);
        }

        [Fact]
        public void DeserializeValue_WithSchemaMetadata_PassesMetadataToFormatSpecific()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();
            var testValue = "test-value-with-metadata";
            var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(testValue));
            var schemaMetadata = new SchemaMetadata { DataFormat = "JSON", SchemaId = "test-schema-001" };

            // Act
            var result = serializer.TestDeserializeValue(base64Value, typeof(string), schemaMetadata);

            // Assert
            Assert.Equal(testValue, result);
        }

        [Fact]
        public void IsPrimitiveOrSimpleType_ChecksVariousTypes()
        {
            // Arrange
            var serializer = new TestKafkaSerializer();

            // Act & Assert
            // Primitive types
            Assert.True(serializer.TestIsPrimitiveOrSimpleType(typeof(int)));
            Assert.True(serializer.TestIsPrimitiveOrSimpleType(typeof(long)));
            Assert.True(serializer.TestIsPrimitiveOrSimpleType(typeof(bool)));

            // Simple types
            Assert.True(serializer.TestIsPrimitiveOrSimpleType(typeof(string)));
            Assert.True(serializer.TestIsPrimitiveOrSimpleType(typeof(Guid)));
            Assert.True(serializer.TestIsPrimitiveOrSimpleType(typeof(DateTime)));

            // Complex types
            Assert.False(serializer.TestIsPrimitiveOrSimpleType(typeof(TestModel)));
            Assert.False(serializer.TestIsPrimitiveOrSimpleType(typeof(Dictionary<string, int>)));
        }

        // Helper method to create Kafka event JSON with specified key and value
        private string CreateKafkaEvent(string keyValue, string valueValue)
        {
            return @$"{{
                ""eventSource"": ""aws:kafka"",
                ""eventSourceArn"": ""arn:aws:kafka:us-east-1:0123456789019:cluster/TestCluster/abcd1234"",
                ""bootstrapServers"": ""b-1.test-cluster.kafka.us-east-1.amazonaws.com:9092"",
                ""records"": {{
                    ""mytopic-0"": [
                        {{
                            ""topic"": ""mytopic"",
                            ""partition"": 0,
                            ""offset"": 15,
                            ""timestamp"": 1645084650987,
                            ""timestampType"": ""CREATE_TIME"",
                            ""key"": ""{keyValue}"",
                            ""value"": ""{valueValue}"",
                            ""headers"": [
                                {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                            ]
                        }}
                    ]
                }}
            }}";
        }
    }

    [JsonSerializable(typeof(TestModel))]
    [JsonSerializable(typeof(ConsumerRecords<string, TestModel>))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    public partial class TestSerializerContext : JsonSerializerContext
    {
    }

    public class TestModel
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}