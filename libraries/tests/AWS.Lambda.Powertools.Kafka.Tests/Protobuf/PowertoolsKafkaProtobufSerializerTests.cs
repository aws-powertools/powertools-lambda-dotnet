using System.Text;
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
}
