using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Kafka.Json;

namespace AWS.Lambda.Powertools.Kafka.Tests;

public class JsonTests
{
    [Fact]
    public void Given_JsonStreamInput_When_DeserializedWithJsonSerializer_Then_CorrectlyDeserializes()
    {
        // Given
        var serializer = new PowertoolsKafkaJsonSerializer();
        string json = @"{
            ""eventSource"": ""aws:kafka"",
            ""records"": {
                ""mytopic-0"": [
                    {
                        ""topic"": ""mytopic"",
                        ""partition"": 0,
                        ""offset"": 15,
                        ""timestamp"": 1645084650987,
                        ""key"": """ + Convert.ToBase64String(Encoding.UTF8.GetBytes("key1")) + @""",
                        ""value"": """ + Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"Name\":\"JSON Test\",\"Price\":199.99,\"Id\":456}")) + @"""
                    }
                ]
            }
        }";
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        // When
        var result = serializer.Deserialize<ConsumerRecords<string, JsonProduct>>(stream);
        
        // Then
        Assert.Equal("aws:kafka", result.EventSource);
        Assert.Single(result.Records);
        var record = result.First();
        Assert.Equal("key1", record.Key);
        Assert.Equal("JSON Test", record.Value.Name);
        Assert.Equal(199.99m, record.Value.Price);
        Assert.Equal(456, record.Value.Id);
    }
    
    [Fact]
    public void Given_RawUtf8Data_When_ProcessedWithDefaultHandler_Then_DeserializesToStrings()
    {
        // Given
        string Handler(ConsumerRecords<string, string> records, ILambdaContext context)
        {
            foreach (var record in records)
            {
                context.Logger.LogInformation($"Key: {record.Key}, Value: {record.Value}");
            }
            return "Processed raw data";
        }
        
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext { Logger = mockLogger };
        
        // Create Kafka event with raw base64-encoded strings
        string kafkaEventJson = @$"{{
            ""eventSource"": ""aws:kafka"",
            ""records"": {{
                ""mytopic-0"": [
                    {{
                        ""topic"": ""mytopic"",
                        ""partition"": 0,
                        ""offset"": 15,
                        ""key"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("simple-key"))}"",
                        ""value"": ""{Convert.ToBase64String(Encoding.UTF8.GetBytes("Simple UTF-8 text value"))}"",
                        ""headers"": [
                            {{ ""content-type"": [{(int)'t'}, {(int)'e'}, {(int)'x'}, {(int)'t'}] }}
                        ]
                    }}
                ]
            }}
        }}";
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));
        
        // Use the default serializer which handles base64 → UTF-8 conversion
        var serializer = new PowertoolsKafkaJsonSerializer();
        var records = serializer.Deserialize<ConsumerRecords<string, string>>(stream);
        
        // When
        var result = Handler(records, mockContext);
        
        // Then
        Assert.Equal("Processed raw data", result);
        Assert.Contains("Key: simple-key, Value: Simple UTF-8 text value", mockLogger.Buffer.ToString());
    }
}