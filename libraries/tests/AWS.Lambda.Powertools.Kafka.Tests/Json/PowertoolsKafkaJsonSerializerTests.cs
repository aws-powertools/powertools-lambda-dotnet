using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Kafka.Json;

#if DEBUG
using KafkaAlias = AWS.Lambda.Powertools.Kafka;
#else
using KafkaAlias = AWS.Lambda.Powertools.Kafka.Json;
#endif

namespace AWS.Lambda.Powertools.Kafka.Tests.Json;

public class PowertoolsKafkaJsonSerializerTests
{
    [Fact]
    public void Deserialize_KafkaEventWithJsonPayload_DeserializesToCorrectType()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        var testModel = new TestModel { Name = "Test Product", Value = 123 };
        var jsonValue = JsonSerializer.Serialize(testModel);
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonValue));
        
        string kafkaEventJson = CreateKafkaEvent("NDI=", base64Value); // Key is 42 in base64
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<int, TestModel>>(stream);

        // Assert
        Assert.NotNull(result);
        var record = result.First();
        Assert.Equal(42, record.Key);
        Assert.Equal("Test Product", record.Value.Name);
        Assert.Equal(123, record.Value.Value);
    }

    [Fact]
    public void KafkaEvent_ImplementsIEnumerable_ForDirectIteration()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        string kafkaEventJson = File.ReadAllText("Json/kafka-json-event.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<string, JsonProduct>>(stream);

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
    public void DeserializeComplexKey_StandardJsonDeserialization_Works()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        var complexObject = new { Name = "Test", Id = 123 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(complexObject));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(jsonBytes),
            valueValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("test"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<Dictionary<string, object>, string>>(stream);

        // Assert
        var record = result.First();
        Assert.NotNull(record.Key);
        Assert.Equal("Test", record.Key["Name"].ToString());
        Assert.Equal(123, int.Parse(record.Key["Id"].ToString()));
    }

    [Fact]
    public void DeserializeComplexKey_WithSerializerContext_UsesContext()
    {
        // Arrange
        // Create custom context
        var options = new JsonSerializerOptions();
        var context = new TestJsonSerializerContext(options);
        var serializer = new PowertoolsKafkaJsonSerializer(context);

        // Create test data with the registered type
        var testModel = new TestModel { Name = "TestFromContext", Value = 456 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testModel));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(jsonBytes),
            valueValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("test"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<TestModel, string>>(stream);

        // Assert
        var record = result.First();
        Assert.NotNull(record.Key);
        Assert.Equal("TestFromContext", record.Key.Name);
        Assert.Equal(456, record.Key.Value);
    }

    [Fact]
    public void DeserializeComplexValue_WithSerializerContext_UsesContext()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        var context = new TestJsonSerializerContext(options);
        var serializer = new PowertoolsKafkaJsonSerializer(context);

        // Create test data with the registered type
        var testModel = new TestModel { Name = "ValueFromContext", Value = 789 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testModel));

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(jsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<string, TestModel>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.NotNull(record.Value);
        Assert.Equal("ValueFromContext", record.Value.Name);
        Assert.Equal(789, record.Value.Value);
    }
    
    [Fact]
    public void DeserializeComplexValue_WithCustomJsonOptions_RespectsOptions()
    {
        // Arrange - create custom options with different naming policy
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false // Force exact case match
        };
        var serializer = new PowertoolsKafkaJsonSerializer(options);

        // Create test data with camelCase property names
        var jsonBytes = Encoding.UTF8.GetBytes(@"{""id"":999,""name"":""camelCase"",""price"":29.99}");

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(jsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<string, JsonProduct>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal(999, record.Value.Id);
        Assert.Equal("camelCase", record.Value.Name);
        Assert.Equal(29.99m, record.Value.Price);
    }
    
    [Fact]
    public void DeserializeComplexValue_WithEmptyData_ReturnsNullOrDefault()
    {
        // Arrange
        var serializer = new PowertoolsKafkaJsonSerializer();
        // Empty JSON data
        byte[] emptyBytes = Array.Empty<byte>();

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(emptyBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<string, JsonProduct>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.Null(record.Value); // Should be null for empty input
    }
    
    [Fact]
    public void DeserializeComplexValue_WithContextAndNullResult_ReturnsNull()
    {
        // Arrange - create a context with JsonNullHandling.Include
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IgnoreNullValues = false
        };
        var context = new TestJsonSerializerContext(options);
        var serializer = new PowertoolsKafkaJsonSerializer(context);

        // JSON that explicitly sets the value to null
        var jsonBytes = Encoding.UTF8.GetBytes("null");

        string kafkaEventJson = CreateKafkaEvent(
            keyValue: Convert.ToBase64String(Encoding.UTF8.GetBytes("testKey")),
            valueValue: Convert.ToBase64String(jsonBytes)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act
        var result = serializer.Deserialize<KafkaAlias.ConsumerRecords<string, TestModel>>(stream);

        // Assert
        var record = result.First();
        Assert.Equal("testKey", record.Key);
        Assert.Null(record.Value);
    }
    

    /// <summary>
    /// Helper method to create Kafka event JSON with specified key and value in base64 format
    /// </summary>
    private string CreateKafkaEvent(string keyValue, string valueValue)
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
                            ]
                        }}
                    ]
                }}
            }}";
    }

    [Fact]
    public void DirectJsonSerializerTest_InvokesFormatSpecificMethod()
    {
        // This test directly tests the JSON serializer methods
        var serializer = new TestJsonDeserializer();

        // Create test data with valid JSON
        var testModel = new TestModel { Name = "DirectTest", Value = 555 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testModel));

        // Act
        var result = serializer.TestDeserializeFormatSpecific(jsonBytes, typeof(TestModel), false);

        // Assert
        Assert.NotNull(result);
        var model = result as TestModel;
        Assert.NotNull(model);
        Assert.Equal("DirectTest", model!.Name);
        Assert.Equal(555, model.Value);
    }

    [Fact]
    public void DirectJsonSerializerTest_WithContext_UsesContext()
    {
        // Create a context that includes TestModel
        var options = new JsonSerializerOptions();
        var context = new TestJsonSerializerContext(options);
        
        // Create the serializer with context
        var serializer = new TestJsonDeserializer(context);

        // Create test data with valid JSON
        var testModel = new TestModel { Name = "ContextTest", Value = 999 };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testModel));

        // Act - directly test the protected method
        var result = serializer.TestDeserializeFormatSpecific(jsonBytes, typeof(TestModel), false);

        // Assert
        Assert.NotNull(result);
        var model = result as TestModel;
        Assert.NotNull(model);
        Assert.Equal("ContextTest", model!.Name);
        Assert.Equal(999, model.Value);
    }

    [Fact]
    public void DirectJsonSerializerTest_WithEmptyJson_ReturnsNullOrDefault()
    {
        // Create the serializer 
        var serializer = new TestJsonDeserializer();

        // Create empty JSON data
        var emptyJsonBytes = Array.Empty<byte>();

        // Act - test with reference type
        var resultRef = serializer.TestDeserializeFormatSpecific(emptyJsonBytes, typeof(TestModel), false);
        // Act - test with value type
        var resultVal = serializer.TestDeserializeFormatSpecific(emptyJsonBytes, typeof(int), false);

        // Assert
        Assert.Null(resultRef); // Reference type should get null
        Assert.Equal(0, resultVal); // Value type should get default
    }
    
    [Fact]
    public void DirectJsonSerializerTest_WithContextResultingInNull_ReturnsNull()
    {
        // Create context
        var options = new JsonSerializerOptions();
        var context = new TestJsonSerializerContext(options);
        
        // Create serializer with context
        var serializer = new TestJsonDeserializer(context);

        // Create JSON that is "null"
        var jsonBytes = Encoding.UTF8.GetBytes("null");

        // Act - even with context, null JSON should return null
        var result = serializer.TestDeserializeFormatSpecific(jsonBytes, typeof(TestModel), false);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test helper to directly access protected methods
    /// </summary>
    private class TestJsonDeserializer : PowertoolsKafkaJsonSerializer
    {
        public TestJsonDeserializer() : base() { }
        
        public TestJsonDeserializer(JsonSerializerOptions options) : base(options) { }
        
        public TestJsonDeserializer(JsonSerializerContext context) : base(context) { }

        public object? TestDeserializeFormatSpecific(byte[] data, Type targetType, bool isKey)
        {
            // Call the protected method directly
            return base.DeserializeComplexTypeFormat(data, targetType, isKey);
        }
    }
}

[JsonSerializable(typeof(TestModel))]
public partial class TestJsonSerializerContext : JsonSerializerContext
{
}

public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public record JsonProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public struct ValueTypeProduct
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
