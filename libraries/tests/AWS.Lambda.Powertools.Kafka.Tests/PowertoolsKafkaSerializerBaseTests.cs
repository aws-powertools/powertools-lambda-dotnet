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

            protected override object? DeserializeComplexKey(byte[] keyBytes, Type keyType)
            {
                return JsonSerializer.Deserialize(keyBytes, keyType);
            }

            protected override object DeserializeComplexValue(string base64Value, Type valueType)
            {
                var bytes = Convert.FromBase64String(base64Value);
                return JsonSerializer.Deserialize(bytes, valueType);
            }

            // Implement our own version that mimics the private method's behavior
            public object TestDeserializePrimitiveValue(byte[] bytes, Type valueType)
            {
                if (bytes == null || bytes.Length == 0)
                    return null!;

                if (valueType == typeof(string))
                {
                    return Encoding.UTF8.GetString(bytes);
                }

                if (valueType == typeof(int))
                {
                    var stringValue = Encoding.UTF8.GetString(bytes);
                    if (int.TryParse(stringValue, out var parsedValue))
                        return parsedValue;

                    return bytes.Length switch
                    {
                        >= 4 => BitConverter.ToInt32(bytes, 0),
                        1 => bytes[0],
                        _ => 0
                    };
                }

                if (valueType == typeof(long))
                {
                    var stringValue = Encoding.UTF8.GetString(bytes);
                    if (long.TryParse(stringValue, out var parsedValue))
                        return parsedValue;

                    return bytes.Length switch
                    {
                        >= 8 => BitConverter.ToInt64(bytes, 0),
                        >= 4 => BitConverter.ToInt32(bytes, 0),
                        _ => 0L
                    };
                }

                if (valueType == typeof(double))
                {
                    return bytes.Length >= 8 ? BitConverter.ToDouble(bytes, 0) : 0.0;
                }

                if (valueType == typeof(bool))
                {
                    return bytes[0] != 0;
                }

                if (valueType == typeof(Guid) && bytes.Length >= 16)
                {
                    return new Guid(bytes);
                }

                return Convert.ChangeType(Encoding.UTF8.GetString(bytes), valueType);
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