using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

public class PowertoolsLoggingSerializerTests : IDisposable
{
    [Fact]
    public void SerializerOptions_ShouldNotBeNull()
    {
        var options = PowertoolsLoggingSerializer.SerializerOptions;
        Assert.NotNull(options);
    }

    [Fact]
    public void SerializerOptions_ShouldHaveCorrectDefaultSettings()
    {
        var options = PowertoolsLoggingSerializer.SerializerOptions;

        Assert.Collection(options.Converters,
            converter => Assert.IsType<ByteArrayConverter>(converter),
            converter => Assert.IsType<ExceptionConverter>(converter),
            converter => Assert.IsType<MemoryStreamConverter>(converter),
            converter => Assert.IsType<ConstantClassConverter>(converter),
            converter => Assert.IsType<DateOnlyConverter>(converter),
            converter => Assert.IsType<TimeOnlyConverter>(converter),
#if NET8_0_OR_GREATER
            converter => Assert.IsType<JsonStringEnumConverter<LogLevel>>(converter));
#elif NET6_0
            converter => Assert.IsType<JsonStringEnumConverter>(converter));
#endif

        Assert.Equal(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, options.Encoder);

#if NET8_0_OR_GREATER
        Assert.Collection(options.TypeInfoResolverChain,
            resolver => Assert.IsType<PowertoolsLoggingSerializationContext>(resolver));
#endif
    }

    [Fact]
    public void SerializerOptions_ShouldUseSnakeCaseByDefault()
    {
        var json = SerializeTestObject(null);
        Assert.Contains("\"cold_start\"", json);
    }

    [Theory]
    [InlineData(LoggerOutputCase.SnakeCase, "cold_start")]
    [InlineData(LoggerOutputCase.CamelCase, "coldStart")]
    [InlineData(LoggerOutputCase.PascalCase, "ColdStart")]
    public void ConfigureNamingPolicy_ShouldUseCorrectNamingConvention(LoggerOutputCase outputCase,
        string expectedPropertyName)
    {
        var json = SerializeTestObject(outputCase);
        Assert.Contains($"\"{expectedPropertyName}\"", json);
    }

    [Fact]
    public void ConfigureNamingPolicy_ShouldNotChangeWhenPassedNull()
    {
        var originalJson = SerializeTestObject(LoggerOutputCase.SnakeCase);
        var newJson = SerializeTestObject(null);
        Assert.Equal(originalJson, newJson);
    }

    [Fact]
    public void ConfigureNamingPolicy_ShouldNotChangeWhenPassedSameCase()
    {
        var originalJson = SerializeTestObject(LoggerOutputCase.SnakeCase);
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.SnakeCase);
        var newJson = SerializeTestObject(LoggerOutputCase.SnakeCase);
        Assert.Equal(originalJson, newJson);
    }

    [Fact]
    public void Serialize_ShouldHandleNestedObjects()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.SnakeCase);

        var testObject = new LogEntry
        {
            ColdStart = true,
            ExtraKeys = new Dictionary<string, object>
            {
                { "NestedObject", new Dictionary<string, string> { { "PropertyName", "Value" } } }
            }
        };

        var json = JsonSerializer.Serialize(testObject, PowertoolsLoggingSerializer.SerializerOptions);
        Assert.Contains("\"cold_start\":true", json);
        Assert.Contains("\"nested_object\":{\"property_name\":\"Value\"}", json);
    }

    [Fact]
    public void Serialize_ShouldHandleEnumValues()
    {
        var testObject = new LogEntry
        {
            Level = LogLevel.Error
        };
        var json = JsonSerializer.Serialize(testObject, PowertoolsLoggingSerializer.SerializerOptions);
        Assert.Contains("\"level\":\"Error\"", json);
    }

    private string SerializeTestObject(LoggerOutputCase? outputCase)
    {
        if (outputCase.HasValue)
        {
            PowertoolsLoggingSerializer.ConfigureNamingPolicy(outputCase.Value);
        }

        LogEntry testObject = new LogEntry { ColdStart = true };
        return JsonSerializer.Serialize(testObject, PowertoolsLoggingSerializer.SerializerOptions);
    }

    public void Dispose()
    {
#if NET8_0_OR_GREATER
        PowertoolsLoggingSerializer.ClearContext();
#endif
    }
}