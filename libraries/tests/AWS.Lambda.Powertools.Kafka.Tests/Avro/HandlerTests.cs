using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Avro.IO;
using Avro.Specific;

namespace AWS.Lambda.Powertools.Kafka.Tests;


public class KafkaHandlerTests
{
    [Fact]
    public async Task Handler_ProcessesKafkaEvent_Successfully()
    {
        // Arrange
        var kafkaJson = GetMockKafkaEvent();
        var mockContext = new TestLambdaContext();
        var serializer = new PowertoolsKafkaAvroSerializer();
        
        // Convert JSON string to stream for deserialization
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaJson));
        
        // Act - Deserialize and process
        var kafkaEvent = serializer.Deserialize<KafkaEvent<AvroProduct>>(stream);
        var response = await Handler(kafkaEvent, mockContext);
        
        // Assert
        Assert.Equal("Successfully processed Kafka events", response);
        
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
        Assert.Equal("Laptop", product.name);
        Assert.Equal(999.99, product.price);
        
        // Verify decoded key and headers
        Assert.Equal("42", firstRecord.Key);
        Assert.Equal("headerValue", firstRecord.Headers["headerKey"]);
    }
    
    private string GetMockKafkaEvent()
    {
        // For testing, we'll create base64-encoded Avro data for our test products
        var laptop = new AvroProduct { name = "Laptop", price = 999.99 };
        var smartphone = new AvroProduct { name = "Smartphone", price = 499.99 };
        var headphones = new AvroProduct { name = "Headphones", price = 99.99 };
        
        // Convert to base64-encoded Avro
        string laptopBase64 = ConvertToAvroBase64(laptop);
        string smartphoneBase64 = ConvertToAvroBase64(smartphone);
        string headphonesBase64 = ConvertToAvroBase64(headphones);
        
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
                        ""key"": ""NDI="",
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
                        ""key"": ""NDI="",
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
    
    private string ConvertToAvroBase64(AvroProduct product)
    {
        using var stream = new MemoryStream();
        var encoder = new BinaryEncoder(stream);
        var writer = new SpecificDatumWriter<AvroProduct>(AvroProduct._SCHEMA);
        
        writer.Write(product, encoder);
        encoder.Flush();
        
        return Convert.ToBase64String(stream.ToArray());
    }
    
    // Define the test handler method
    private async Task<string> Handler(KafkaEvent<AvroProduct> kafkaEvent, ILambdaContext context)
    {
        foreach (var topicRecords in kafkaEvent.Records)
        {
            foreach (var record in topicRecords.Value)
            {
                var product = record.Value;
                context.Logger.LogInformation($"Processing {product.name} at ${product.price}");
            }
        }

        return "Successfully processed Kafka events";
    }
}