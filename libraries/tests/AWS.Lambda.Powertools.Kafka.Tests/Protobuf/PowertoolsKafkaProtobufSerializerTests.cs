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
using AWS.Lambda.Powertools.Kafka.Protobuf;
using TestKafka;

namespace AWS.Lambda.Powertools.Kafka.Tests.Protobuf;

public class PowertoolsKafkaProtobufSerializerTests
{
    [Fact]
    public void Deserialize_KafkaEventWithProtobufPayload_DeserializesToCorrectType()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        string kafkaEventJson = File.ReadAllText("Protobuf/kafka-protobuf-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<int, ProtobufProduct>>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aws:kafka", result.EventSource);

        // Verify records were deserialized
        Assert.True(result.Records.ContainsKey("mytopic-0"));
        var records = result.Records["mytopic-0"];
        Assert.Equal(3, records.Count); // Fixed to expect 3 records instead of 1

        // Verify first record's content
        var firstRecord = records[0];
        Assert.Equal("mytopic", firstRecord.Topic);
        Assert.Equal(0, firstRecord.Partition);
        Assert.Equal(15, firstRecord.Offset);
        Assert.Equal(42, firstRecord.Key);

        // Verify deserialized Protobuf value
        var product = firstRecord.Value;
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(1001, product.Id);
        Assert.Equal(999.99, product.Price);

        // Verify second record
        var secondRecord = records[1];
        var smartphone = secondRecord.Value;
        Assert.Equal("Smartphone", smartphone.Name);
        Assert.Equal(1002, smartphone.Id);
        Assert.Equal(599.99, smartphone.Price);

        // Verify third record
        var thirdRecord = records[2];
        var headphones = thirdRecord.Value;
        Assert.Equal("Headphones", headphones.Name);
        Assert.Equal(1003, headphones.Id);
        Assert.Equal(149.99, headphones.Price);
    }

    [Fact]
    public void KafkaEvent_ImplementsIEnumerable_ForDirectIteration()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        string kafkaEventJson = File.ReadAllText("Protobuf/kafka-protobuf-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<int, ProtobufProduct>>(stream);

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
        Assert.Contains("Laptop", products);
        Assert.Contains("Smartphone", products);
        Assert.Contains("Headphones", products);

        // Get first record directly through Linq extension
        var firstRecord = result.First();
        Assert.Equal("Laptop", firstRecord.Value.Name);
        Assert.Equal(1001, firstRecord.Value.Id);
    }

    [Fact]
    public void Primitive_Deserialization()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
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
    public void DeserializeComplexKey_WithoutProtobufParser_FallsBackToJson()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        var complexObject = new { Name = "Test", Id = 123 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(complexObject));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(jsonBytes),
            valueValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("test"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        // Use Dictionary<string, object> as key type since it doesn't have a Protobuf parser
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
        var context = new TestProtobufSerializerContext(options);
        var serializer = new PowertoolsKafkaProtobufSerializer(context);

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
    public void DeserializeComplexKey_WhenAllDeserializationMethodsFail_ReturnsNull()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        // Invalid JSON and not Protobuf binary
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
public partial class TestProtobufSerializerContext : JsonSerializerContext
{
}