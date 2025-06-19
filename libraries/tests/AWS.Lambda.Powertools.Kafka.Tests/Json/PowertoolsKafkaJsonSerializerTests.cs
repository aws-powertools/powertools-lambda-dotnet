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

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Kafka.Json;

namespace AWS.Lambda.Powertools.Kafka.Tests.Json;

public class PowertoolsKafkaJsonSerializerTests
{
    [Fact]
    public void Deserialize_KafkaEventWithJsonPayload_DeserializesToCorrectType()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        string kafkaEventJson = File.ReadAllText("Json/kafka-json-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, JsonProduct>>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aws:kafka", result.EventSource);

        // Verify records were deserialized
        Assert.True(result.Records.ContainsKey("mytopic-0"));
        var records = result.Records["mytopic-0"];
        Assert.Equal(3, records.Count);

        // Verify first record's content
        var firstRecord = records[0];
        Assert.Equal("mytopic", firstRecord.Topic);
        Assert.Equal(0, firstRecord.Partition);
        Assert.Equal(15, firstRecord.Offset);
        Assert.Equal("recordKey", firstRecord.Key);

        // Verify deserialized JSON value
        var product = firstRecord.Value;
        Assert.Equal("product5", product.Name);
        Assert.Equal(12345, product.Id);
        Assert.Equal(45, product.Price);

        // Verify second record
        var secondRecord = records[1];
        var p2 = secondRecord.Value;
        Assert.Equal("product5", p2.Name);
        Assert.Equal(12345, p2.Id);
        Assert.Equal(45, p2.Price);

