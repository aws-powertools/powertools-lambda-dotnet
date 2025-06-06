using System.Text;
using Avro;
using Avro.Generic;
using Avro.IO;
using Avro.Specific;

namespace AWS.Lambda.Powertools.Kafka.Tests;

public class PowertoolsKafkaAvroSerializerTests
{
    [Fact]
    public void Deserialize_KafkaEventWithAvroPayload_DeserializesToCorrectType()
    {
        // Arrange
        var serializer = new PowertoolsKafkaAvroSerializer();
        string kafkaEventJson = File.ReadAllText("Avro/kafka-avro-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));
            
        // Act
        var result = serializer.Deserialize<KafkaEvent<AvroProduct>>(stream);
            
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
        Assert.Equal("42", firstRecord.Key);
            
        // Verify deserialized Avro value
        var product = firstRecord.Value;
        Assert.Equal("Laptop", product.name);
        Assert.Equal(1001, product.id);
        Assert.Equal(999.99000000000001, product.price);
            
        // Verify second record
        var secondRecord = records[1];
        var smartphone = secondRecord.Value;
        Assert.Equal("Smartphone", smartphone.name);
    }
    
    [Fact]
    public void KafkaEvent_ImplementsIEnumerable_ForDirectIteration()
    {
        // Arrange
        var serializer = new PowertoolsKafkaAvroSerializer();
        string kafkaEventJson = File.ReadAllText("Avro/kafka-avro-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));
        
        // Act
        var result = serializer.Deserialize<KafkaEvent<AvroProduct>>(stream);
    
        // Assert - Test enumeration
        int count = 0;
        var products = new List<string>();
    
        // Directly iterate over KafkaEvent
        foreach (var record in result)
        {
            count++;
            products.Add(record.Value.name);
        }
    
        // Verify correct count and values
        Assert.Equal(3, count);
        Assert.Contains("Laptop", products);
        Assert.Contains("Smartphone", products);
        Assert.Equal(3, products.Count);
    
        // Get first record directly through Linq extension
        var firstRecord = result.First();
        Assert.Equal("Laptop", firstRecord.Value.name);
        Assert.Equal(1001, firstRecord.Value.id);
    }
}