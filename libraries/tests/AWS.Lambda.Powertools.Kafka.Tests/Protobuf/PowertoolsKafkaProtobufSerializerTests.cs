using System.Runtime.Serialization;
using System.Text;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using Com.Example.Protobuf;
using TestKafka;

#if DEBUG
using KafkaAlias = AWS.Lambda.Powertools.Kafka;
#else
using KafkaAlias = AWS.Lambda.Powertools.Kafka.Protobuf;
#endif

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
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<int, ProtobufProduct>>(stream);

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
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<int, ProtobufProduct>>(stream);

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
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<string, string>>(stream);
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
        var message =
            Assert.Throws<SerializationException>(() =>
                serializer.Deserialize<KafkaAlias.ConsumerRecords<TestModel, string>>(stream));
        Assert.Contains("Failed to deserialize key data: Unsupported", message.Message);
    }

    [Fact]
    public void Deserialize_Confluent_DeserializeCorrectly()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        string kafkaEventJson = File.ReadAllText("Protobuf/kafka-protobuf-confluent-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<int, UserProfile>>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aws:kafka", result.EventSource);

        // Verify records
        Assert.True(result.Records.ContainsKey("confluent_proto-0"));
        var records = result.Records["confluent_proto-0"];
        Assert.Equal(4, records.Count);

        // Verify all records have been deserialized correctly (all should have the same content)
        Assert.Equal("a8e40971-1552-420d-a7c9-b8982325702d", records[0].Value.UserId);
        Assert.Equal("Bob", records[0].Value.Name);
        Assert.Equal("bob@example.com", records[0].Value.Email);
        Assert.Equal("Seattle", records[0].Value.Address.City);
        Assert.Equal(28, records[0].Value.Age);
        
        Assert.Equal("4dcfc61b-3993-49c3-a04f-8a6c7aaf7881", records[1].Value.UserId);
        Assert.Equal("Bob", records[1].Value.Name);
        Assert.Equal("bob@example.com", records[1].Value.Email);
        Assert.Equal("Seattle", records[1].Value.Address.City);
        Assert.Equal(28, records[1].Value.Age);
        
        Assert.Equal("2a861628-0800-4b76-bd3f-6ecba7cd286c", records[2].Value.UserId);
        Assert.Equal("Bob", records[2].Value.Name);
        Assert.Equal("Seattle", records[2].Value.Address.City);
        Assert.Equal(28, records[2].Value.Age);
    }
    
    [Fact]
    public void Deserialize_Glue_DeserializeCorrectly()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        string kafkaEventJson = File.ReadAllText("Protobuf/kafka-protobuf-glue-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<int, UserProfile>>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aws:kafka", result.EventSource);

        // Verify records
        Assert.True(result.Records.ContainsKey("gsr_proto-0"));
        var records = result.Records["gsr_proto-0"];
        Assert.Equal(4, records.Count);

        // Verify all records have been deserialized correctly (all should have the same content)
        Assert.Equal("u859", records[0].Value.UserId);
        Assert.Equal("Alice", records[0].Value.Name);
        Assert.Equal("alice@example.com", records[0].Value.Email);
        Assert.Equal("dark", records[0].Value.Address.City);
        Assert.Equal(54, records[0].Value.Age);
        
        Assert.Equal("u809", records[1].Value.UserId);
        Assert.Equal("Alice", records[1].Value.Name);
        Assert.Equal("alice@example.com", records[1].Value.Email);
        Assert.Equal("dark", records[1].Value.Address.City);
        Assert.Equal(40, records[1].Value.Age);
        
        Assert.Equal("u453", records[2].Value.UserId);
        Assert.Equal("Alice", records[2].Value.Name);
        Assert.Equal("dark", records[2].Value.Address.City);
        Assert.Equal(74, records[2].Value.Age);
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
            serializer.Deserialize<KafkaAlias.ConsumerRecords<int, ProtobufProduct>>(stream));

        // Verify the exception message contains useful information
        Assert.Contains("Failed to deserialize value data:", ex.Message);
    }

    /*
      1/ If this field is None = We go in the easy way that is decode pure protobuf
      2/ If the schemaId in this field is a uuid (16+ chars), its Glue and then you need to strip only the first byte and the deserialize
      3/ If the len(schemaId) is 4, it means it is Confluent and then you need to strip the message fields numbers
    */
    [Theory]
    [InlineData(
        "CgMxMjMSBFRlc3QaDHRlc3RAZ214LmNvbSAKMgoyMDI1LTA2LTIwOgR0YWcxOgR0YWcySg4KBXRoZW1lEgVsaWdodFIaCgpNeXRoZW5xdWFpEgZadXJpY2gaBDgwMDI=",
        null)]
    [InlineData(
        "AAoDMTIzEgRUZXN0Ggx0ZXN0QGdteC5jb20gCjIKMjAyNS0wNi0yMDoEdGFnMToEdGFnMkoOCgV0aGVtZRIFbGlnaHRSGgoKTXl0aGVucXVhaRIGWnVyaWNoGgQ4MDAy",
        "123")]
    [InlineData(
        "BAIACgMxMjMSBFRlc3QaDHRlc3RAZ214LmNvbSAKMgoyMDI1LTA2LTIwOgR0YWcxOgR0YWcyQQAAAAAAAChASg4KBXRoZW1lEgVsaWdodFIaCgpNeXRoZW5xdWFpEgZadXJpY2gaBDgwMDI=",
        "456")]
    [InlineData(
        "AQoDMTIzEgRUZXN0Ggx0ZXN0QGdteC5jb20gCjIKMjAyNS0wNi0yMDoEdGFnMToEdGFnMkoOCgV0aGVtZRIFbGlnaHRSGgoKTXl0aGVucXVhaRIGWnVyaWNoGgQ4MDAy",
        "12345678-1234-1234-1234-123456789012")]
    public void Deserialize_MultipleFormats_EachFormatDeserializesCorrectly(string base64Value,
        string? schemaId)
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        string kafkaEventJson = CreateKafkaEvent("NDI=", base64Value, schemaId); // Key is 42 in base64
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<int, UserProfile>>(stream);

        // Assert
        var record = result.First();
        Assert.NotNull(record);
        Assert.Equal(42, record.Key); // Key should be 42

        // Value should be the same regardless of message index format
        Assert.Equal("Test", record.Value.Name);
        Assert.Equal("Zurich", record.Value.Address.City);
        Assert.Equal(10, record.Value.Age);
        Assert.Single(record.Value.Preferences);
        Assert.Equal("light",record.Value.Preferences.First().Value);
    }

    private string CreateKafkaEvent(string keyValue, string valueValue, string? schemaId = null)
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
                    ],
                    ""valueSchemaMetadata"": {{
                        ""dataFormat"": ""PROTOBUF"",
                        ""schemaId"": ""{schemaId}""
                    }}
                }}
            ]
        }}
    }}";
    }
}