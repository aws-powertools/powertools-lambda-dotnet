using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Google.Protobuf;
using TestKafka;

namespace AWS.Lambda.Powertools.Kafka.Tests.Protobuf;

public class ProtobufHandlerTests
{
    [Fact]
    public async Task Handler_ProcessesKafkaEvent_Successfully()
    {
        // Arrange
        var kafkaJson = GetMockKafkaEvent();
        var mockContext = new TestLambdaContext();
        var serializer = new PowertoolsKafkaProtobufSerializer();

        // Convert JSON string to stream for deserialization
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaJson));

        // Act - Deserialize and process
        var kafkaEvent = serializer.Deserialize<ConsumerRecords<int, ProtobufProduct>>(stream);
        var response = await Handler(kafkaEvent, mockContext);

        // Assert
        Assert.Equal("Successfully processed Protobuf Kafka events", response);

        // Verify event structure
        Assert.Equal("aws:kafka", kafkaEvent.EventSource);
        Assert.Single(kafkaEvent.Records);

        // Verify record content
        var records = kafkaEvent.Records["mytopic-0"];
        Assert.Equal(3, records.Count);

        // Verify first record
        var firstRecord = records[0];
        Assert.Equal("mytopic", firstRecord.Topic);
        Assert.Equal(0, firstRecord.Partition);
        Assert.Equal(15, firstRecord.Offset);

        // Verify deserialized value
        var product = firstRecord.Value;
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(999.99, product.Price);

        // Verify decoded key and headers
        Assert.Equal(42, firstRecord.Key);
        Assert.Equal("headerValue", firstRecord.Headers["headerKey"]);

        var secondRecord = records[1];
        Assert.Equal(43, secondRecord.Key);

        var thirdRecord = records[2];
        Assert.Equal(0, thirdRecord.Key);
    }

    [Fact]
    public async Task Handler_ProcessesKafkaEvent_WithProtobufKey_Successfully()
    {
        // Arrange
        var kafkaJson = GetMockKafkaEventWithProtobufKeys();
        var mockContext = new TestLambdaContext();
        var serializer = new PowertoolsKafkaProtobufSerializer();

        // Convert JSON string to stream for deserialization
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaJson));

        // Act - Deserialize and process
        var kafkaEvent = serializer.Deserialize<ConsumerRecords<ProtobufKey, ProtobufProduct>>(stream);
        var response = await HandlerWithProtobufKeys(kafkaEvent, mockContext);

        // Assert
        Assert.Equal("Successfully processed Protobuf Kafka events with complex keys", response);

        // Verify event structure
        Assert.Equal("aws:kafka", kafkaEvent.EventSource);
        Assert.Single(kafkaEvent.Records);

        // Verify record content
        var records = kafkaEvent.Records["mytopic-0"];
        Assert.Equal(3, records.Count);

        // Verify first record
        var firstRecord = records[0];
        Assert.Equal("mytopic", firstRecord.Topic);
        Assert.Equal(0, firstRecord.Partition);
        Assert.Equal(15, firstRecord.Offset);

        // Verify deserialized Protobuf key and value
        Assert.Equal("Laptop", firstRecord.Value.Name);
        Assert.Equal(999.99, firstRecord.Value.Price);
        Assert.Equal(1, firstRecord.Key.Id);
        Assert.Equal(TestKafka.Color.Green, firstRecord.Key.Color);

        // Verify headers
        Assert.Equal("headerValue", firstRecord.Headers["headerKey"]);

        var secondRecord = records[1];
        Assert.Equal(2, secondRecord.Key.Id);
        Assert.Equal(TestKafka.Color.Unknown, secondRecord.Key.Color);

        var thirdRecord = records[2];
        Assert.Equal(3, thirdRecord.Key.Id);
        Assert.Equal(TestKafka.Color.Red, thirdRecord.Key.Color);
    }

    private string GetMockKafkaEvent()
    {
        // For testing, we'll create base64-encoded Protobuf data for our test products
        var laptop = new ProtobufProduct 
        { 
            Name = "Laptop", 
            Id = 1001, 
            Price = 999.99
        };
        
        var smartphone = new ProtobufProduct 
        { 
            Name = "Smartphone", 
            Id = 1002, 
            Price = 499.99
        };
        
        var headphones = new ProtobufProduct 
        { 
            Name = "Headphones", 
            Id = 1003, 
            Price = 99.99
        };

        // Convert to base64-encoded Protobuf
        string laptopBase64 = Convert.ToBase64String(laptop.ToByteArray());
        string smartphoneBase64 = Convert.ToBase64String(smartphone.ToByteArray());
        string headphonesBase64 = Convert.ToBase64String(headphones.ToByteArray());

        string firstRecordKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("42")); // Example key
        string secondRecordKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("43")); // Example key for second record

        // Create mock Kafka event JSON
        return @$"{{
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
                        ""key"": ""{firstRecordKey}"",
                        ""value"": ""{laptopBase64}"",
                        ""headers"": [
                            {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                        ]
                    }},
                    {{
                        ""topic"": ""mytopic"",
                        ""partition"": 0,
                        ""offset"": 16,
                        ""timestamp"": 1545084650988,
                        ""timestampType"": ""CREATE_TIME"",
                        ""key"": ""{secondRecordKey}"",
                        ""value"": ""{smartphoneBase64}"",
                        ""headers"": [
                            {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                        ]
                    }},
                    {{
                        ""topic"": ""mytopic"",
                        ""partition"": 0,
                        ""offset"": 17,
                        ""timestamp"": 1545084650989,
                        ""timestampType"": ""CREATE_TIME"",
                        ""key"": null,
                        ""value"": ""{headphonesBase64}"",
                        ""headers"": [
                            {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                        ]
                    }}
                ]
            }}
        }}";
    }

    private string GetMockKafkaEventWithProtobufKeys()
    {
        // Create test products
        var laptop = new ProtobufProduct 
        { 
            Name = "Laptop", 
            Id = 1001, 
            Price = 999.99
        };
        
        var smartphone = new ProtobufProduct 
        { 
            Name = "Smartphone", 
            Id = 1002, 
            Price = 499.99
        };
        
        var headphones = new ProtobufProduct 
        { 
            Name = "Headphones", 
            Id = 1003, 
            Price = 99.99
        };

        // Create test keys
        var key1 = new ProtobufKey { Id = 1, Color = TestKafka.Color.Green };
        var key2 = new ProtobufKey { Id = 2 };
        var key3 = new ProtobufKey { Id = 3, Color = TestKafka.Color.Red };

        // Convert values to base64-encoded Protobuf
        string laptopBase64 = Convert.ToBase64String(laptop.ToByteArray());
        string smartphoneBase64 = Convert.ToBase64String(smartphone.ToByteArray());
        string headphonesBase64 = Convert.ToBase64String(headphones.ToByteArray());

        // Convert keys to base64-encoded Protobuf
        string key1Base64 = Convert.ToBase64String(key1.ToByteArray());
        string key2Base64 = Convert.ToBase64String(key2.ToByteArray());
        string key3Base64 = Convert.ToBase64String(key3.ToByteArray());

        // Create mock Kafka event JSON
        return @$"{{
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
                    ""key"": ""{key1Base64}"",
                    ""value"": ""{laptopBase64}"",
                    ""headers"": [
                        {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                    ]
                }},
                {{
                    ""topic"": ""mytopic"",
                    ""partition"": 0,
                    ""offset"": 16,
                    ""timestamp"": 1545084650988,
                    ""timestampType"": ""CREATE_TIME"",
                    ""key"": ""{key2Base64}"",
                    ""value"": ""{smartphoneBase64}"",
                    ""headers"": [
                        {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                    ]
                }},
                {{
                    ""topic"": ""mytopic"",
                    ""partition"": 0,
                    ""offset"": 17,
                    ""timestamp"": 1545084650989,
                    ""timestampType"": ""CREATE_TIME"",
                    ""key"": ""{key3Base64}"",
                    ""value"": ""{headphonesBase64}"",
                    ""headers"": [
                        {{ ""headerKey"": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] }}
                    ]
                }}
            ]
        }}
    }}";
    }

    // Define the test handler method
    private async Task<string> Handler(ConsumerRecords<int, ProtobufProduct> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            var product = record.Value;
            context.Logger.LogInformation($"Processing {product.Name} at ${product.Price}");
        }

        return "Successfully processed Protobuf Kafka events";
    }

    private async Task<string> HandlerWithProtobufKeys(ConsumerRecords<ProtobufKey, ProtobufProduct> records,
        ILambdaContext context)
    {
        foreach (var record in records)
        {
            var key = record.Key;
            var product = record.Value;
            context.Logger.LogInformation($"Processing key {key.Id} - {product.Name} at ${product.Price}");
        }

        return "Successfully processed Protobuf Kafka events with complex keys";
    }
}
