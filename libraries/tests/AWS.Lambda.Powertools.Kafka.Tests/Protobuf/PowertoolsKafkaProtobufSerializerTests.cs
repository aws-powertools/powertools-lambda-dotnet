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
    public void DeserializeComplexKey_WhenAllDeserializationMethodsFail_ReturnsException()
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
        var message = Assert.Throws<SerializationException>(() => serializer.Deserialize<ConsumerRecords<TestModel, string>>(stream));
        Assert.Contains("Failed to deserialize key data: Failed to deserialize", message.Message);
    }

    [Fact]
    public void Deserialize_ConfluentMessageIndexFormats_AllFormatsDeserializeCorrectly()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        string kafkaEventJson = File.ReadAllText("Protobuf/kafka-protobuf-confluent-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<int, ProtobufProduct>>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aws:kafka", result.EventSource);

        // Verify records
        Assert.True(result.Records.ContainsKey("mytopic-0"));
        var records = result.Records["mytopic-0"];
        Assert.Equal(3, records.Count);

        // Verify all records have been deserialized correctly (all should have the same content)
        foreach (var record in records)
        {
            Assert.Equal("Laptop", record.Value.Name);
            Assert.Equal(1001, record.Value.Id);
            Assert.Equal(999.99, record.Value.Price);
        }
    }

    [Theory]
    [InlineData("COkHEgZMYXB0b3AZUrgehes/j0A=", "Standard Protobuf")] // Standard protobuf
    [InlineData("AAjpBxIGTGFwdG9wGVK4HoXrP49A", "Single Index")] // Confluent with single 0 index
    [InlineData("AgEACOkHEgZMYXB0b3AZUrgehes/j0A=", "Complex Index")] // Confluent with index array [1, 0]
    public void Deserialize_SpecificConfluentFormats_EachFormatDeserializesCorrectly(string base64Value, string testCase)
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        string kafkaEventJson = CreateKafkaEvent("NDI=", base64Value); // Key is 42 in base64
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<ConsumerRecords<int, ProtobufProduct>>(stream);

        // Assert
        var record = result.First();
        Assert.NotNull(record);
        Assert.Equal(42, record.Key); // Key should be 42

        // Value should be the same regardless of message index format
        Assert.Equal("Laptop", record.Value.Name);
        Assert.Equal(1001, record.Value.Id);
        Assert.Equal(999.99, record.Value.Price);
    }

    [Fact]
    public void Deserialize_MessageIndexWithCorruptData_HandlesError()
    {
        // Arrange - Create invalid message index data (starts with 5 but doesn't have 5 entries)
        byte[] invalidData = [5, 1, 2]; // Claims to have 5 entries but only has 2
        string kafkaEventJson = CreateKafkaEvent("NDI=", Convert.ToBase64String(invalidData));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));
        var serializer = new PowertoolsKafkaProtobufSerializer();

        // Act & Assert
        var ex = Assert.Throws<SerializationException>(() => 
            serializer.Deserialize<ConsumerRecords<int, ProtobufProduct>>(stream));
        
        // Verify the exception message contains useful information
        Assert.Contains("Failed to deserialize value data:", ex.Message);
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