        // Verify third record
        var thirdRecord = records[2];
        var p3 = thirdRecord.Value;
        Assert.Equal("product5", p3.Name);
        Assert.Equal(12345, p3.Id);
        Assert.Equal(45, p3.Price);
    }

    [Fact]
    public void KafkaEvent_ImplementsIEnumerable_ForDirectIteration()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        string kafkaEventJson = File.ReadAllText("Json/kafka-json-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, JsonProduct>>(stream);

        // Assert - Test enumeration
        int count = 0;
        var products = new List<string>();

        // Directly iterate over ConsumerRecords
        foreach (var record in result)
        {
            count++;
            products.Add(record.Value.Name);
        }

        // Verify correct count and values
        Assert.Equal(3, count);
        Assert.Contains("product5", products);

        // Get first record directly through Linq extension
        var firstRecord = result.First();
        Assert.Equal("product5", firstRecord.Value.Name);
        Assert.Equal(12345, firstRecord.Value.Id);
    }

    [Fact]
    public void Primitive_Deserialization()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        string kafkaEventJson =
            CreateKafkaEvent(Convert.ToBase64String("MyKey"u8.ToArray()),
                Convert.ToBase64String("Myvalue"u8.ToArray()));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, string>>(stream);
        var firstRecord = result.First();
        Assert.Equal("Myvalue", firstRecord.Value);
        Assert.Equal("MyKey", firstRecord.Key);
    }

    [Fact]
    public void DeserializeComplexKey_StandardJsonDeserialization_Works()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        var complexObject = new { Name = "Test", Id = 123 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(complexObject));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(jsonBytes),
            valueValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("test"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<Dictionary<string, object>, string>>(stream);

        // Assert
        var record = result.First();
        Assert.NotNull(record.Key);
        Assert.Equal("Test", record.Key["Name"].ToString());
        Assert.Equal(123, int.Parse(record.Key["Id"].ToString()));
    }

    [Fact]
    public void DeserializeComplexKey_WithSerializerContext_UsesContext()
    {
        // Arrange
        // Create custom context
        var options = new JsonSerializerOptions();
        var context = new TestJsonSerializerContext(options);
        var serializer = new PowertoolsKafkaJsonSerializer(context);

        // Create test data with the registered type
        var testModel = new TestModel { Name = "TestFromContext", Value = 456 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testModel));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(jsonBytes),
            valueValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("test"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<TestModel, string>>(stream);

        // Assert
        var record = result.First();
        Assert.NotNull(record.Key);
        Assert.Equal("TestFromContext", record.Key.Name);
        Assert.Equal(456, record.Key.Value);
    }

    [Fact]
    public void DeserializeComplexKey_WhenDeserializationFails_ReturnsNull()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        // Invalid JSON
        byte[] invalidBytes = { 0xDE, 0xAD, 0xBE, 0xEF };

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(invalidBytes),
            valueValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("test"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        // This shouldn't throw but return a record with null key
        var result = serializer.Deserialize<ConsumerRecords<TestModel, string>>(stream);

        // Assert
        var record = result.First();
        Assert.Null(record.Key);
    }

    [Fact]
    public void DeserializeComplexValue_WithSerializerContext_UsesContext()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        var context = new TestJsonSerializerContext(options);
        var serializer = new PowertoolsKafkaJsonSerializer(context);

        // Create test data with the registered type
        var testModel = new TestModel { Name = "ValueFromContext", Value = 789 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testModel));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(jsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, TestModel>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.NotNull(record.Value);
        Assert.Equal("ValueFromContext", record.Value.Name);
        Assert.Equal(789, record.Value.Value);
    }
    
    [Fact]
    public void DeserializeComplexValue_WithContextButNoTypeInfo_UsesFallback()
    {
        // Arrange - create context without registering Dictionary<,> type
        var options = new JsonSerializerOptions();
        var context = new TestJsonSerializerContext(options);
        var serializer = new PowertoolsKafkaJsonSerializer(context);

        // Create test data with an unregistered type
        var dictData = new Dictionary<string, int> { ["test"] = 123, ["value"] = 456 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dictData));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(jsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act - should use fallback deserialization
        var result = serializer.Deserialize<ConsumerRecords<string, Dictionary<string, int>>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.NotNull(record.Value);
        Assert.Equal(2, record.Value.Count);
        Assert.Equal(123, record.Value["test"]);
        Assert.Equal(456, record.Value["value"]);
    }

    [Fact]
    public void DeserializeComplexValue_WithInvalidJson_ReturnsNullForReferenceTypes()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        byte[] invalidJsonBytes = Encoding.UTF8.GetBytes("{ this is not valid json }");

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(invalidJsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, TestModel>>(stream);

        // Assert - value should be null because it's a reference type
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.Null(record.Value);
    }

    [Fact]
    public void DeserializeComplexValue_WithInvalidJson_ReturnsDefaultForValueTypes()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        byte[] invalidJsonBytes = Encoding.UTF8.GetBytes("{ bad json");

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(invalidJsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, ValueTypeProduct>>(stream);

        // Assert - value should be default because it's a value type
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.Equal(0, record.Value.Id);
        Assert.Equal(default, record.Value.Name);
        Assert.Equal(0, record.Value.Price);
    }
    
    [Fact]
    public void DeserializeComplexValue_WithCustomJsonOptions_RespectsOptions()
    {
        // Arrange - create custom options with different naming policy
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false // Force exact case match
        };
        var serializer = new PowertoolsKafkaJsonSerializer(options);

        // Create test data with camelCase property names
        var jsonBytes = Encoding.UTF8.GetBytes(@"{""id"":999,""name"":""camelCase"",""price"":29.99}");

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(jsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, JsonProduct>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal(999, record.Value.Id);
        Assert.Equal("camelCase", record.Value.Name);
        Assert.Equal(29.99m, record.Value.Price);
    }
    
    [Fact]
    public void DeserializeComplexValue_WithEmptyData_ReturnsNullOrDefault()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        // Empty JSON data
        byte[] emptyBytes = Array.Empty<byte>();

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(emptyBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, JsonProduct>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.Null(record.Value); // Should be null for empty input
    }
    
    [Fact]
    public void DeserializeComplexValue_WithContextAndNullResult_ReturnsNull()
    {
        // Arrange - create a context with JsonNullHandling.Include
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IgnoreNullValues = false
        };
        var context = new TestJsonSerializerContext(options);
        var serializer = new PowertoolsKafkaJsonSerializer(context);

        // JSON that explicitly sets the value to null
        var jsonBytes = Encoding.UTF8.GetBytes("null");

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(jsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, TestModel>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.Null(record.Value);
    }
    

    /// <summary>
    /// Helper method to create Kafka event JSON with specified key and value in base64 format
    /// </summary>
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
public partial class TestJsonSerializerContext : JsonSerializerContext
{
}

public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public record JsonProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public struct ValueTypeProduct
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
