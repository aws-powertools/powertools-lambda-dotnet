using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using AWS.Lambda.Powertools.Kafka.Avro;
using AWS.Lambda.Powertools.Kafka.Json;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using JsonTestModel = AWS.Lambda.Powertools.Kafka.Tests.Json.TestModel;
using TestJsonSerializerContext = AWS.Lambda.Powertools.Kafka.Tests.Json.TestJsonSerializerContext;
using ValueTypeProduct = AWS.Lambda.Powertools.Kafka.Tests.Json.ValueTypeProduct;
using System.Text.Json.Serialization;
using TestKafka;

#if DEBUG
using JsonKafka = AWS.Lambda.Powertools.Kafka;
using AvroKafka = AWS.Lambda.Powertools.Kafka;
using ProtobufKafka = AWS.Lambda.Powertools.Kafka;
#else
using JsonKafka = AWS.Lambda.Powertools.Kafka.Json;
using AvroKafka = AWS.Lambda.Powertools.Kafka.Avro;
using ProtobufKafka = AWS.Lambda.Powertools.Kafka.Protobuf;
#endif

namespace AWS.Lambda.Powertools.Kafka.Tests;

[CollectionDefinition(nameof(NativeAotCompatibilityTests), DisableParallelization = true)]
public class NativeAotCompatibilityCollection
{
}

/// <summary>
/// Runs the serializers as they behave in a Native AOT app, where reflection-based System.Text.Json is disabled.
/// RuntimeFeatureWrapper simulates that; the same paths are verified with real Native AOT binaries.
/// </summary>
[Collection(nameof(NativeAotCompatibilityTests))]
public class NativeAotCompatibilityTests : IDisposable
{
    public NativeAotCompatibilityTests()
    {
        ForEachRuntimeFeatureWrapper("SetIsDynamicCodeSupported", false);
    }

    public void Dispose()
    {
        ForEachRuntimeFeatureWrapper("Reset");
    }

