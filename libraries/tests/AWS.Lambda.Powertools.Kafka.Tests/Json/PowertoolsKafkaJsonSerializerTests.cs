using System.Text;
using AWS.Lambda.Powertools.Kafka.Json;

namespace AWS.Lambda.Powertools.Kafka.Tests.Json;

public record JsonProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

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
        string kafkaEventJson = @$"{{
            ""eventSource"": ""aws:kafka"",
            ""eventSourceArn"": ""arn:aws:kafka:us-east-1:0123456789019:cluster/SalesCluster/abcd1234-abcd-cafe-abab-9876543210ab-4"",
            ""bootstrapServers"": ""b-2.demo-cluster-1.a1bcde.c1.kafka.us-east-1.amazonaws.com:9092,b-1.demo-cluster-1.a1bcde.c1.kafka.us-east-1.amazonaws.com:9092"",
            ""records"": {{
                ""mytopic-0"": [
                    {{
                        ""topic"": ""mytopic"",
                        ""partition"": 0,
                        ""offset"": 15,
                        ""timestamp"": 1545084650987,
                        ""timestampType"": ""CREATE_TIME"",
                        ""key"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("MyKey"))}"",
                        ""value"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("Myvalue"))}"",
                        ""headers"": [
                            {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                        ]
                    }}
                ]
            }}
        }}";
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));
        
        // Act
        var result = serializer.Deserialize<ConsumerRecords<string, string>>(stream);
        var firstRecord = result.First();
        Assert.Equal("Myvalue", firstRecord.Value);
        Assert.Equal("MyKey", firstRecord.Key);
    }
}
