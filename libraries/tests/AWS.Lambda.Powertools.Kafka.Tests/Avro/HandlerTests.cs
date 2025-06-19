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
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Avro.IO;
using Avro.Specific;
using AWS.Lambda.Powertools.Kafka.Avro;

namespace AWS.Lambda.Powertools.Kafka.Tests.Avro;

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
        var kafkaEvent = serializer.Deserialize<ConsumerRecords<int, AvroProduct>>(stream);
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
        Assert.Equal(42, firstRecord.Key);
        Assert.Equal("headerValue", firstRecord.Headers["headerKey"].DecodedValue());

        var secondRecord = records[1];
        Assert.Equal(43, secondRecord.Key);

        var thirdRecord = records[2];
        Assert.Equal(0, thirdRecord.Key);
    }
    
    [Fact]
    public async Task Handler_ProcessesKafkaEvent_Primitive_Successfully()
    {
        // Arrange
        var kafkaJson = GetSimpleMockKafkaEvent();
        var mockContext = new TestLambdaContext();
        var serializer = new PowertoolsKafkaAvroSerializer();

        // Convert JSON string to stream for deserialization
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaJson));

        // Act - Deserialize and process
        var kafkaEvent = serializer.Deserialize<ConsumerRecords<int, string>>(stream);
        var response = await HandlerSimple(kafkaEvent, mockContext);

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
        Assert.Equal("Laptop", firstRecord.Value);

        // Verify decoded key and headers
        Assert.Equal(42, firstRecord.Key);
        Assert.Equal("headerValue", firstRecord.Headers["headerKey"].DecodedValue());

        var secondRecord = records[1];
        Assert.Equal(43, secondRecord.Key);
        Assert.Equal("Smartphone", secondRecord.Value);

        var thirdRecord = records[2];
        Assert.Equal(0, thirdRecord.Key);
        Assert.Null(thirdRecord.Value);
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
    
    private string GetSimpleMockKafkaEvent()
    {
        // For testing, we'll create base64-encoded Avro data for our test products

        // Convert to base64-encoded Avro
        string laptopBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("Laptop"));
        string smartphoneBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("Smartphone"));

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
                        ""value"": null,
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
    private async Task<string> Handler(ConsumerRecords<int, AvroProduct> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            var product = record.Value;
            context.Logger.LogInformation($"Processing {product.name} at ${product.price}");
        }

        return "Successfully processed Kafka events";
    }
    
    private async Task<string> HandlerSimple(ConsumerRecords<int, string> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            var product = record.Value;
            context.Logger.LogInformation($"Processing {product}");
        }

        return "Successfully processed Kafka events";
    }

    [Fact]
    public async Task Handler_ProcessesKafkaEvent_WithAvroKey_Successfully()
    {
        // Arrange
        var kafkaJson = GetMockKafkaEventWithAvroKeys();
        var mockContext = new TestLambdaContext();
        var serializer = new PowertoolsKafkaAvroSerializer();

        // Convert JSON string to stream for deserialization
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaJson));

        // Act - Deserialize and process
        var kafkaEvent = serializer.Deserialize<ConsumerRecords<AvroKey, AvroProduct>>(stream);
        var response = await HandlerWithAvroKeys(kafkaEvent, mockContext);

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

        // Verify deserialized Avro key and value
        Assert.Equal("Laptop", firstRecord.Value.name);
        Assert.Equal(999.99, firstRecord.Value.price);
        Assert.Equal(1, firstRecord.Key.id);
        Assert.Equal(Color.GREEN, firstRecord.Key.color);
        
        // Verify headers
        Assert.Equal("headerValue", firstRecord.Headers["headerKey"].DecodedValue());

        var secondRecord = records[1];
        Assert.Equal(2, secondRecord.Key.id);
        Assert.Equal(Color.UNKNOWN, secondRecord.Key.color);

        var thirdRecord = records[2];
        Assert.Equal(3, thirdRecord.Key.id);
        Assert.Equal(Color.RED, thirdRecord.Key.color);
    }

    private string GetMockKafkaEventWithAvroKeys()
    {
        // Create test products
        var laptop = new AvroProduct { name = "Laptop", price = 999.99 };
        var smartphone = new AvroProduct { name = "Smartphone", price = 499.99 };
        var headphones = new AvroProduct { name = "Headphones", price = 99.99 };

        // Create test keys
        var key1 = new AvroKey { id = 1, color = Color.GREEN };
        var key2 = new AvroKey { id = 2 };
        var key3 = new AvroKey { id = 3, color = Color.RED };

        // Convert values to base64-encoded Avro
        string laptopBase64 = ConvertToAvroBase64(laptop);
        string smartphoneBase64 = ConvertToAvroBase64(smartphone);
        string headphonesBase64 = ConvertToAvroBase64(headphones);

        // Convert keys to base64-encoded Avro
        string key1Base64 = ConvertKeyToAvroBase64(key1);
        string key2Base64 = ConvertKeyToAvroBase64(key2);
        string key3Base64 = ConvertKeyToAvroBase64(key3);

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

    private string ConvertKeyToAvroBase64(AvroKey key)
    {
        using var stream = new MemoryStream();
        var encoder = new BinaryEncoder(stream);
        var writer = new SpecificDatumWriter<AvroKey>(AvroKey._SCHEMA);

        writer.Write(key, encoder);
        encoder.Flush();

        return Convert.ToBase64String(stream.ToArray());
    }

    private async Task<string> HandlerWithAvroKeys(ConsumerRecords<AvroKey, AvroProduct> records,
        ILambdaContext context)
    {
        foreach (var record in records)
        {
            var key = record.Key.id;
            var product = record.Value;
        }

        return "Successfully processed Kafka events";
    }
}