    /// <summary>
    /// In Debug, the Kafka packages share AWS.Lambda.Powertools.Common. In Release, each package compiles its own
    /// copy of Common, so every copy of RuntimeFeatureWrapper is switched.
    /// </summary>
    private static void ForEachRuntimeFeatureWrapper(string method, params object[] args)
    {
        var serializerAssemblies = new[]
        {
            typeof(PowertoolsKafkaJsonSerializer).Assembly,
            typeof(PowertoolsKafkaAvroSerializer).Assembly,
            typeof(PowertoolsKafkaProtobufSerializer).Assembly
        };
        var candidates = serializerAssemblies.Concat(serializerAssemblies
            .SelectMany(a => a.GetReferencedAssemblies())
            .Where(n => n.Name == "AWS.Lambda.Powertools.Common")
            .Select(Assembly.Load));

        foreach (var assembly in candidates.Distinct())
        {
            assembly.GetType("AWS.Lambda.Powertools.Common.Utils.RuntimeFeatureWrapper")
                ?.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, args);
        }
    }

    [Fact]
    public void Json_WithoutReflection_AndNoSerializerContext_ThrowsWithGuidance()
    {
        var serializer = new PowertoolsKafkaJsonSerializer();
        using var stream = KafkaEvent(Base64("key"), Base64("{\"Name\":\"a\",\"Value\":1}"));

        var ex = Assert.Throws<SerializationException>(() =>
            serializer.Deserialize<JsonKafka.ConsumerRecords<string, JsonTestModel>>(stream));

        Assert.Contains("Failed to deserialize value data", ex.Message);
        var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("JsonSerializerContext", inner.Message);
        Assert.Contains(typeof(JsonTestModel).FullName!, inner.Message);
    }

    [Fact]
    public void Json_WithoutReflection_UsesSerializerContext()
    {
        var serializer = new PowertoolsKafkaJsonSerializer(TestJsonSerializerContext.Default);
        using var stream = KafkaEvent(Base64("key"), Base64("{\"Name\":\"a\",\"Value\":1}"));

        var record = serializer.Deserialize<JsonKafka.ConsumerRecords<string, JsonTestModel>>(stream).Single();

        Assert.Equal("key", record.Key);
        Assert.Equal("a", record.Value.Name);
        Assert.Equal(1, record.Value.Value);
        Assert.Equal("headerValue", Encoding.UTF8.GetString(record.Headers["headerKey"]));
    }

    [Fact]
    public void Json_WithoutReflection_UsesTypeInfoResolverFromOptions()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = TestJsonSerializerContext.Default };
        var serializer = new PowertoolsKafkaJsonSerializer(options);
        using var stream = KafkaEvent(Base64("key"), Base64("{\"Name\":\"b\",\"Value\":2}"));

        var record = serializer.Deserialize<JsonKafka.ConsumerRecords<string, JsonTestModel>>(stream).Single();

        Assert.Equal("b", record.Value.Name);
        Assert.Equal(2, record.Value.Value);
    }

    [Fact]
    public void Json_WithoutReflection_PrimitiveKeyAndValue_NeedNoSerializerContext()
    {
        var serializer = new PowertoolsKafkaJsonSerializer();
        using var stream = KafkaEvent(Base64("42"), Base64("hello"));

        var record = serializer.Deserialize<JsonKafka.ConsumerRecords<int, string>>(stream).Single();

        Assert.Equal(42, record.Key);
        Assert.Equal("hello", record.Value);
    }

    [Fact]
    public void Avro_WithoutReflection_DeserializesKeyAndValue()
    {
        var serializer = new PowertoolsKafkaAvroSerializer();
        using var stream = new MemoryStream(File.ReadAllBytes("Avro/kafka-avro-event.json"));

        var records = serializer.Deserialize<AvroKafka.ConsumerRecords<int, AvroProduct>>(stream).ToList();

        Assert.Equal(3, records.Count);
        Assert.Equal("Laptop", records[0].Value.name);
    }

    [Fact]
    public void Protobuf_WithoutReflection_DeserializesKeyAndValue()
    {
        var serializer = new PowertoolsKafkaProtobufSerializer();
        using var stream = new MemoryStream(File.ReadAllBytes("Protobuf/kafka-protobuf-event.json"));

        var records = serializer.Deserialize<ProtobufKafka.ConsumerRecords<int, ProtobufProduct>>(stream).ToList();

        Assert.Equal(3, records.Count);
        Assert.Equal("Laptop", records[0].Value.Name);
    }

    [Fact]
    public void Deserialize_NonKafkaType_WithoutReflection_AndNoSerializerContext_ThrowsWithGuidance()
    {
        var serializer = new PowertoolsKafkaJsonSerializer();
        using var stream = new MemoryStream("{\"Name\":\"a\",\"Value\":1}"u8.ToArray());

        var ex = Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<JsonTestModel>(stream));

        Assert.Contains("JsonSerializerContext", ex.Message);
        Assert.Contains(typeof(JsonTestModel).FullName!, ex.Message);
    }

    [Fact]
    public void Deserialize_NonKafkaType_WithoutReflection_UsesSerializerContext()
    {
        var serializer = new PowertoolsKafkaJsonSerializer(TestJsonSerializerContext.Default);
        using var stream = new MemoryStream("{\"Name\":\"a\",\"Value\":1}"u8.ToArray());

        var result = serializer.Deserialize<JsonTestModel>(stream);

        Assert.Equal("a", result.Name);
    }

    [Fact]
    public void Serialize_String_WithoutReflection_AndNoSerializerContext_WritesJsonString()
    {
        var serializer = new PowertoolsKafkaAvroSerializer();
        using var responseStream = new MemoryStream();

        serializer.Serialize("Processed 3 records", responseStream);

        Assert.Equal("\"Processed 3 records\"", Encoding.UTF8.GetString(responseStream.ToArray()));
    }

    [Fact]
    public void Serialize_WithoutReflection_UsesTypeInfoResolverFromOptions()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = TestJsonSerializerContext.Default };
        var serializer = new PowertoolsKafkaJsonSerializer(options);
        using var responseStream = new MemoryStream();

        serializer.Serialize(new JsonTestModel { Name = "a", Value = 1 }, responseStream);

        Assert.Equal("{\"Name\":\"a\",\"Value\":1}", Encoding.UTF8.GetString(responseStream.ToArray()));
    }

    [Fact]
    public void Serialize_UnknownType_WithoutReflection_AndNoSerializerContext_ThrowsWithGuidance()
    {
        var serializer = new PowertoolsKafkaJsonSerializer();
        using var responseStream = new MemoryStream();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            serializer.Serialize(new JsonTestModel { Name = "a", Value = 1 }, responseStream));

        Assert.Contains("JsonSerializerContext", ex.Message);
        Assert.Contains(typeof(JsonTestModel).FullName!, ex.Message);
    }

    [Fact]
    public void Json_WithoutReflection_EmptyValueForValueType_IsDefault()
    {
        var serializer = new PowertoolsKafkaJsonSerializer();
        using var stream = KafkaEvent(Base64("key"), "");

        var record = serializer.Deserialize<JsonKafka.ConsumerRecords<string, ValueTypeProduct>>(stream).Single();

        Assert.Equal(default, record.Value);
    }

    [Fact]
    public void Json_WithoutReflection_EmptyValueForValueType_UsesSerializerContext()
    {
        var serializer = new PowertoolsKafkaJsonSerializer(AotTestJsonContext.Default);
        using var stream = KafkaEvent(Base64("key"), "");

        var record = serializer.Deserialize<JsonKafka.ConsumerRecords<string, ValueTypeProduct>>(stream).Single();

        Assert.Equal(default, record.Value);
    }

    [Fact]
    public void Deserialize_NonKafkaType_WithoutReflection_UsesTypeInfoResolverFromOptions()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = TestJsonSerializerContext.Default };
        var serializer = new PowertoolsKafkaJsonSerializer(options);
        using var stream = new MemoryStream("{\"Name\":\"a\",\"Value\":1}"u8.ToArray());

        var result = serializer.Deserialize<JsonTestModel>(stream);

        Assert.Equal("a", result.Name);
    }

    [Fact]
    public void Deserialize_NonKafkaType_WithoutReflection_NullJson_Throws()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = TestJsonSerializerContext.Default };
        var serializer = new PowertoolsKafkaJsonSerializer(options);
        using var stream = new MemoryStream("null"u8.ToArray());

        var ex = Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<JsonTestModel>(stream));

        Assert.Contains("Failed to deserialize", ex.Message);
    }

    private static string Base64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static MemoryStream KafkaEvent(string key, string value)
    {
        var json = $$"""
            {
              "eventSource": "aws:kafka",
              "eventSourceArn": "arn:aws:kafka:us-east-1:0123456789019:cluster/TestCluster/abcd1234",
              "bootstrapServers": "b-1.test-cluster.kafka.us-east-1.amazonaws.com:9092",
              "records": {
                "mytopic-0": [
                  {
                    "topic": "mytopic",
                    "partition": 0,
                    "offset": 15,
                    "timestamp": 1645084650987,
                    "timestampType": "CREATE_TIME",
                    "key": "{{key}}",
                    "value": "{{value}}",
                    "headers": [ { "headerKey": [104, 101, 97, 100, 101, 114, 86, 97, 108, 117, 101] } ]
                  }
                ]
              }
            }
            """;
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}

[JsonSerializable(typeof(ValueTypeProduct))]
public partial class AotTestJsonContext : JsonSerializerContext
{
}
