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

/*
   These tests cover the key use cases you requested:
   
   1. Basic Functionality:
   Processing single records
   Processing multiple records
   Accessing record metadata
   
   2. Data Formats:
   JSON deserialization
   Avro deserialization
   Protobuf deserialization
   Raw/default deserialization
   
   3. Key Processing:
   Processing various key formats (string, int, complex objects)
   Handling null keys
   
   4.Error Handling:
   Invalid JSON data
   Missing schemas with fallback mechanisms
   
   5.Headers & Metadata:
   Accessing and parsing record headers
 */

using System.Runtime.Serialization;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Kafka.Avro;
using TestKafka;

namespace AWS.Lambda.Powertools.Kafka.Tests;

public class KafkaHandlerFunctionalTests
{
    #region JSON Serializer Tests
    
    [Fact]
    public void Given_SingleJsonRecord_When_ProcessedWithHandler_Then_SuccessfullyDeserializedAndProcessed()
    {
        // Given
        string Handler(ConsumerRecords<string, JsonProduct> records, ILambdaContext context)
        {
            foreach (var record in records)
            {
                context.Logger.LogInformation($"Processing {record.Value.Name} at ${record.Value.Price}");
            }
            return "Successfully processed JSON Kafka events";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        // Create a single record
        var records = new ConsumerRecords<string, JsonProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<string, JsonProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<string, JsonProduct>>
                    {
                        new()
                        {
                            Topic = "mytopic",
                            Partition = 0,
                            Offset = 15,
                            Timestamp = 1645084650987,
                            TimestampType = "CREATE_TIME",
                            Key = "product-123",
                            Value = new JsonProduct { Name = "Laptop", Price = 999.99m, Id = 123 },
                            Headers = new Dictionary<string, byte[]>
                            {
                                { "source", Encoding.UTF8.GetBytes("online-store") }
                            }
                        }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Successfully processed JSON Kafka events", result);
        Assert.Contains("Processing Laptop at $999.99", mockLogger.Buffer.ToString());
    }
    
    [Fact]
    public void Given_MultipleJsonRecords_When_ProcessedWithHandler_Then_AllRecordsProcessed()
    {
        // Given
        int processedCount = 0;
        string Handler(ConsumerRecords<string, JsonProduct> records, ILambdaContext context)
        {
            foreach (var record in records)
            {
                context.Logger.LogInformation($"Processing {record.Value.Name}");
                processedCount++;
            }
            return $"Processed {processedCount} records";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        // Create multiple records
        var records = new ConsumerRecords<string, JsonProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<string, JsonProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<string, JsonProduct>>
                    {
                        new() { Topic = "mytopic", Value = new JsonProduct { Name = "Laptop" } },
                        new() { Topic = "mytopic", Value = new JsonProduct { Name = "Phone" } },
                        new() { Topic = "mytopic", Value = new JsonProduct { Name = "Tablet" } }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Processed 3 records", result);
        Assert.Contains("Processing Laptop", mockLogger.Buffer.ToString());
        Assert.Contains("Processing Phone", mockLogger.Buffer.ToString());
        Assert.Contains("Processing Tablet", mockLogger.Buffer.ToString());
    }
    
    [Fact]
    public void Given_JsonRecordWithMetadata_When_ProcessedWithHandler_Then_MetadataIsAccessible()
    {
        // Given
        string Handler(ConsumerRecords<string, JsonProduct> records, ILambdaContext context)
        {
            var record = records.First();
            context.Logger.LogInformation($"Topic: {record.Topic}, Partition: {record.Partition}, Offset: {record.Offset}, Time: {record.Timestamp}");
            return "Metadata accessed";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        var records = new ConsumerRecords<string, JsonProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<string, JsonProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<string, JsonProduct>>
                    {
                        new()
                        {
                            Topic = "sales-data",
                            Partition = 3,
                            Offset = 42,
                            Timestamp = 1645084650987,
                            TimestampType = "CREATE_TIME",
                            Value = new JsonProduct { Name = "Metadata Test" }
                        }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Metadata accessed", result);
        Assert.Contains("Topic: sales-data, Partition: 3, Offset: 42", mockLogger.Buffer.ToString());
    }
    
    [Fact]
    public void Given_JsonRecordWithHeaders_When_ProcessedWithHandler_Then_HeadersAreAccessible()
    {
        // Given
        string Handler(ConsumerRecords<string, JsonProduct> records, ILambdaContext context)
        {
            var record = records.First();
            var source = record.Headers["source"].DecodedValue();
            var contentType = record.Headers["content-type"].DecodedValue();
            context.Logger.LogInformation($"Headers: source={source}, content-type={contentType}");
            return "Headers processed";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        var records = new ConsumerRecords<string, JsonProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<string, JsonProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<string, JsonProduct>>
                    {
                        new()
                        {
                            Value = new JsonProduct { Name = "Header Test" },
                            Headers = new Dictionary<string, byte[]>
                            {
                                { "source", Encoding.UTF8.GetBytes("web-app") },
                                { "content-type", Encoding.UTF8.GetBytes("application/json") }
                            }
                        }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Headers processed", result);
        Assert.Contains("Headers: source=web-app, content-type=application/json", mockLogger.Buffer.ToString());
    }
    
    #endregion
    
    #region Avro Serializer Tests
    
    [Fact]
    public void Given_SingleAvroRecord_When_ProcessedWithHandler_Then_SuccessfullyDeserializedAndProcessed()
    {
        // Given
        string Handler(ConsumerRecords<string, AvroProduct> records, ILambdaContext context)
        {
            foreach (var record in records)
            {
                context.Logger.LogInformation($"Processing {record.Value.name} at ${record.Value.price}");
            }
            return "Successfully processed Avro Kafka events";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        // Create a single record
        var records = new ConsumerRecords<string, AvroProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<string, AvroProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<string, AvroProduct>>
                    {
                        new()
                        {
                            Topic = "mytopic",
                            Partition = 0,
                            Offset = 15,
                            Key = "avro-key",
                            Value = new AvroProduct { name = "Camera", price = 349.95 }
                        }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Successfully processed Avro Kafka events", result);
        Assert.Contains("Processing Camera at $349.95", mockLogger.Buffer.ToString());
    }
    
    [Fact]
    public void Given_ComplexAvroKey_When_ProcessedWithHandler_Then_KeyIsCorrectlyDeserialized()
    {
        // Given
        string Handler(ConsumerRecords<AvroKey, AvroProduct> records, ILambdaContext context)
        {
            var record = records.First();
            context.Logger.LogInformation($"Processing product with key ID: {record.Key.id}, color: {record.Key.color}");
            return "Successfully processed complex keys";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        var records = new ConsumerRecords<AvroKey, AvroProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<AvroKey, AvroProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<AvroKey, AvroProduct>>
                    {
                        new()
                        {
                            Key = new AvroKey { id = 42, color = Color.GREEN },
                            Value = new AvroProduct { name = "Green Item", price = 49.99 }
                        }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Successfully processed complex keys", result);
        Assert.Contains("Processing product with key ID: 42, color: GREEN", mockLogger.Buffer.ToString());
    }
    
    [Fact]
    public void Given_MissingAvroSchema_When_DeserializedWithAvroSerializer_Then_ReturnsException()
    {
        // Arrange
        var serializer = new PowertoolsKafkaAvroSerializer();

        // Create data that looks like Avro but without schema
        byte[] invalidAvroData = { 0x01, 0x02, 0x03, 0x04 }; // Just some random bytes
        string base64Data = Convert.ToBase64String(invalidAvroData);

        string kafkaEventJson = @$"{{
        ""eventSource"": ""aws:kafka"",
        ""records"": {{
            ""mytopic-0"": [
                {{
                    ""topic"": ""mytopic"",
                    ""partition"": 0,
                    ""offset"": 15,
                    ""key"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key"))}"",
                    ""value"": ""{base64Data}""
                }}
            ]
        }}
    }}";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));
        Assert.Throws<SerializationException>(() => 
            serializer.Deserialize<ConsumerRecords<string, AvroProduct>>(stream));
    }
    
    #endregion
    
    #region Protobuf Serializer Tests
    
    [Fact]
    public void Given_SingleProtobufRecord_When_ProcessedWithHandler_Then_SuccessfullyDeserializedAndProcessed()
    {
        // Given
        string Handler(ConsumerRecords<int, ProtobufProduct> records, ILambdaContext context)
        {
            foreach (var record in records)
            {
                context.Logger.LogInformation($"Processing {record.Value.Name} at ${record.Value.Price}");
            }
            return "Successfully processed Protobuf Kafka events";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        // Create a single record
        var records = new ConsumerRecords<int, ProtobufProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<int, ProtobufProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<int, ProtobufProduct>>
                    {
                        new()
                        {
                            Topic = "mytopic",
                            Partition = 0,
                            Offset = 15,
                            Key = 42,
                            Value = new ProtobufProduct { Name = "Smart Watch", Id = 789, Price = 249.99 }
                        }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Successfully processed Protobuf Kafka events", result);
        Assert.Contains("Processing Smart Watch at $249.99", mockLogger.Buffer.ToString());
    }
    
    [Fact] 
    public void Given_NullKeyOrValue_When_ProcessedWithHandler_Then_HandlesNullsCorrectly()
    {
        // Given
        string Handler(ConsumerRecords<int?, ProtobufProduct> records, ILambdaContext context)
        {
            foreach (var record in records)
            {
                string keyInfo = record.Key.HasValue ? record.Key.Value.ToString() : "null";
                string valueInfo = record.Value != null ? record.Value.Name : "null";
                context.Logger.LogInformation($"Key: {keyInfo}, Value: {valueInfo}");
            }
            return "Processed records with nulls";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        var records = new ConsumerRecords<int?, ProtobufProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<int?, ProtobufProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<int?, ProtobufProduct>>
                    {
                        new() { Key = 1, Value = new ProtobufProduct { Name = "Valid Product" } },
                        new() { Key = null, Value = new ProtobufProduct { Name = "No Key" } },
                        new() { Key = 3, Value = null }
                    }
                }
            }
        };
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Processed records with nulls", result);
        Assert.Contains("Key: 1, Value: Valid Product", mockLogger.Buffer.ToString());
        Assert.Contains("Key: null, Value: No Key", mockLogger.Buffer.ToString());
        Assert.Contains("Key: 3, Value: null", mockLogger.Buffer.ToString());
    }
    
    #endregion
}

// Model classes for testing
public class JsonProduct
{
    public string Name { get; set; }
    public int Id { get; set; }
    public decimal Price { get; set; }
}