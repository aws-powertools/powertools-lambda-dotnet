using System.Runtime.Serialization;
using System.Text;
using AWS.Lambda.Powertools.Kafka.Json;

#if DEBUG
using KafkaAlias = AWS.Lambda.Powertools.Kafka;
#else
using KafkaAlias = AWS.Lambda.Powertools.Kafka.Json;
#endif

namespace AWS.Lambda.Powertools.Kafka.Tests;

public class JsonErrorHandlingTests
{
    [Fact]
    public void JsonSerializer_WithCorruptedKeyData_ThrowSerializationException()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        var corruptedData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        string kafkaEventJson = CreateKafkaEvent(
            Convert.ToBase64String(corruptedData),
            Convert.ToBase64String(Encoding.UTF8.GetBytes("valid-value"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act & Assert
        var ex = Assert.Throws<SerializationException>(() =>
            serializer.Deserialize<KafkaAlias.ConsumerRecords<Json.TestModel, string>>(stream));

        Assert.Contains("Failed to deserialize key data", ex.Message);
    }

    [Fact]
    public void JsonSerializer_WithCorruptedValueData_ThrowSerializationException()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        var corruptedData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        string kafkaEventJson = CreateKafkaEvent(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("valid-key")),
            Convert.ToBase64String(corruptedData)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act & Assert
        var ex = Assert.Throws<SerializationException>(() =>
            serializer.Deserialize<KafkaAlias.ConsumerRecords<string, Json.TestModel>>(stream));

        Assert.Contains("Failed to deserialize value data", ex.Message);
    }

    private string CreateKafkaEvent(string keyValue, string valueValue)
    {
        return @$"{{
            ""eventSource"": ""aws:kafka"",
            ""records"": {{
                ""mytopic-0"": [
                    {{
                        ""topic"": ""mytopic"",
                        ""partition"": 0,
                        ""offset"": 15,
                        ""key"": ""{keyValue}"",
                        ""value"": ""{valueValue}""
                    }}
                ]
            }}
        }}";
    }
}