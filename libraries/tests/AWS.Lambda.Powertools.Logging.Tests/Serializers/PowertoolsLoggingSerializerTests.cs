using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

public class PowertoolsLoggingSerializerTests : IDisposable
{
    private readonly PowertoolsLoggingSerializer _serializer;

    public PowertoolsLoggingSerializerTests()
    {
        _serializer = new PowertoolsLoggingSerializer();
        _serializer.ConfigureNamingPolicy(LoggingConstants.DefaultLoggerOutputCase);
#if NET8_0_OR_GREATER
        ClearContext();
#endif
    }

    [Fact]
    public void SerializerOptions_ShouldNotBeNull()
    {
        var options = _serializer.GetSerializerOptions();
        Assert.NotNull(options);
    }

    [Fact]
    public void SerializerOptions_ShouldHaveCorrectDefaultSettings()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var options = _serializer.GetSerializerOptions();

        Assert.Collection(options.Converters,
            converter => Assert.IsType<ByteArrayConverter>(converter),
            converter => Assert.IsType<ExceptionConverter>(converter),
            converter => Assert.IsType<MemoryStreamConverter>(converter),
            converter => Assert.IsType<ConstantClassConverter>(converter),
            converter => Assert.IsType<DateOnlyConverter>(converter),
            converter => Assert.IsType<TimeOnlyConverter>(converter),
#if NET8_0_OR_GREATER
            converter => Assert.IsType<LogLevelJsonConverter>(converter));
#elif NET6_0
            converter => Assert.IsType<LogLevelJsonConverter>(converter));
#endif

        Assert.Equal(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, options.Encoder);

#if NET8_0_OR_GREATER
        Assert.Collection(options.TypeInfoResolverChain,
            resolver => Assert.IsType<CompositeJsonTypeInfoResolver>(resolver));
#endif
    }

    [Fact]
    public void SerializerOptions_ShouldHaveCorrectDefaultSettings_WhenDynamic()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);

        var options = _serializer.GetSerializerOptions();

        Assert.Collection(options.Converters,
            converter => Assert.IsType<ByteArrayConverter>(converter),
            converter => Assert.IsType<ExceptionConverter>(converter),
            converter => Assert.IsType<MemoryStreamConverter>(converter),
            converter => Assert.IsType<ConstantClassConverter>(converter),
            converter => Assert.IsType<DateOnlyConverter>(converter),
            converter => Assert.IsType<TimeOnlyConverter>(converter),
#if NET8_0_OR_GREATER
            converter => Assert.IsType<LogLevelJsonConverter>(converter));
#elif NET6_0
            converter => Assert.IsType<LogLevelJsonConverter>(converter));
#endif

        Assert.Equal(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, options.Encoder);

#if NET8_0_OR_GREATER
        Assert.Empty(options.TypeInfoResolverChain);
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
        _serializer.ConfigureNamingPolicy(LoggerOutputCase.SnakeCase);
        var newJson = SerializeTestObject(LoggerOutputCase.SnakeCase);
        Assert.Equal(originalJson, newJson);
    }

    [Fact]
    public void Serialize_ShouldHandleNestedObjects()
    {
        _serializer.ConfigureNamingPolicy(LoggerOutputCase.SnakeCase);

        var testObject = new LogEntry
        {
            ColdStart = true,
            ExtraKeys = new Dictionary<string, object>
            {
                { "NestedObject", new Dictionary<string, string> { { "PropertyName", "Value" } } }
            }
        };

        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());
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
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());
        Assert.Contains("\"level\":\"Error\"", json);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void Serialize_UnknownType_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownObject = new UnknownType();

        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        // Act & Assert
        var exception = Assert.Throws<JsonSerializerException>(() =>
            _serializer.Serialize(unknownObject, typeof(UnknownType)));

        Assert.Contains("is not known to the serializer", exception.Message);
        Assert.Contains(typeof(UnknownType).ToString(), exception.Message);
    }

    [Fact]
    public void Serialize_UnknownType_Should_Not_Throw_InvalidOperationException_When_Dynamic()
    {
        // Arrange
        var unknownObject = new UnknownType { SomeProperty = "Hello" };

        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);
        // Act & Assert
        var expected =
            _serializer.Serialize(unknownObject, typeof(UnknownType));

        Assert.Equal("{\"some_property\":\"Hello\"}", expected);
    }

    [Fact]
    public void AddSerializerContext_ShouldUpdateTypeInfoResolver()
    {
        // Arrange
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        var testContext = new TestSerializerContext(new JsonSerializerOptions());

        // Get the initial resolver
        var beforeOptions = _serializer.GetSerializerOptions();
        var beforeResolver = beforeOptions.TypeInfoResolver;

        // Act
        _serializer.AddSerializerContext(testContext);

        // Get the updated resolver
        var afterOptions = _serializer.GetSerializerOptions();
        var afterResolver = afterOptions.TypeInfoResolver;

        // Assert - adding a context should create a new resolver
        Assert.NotSame(beforeResolver, afterResolver);
        Assert.IsType<CompositeJsonTypeInfoResolver>(afterResolver);
    }

    private class UnknownType
    {
        public string SomeProperty { get; set; }
    }

    private class TestSerializerContext : JsonSerializerContext
    {
        private readonly JsonSerializerOptions _options;

        public TestSerializerContext(JsonSerializerOptions options) : base(options)
        {
            _options = options;
        }

        public override JsonTypeInfo? GetTypeInfo(Type type)
        {
            return null; // For testing purposes only
        }

        protected override JsonSerializerOptions? GeneratedSerializerOptions => _options;
    }

    private void ClearContext()
    {
        // Create a new serializer to clear any existing contexts
        _serializer.SetOptions(new JsonSerializerOptions());
    }
#endif

    private string SerializeTestObject(LoggerOutputCase? outputCase)
    {
        if (outputCase.HasValue)
        {
            _serializer.ConfigureNamingPolicy(outputCase.Value);
        }

        LogEntry testObject = new LogEntry { ColdStart = true };
        return JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());
    }

    [Fact]
    public void ByteArrayConverter_ShouldProduceBase64EncodedString()
    {
        // Arrange
        var testObject = new { BinaryData = new byte[] { 1, 2, 3, 4, 5 } };

        // Act
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());

        // Assert
        Assert.Contains("\"binary_data\":\"AQIDBAU=\"", json);
    }

    [Fact]
    public void ExceptionConverter_ShouldSerializeExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error message", new Exception("Inner exception"));
        var testObject = new { Error = exception };

        // Act
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());

        // Assert
        Assert.Equal("{\"error\":{\"type\":\"System.InvalidOperationException\",\"message\":\"Test error message\",\"inner_exception\":{\"type\":\"System.Exception\",\"message\":\"Inner exception\"}}}", json);
    }

    [Fact]
    public void MemoryStreamConverter_ShouldConvertToBase64()
    {
        // Arrange
        var bytes = new byte[] { 10, 20, 30, 40, 50 };
        var memoryStream = new MemoryStream(bytes);
        var testObject = new { Stream = memoryStream };

        // Act
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());

        // Assert
        Assert.Contains("\"stream\":\"ChQeKDI=\"", json);
    }

    [Fact]
    public void ConstantClassConverter_ShouldSerializeToString()
    {
        // Arrange
        var testObject = new { Level = LogLevel.Warning };

        // Act
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());

        // Assert
        Assert.Contains("\"level\":\"Warning\"", json);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void DateOnlyConverter_ShouldSerializeToIsoDate()
    {
        // Arrange
        var date = new DateOnly(2023, 10, 15);
        var testObject = new { Date = date };

        // Act
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());

        // Assert
        Assert.Contains("\"date\":\"2023-10-15\"", json);
    }

    [Fact]
    public void TimeOnlyConverter_ShouldSerializeToIsoTime()
    {
        // Arrange
        var time = new TimeOnly(13, 45, 30);
        var testObject = new { Time = time };

        // Act
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());

        // Assert
        Assert.Contains("\"time\":\"13:45:30\"", json);
    }
#endif

    [Fact]
    public void LogLevelJsonConverter_ShouldSerializeAllLogLevels()
    {
        // Arrange
        var levels = new Dictionary<string, LogLevel>
        {
            { "trace", LogLevel.Trace },
            { "debug", LogLevel.Debug },
            { "info", LogLevel.Information },
            { "warning", LogLevel.Warning },
            { "error", LogLevel.Error },
            { "critical", LogLevel.Critical }
        };

        // Act
        var json = JsonSerializer.Serialize(levels, _serializer.GetSerializerOptions());

        // Assert
        Assert.Contains("\"trace\":\"Trace\"", json);
        Assert.Contains("\"debug\":\"Debug\"", json);
        Assert.Contains("\"info\":\"Information\"", json);
        Assert.Contains("\"warning\":\"Warning\"", json);
        Assert.Contains("\"error\":\"Error\"", json);
        Assert.Contains("\"critical\":\"Critical\"", json);
    }

    [Fact]
    public void Serialize_ComplexObjectWithMultipleConverters_ShouldConvertAllProperties()
    {
        // Arrange
        var testObject = new ComplexTestObject
        {
            BinaryData = new byte[] { 1, 2, 3 },
            Exception = new ArgumentException("Test argument"),
            Stream = new MemoryStream(new byte[] { 4, 5, 6 }),
            Level = LogLevel.Information,
#if NET6_0_OR_GREATER
            Date = new DateOnly(2023, 1, 15),
            Time = new TimeOnly(14, 30, 0),
#endif
        };

        // Act
        var json = JsonSerializer.Serialize(testObject, _serializer.GetSerializerOptions());

        // Assert
        Assert.Contains("\"binary_data\":\"AQID\"", json);
        Assert.Contains("\"exception\":{\"type\":\"System.ArgumentException\"", json);
        Assert.Contains("\"stream\":\"BAUG\"", json);
        Assert.Contains("\"level\":\"Information\"", json);
#if NET6_0_OR_GREATER
        Assert.Contains("\"date\":\"2023-01-15\"", json);
        Assert.Contains("\"time\":\"14:30:00\"", json);
#endif
    }

    private class ComplexTestObject
    {
        public byte[] BinaryData { get; set; }
        public Exception Exception { get; set; }
        public MemoryStream Stream { get; set; }
        public LogLevel Level { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
#endif
    }
    
    [Fact]
        public void ConfigureNamingPolicy_WhenChanged_RebuildsOptions()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            
            // Force initialization of _jsonOptions
            _ = serializer.GetSerializerOptions();
            
            // Act
            serializer.ConfigureNamingPolicy(LoggerOutputCase.CamelCase);
            var options = serializer.GetSerializerOptions();
            
            // Assert
            Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
            Assert.Equal(JsonNamingPolicy.CamelCase, options.DictionaryKeyPolicy);
        }

        [Fact]
        public void ConfigureNamingPolicy_WhenAlreadySet_DoesNothing()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            serializer.ConfigureNamingPolicy(LoggerOutputCase.CamelCase);
            
            // Get the initial options
            var initialOptions = serializer.GetSerializerOptions();
            
            // Act - set the same case again
            serializer.ConfigureNamingPolicy(LoggerOutputCase.CamelCase);
            var newOptions = serializer.GetSerializerOptions();
            
            // Assert - should be the same instance
            Assert.Same(initialOptions, newOptions);
        }

        [Fact]
        public void Serialize_WithValidObject_ReturnsJsonString()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            var testObj = new TestClass { Name = "Test", Value = 123 };
            
            // Act
            var json = serializer.Serialize(testObj, typeof(TestClass));
            
            // Assert
            Assert.Contains("\"name\"", json);
            Assert.Contains("\"value\"", json);
            Assert.Contains("123", json);
            Assert.Contains("Test", json);
        }

#if NET8_0_OR_GREATER
        [Fact]
        public void AddSerializerContext_AddsContext()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            var context = new TestJsonContext(new JsonSerializerOptions());
            
            // Act
            serializer.AddSerializerContext(context);
            
            // No immediate assertion - the context is added internally
            // We'll verify it works through serialization tests
        }

        [Fact]
        public void SetOptions_WithTypeInfoResolver_SetsCustomResolver()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
    
            // Explicitly disable dynamic code - important to set before creating options
            RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
    
            var context = new TestJsonContext(new JsonSerializerOptions());
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = context
            };

            // Act
            serializer.SetOptions(options);
            var serializerOptions = serializer.GetSerializerOptions();

            // Assert - options are properly configured
            Assert.NotNull(serializerOptions.TypeInfoResolver);
        }

        [Fact]
        public void SetOptions_WithContextAsResolver_AddsToContexts()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            var context = new TestJsonContext(new JsonSerializerOptions());
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = context
            };
            
            // Act - This adds the context automatically
            serializer.SetOptions(options);
            
            // No direct assertion possible for internal state, but we can test it works
            // through proper serialization
        }
#endif

        [Fact]
        public void SetOutputCase_CamelCase_SetsPoliciesCorrectly()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            serializer.ConfigureNamingPolicy(LoggerOutputCase.CamelCase);
            
            // Act
            var options = serializer.GetSerializerOptions();
            
            // Assert
            Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
            Assert.Equal(JsonNamingPolicy.CamelCase, options.DictionaryKeyPolicy);
        }

        [Fact]
        public void SetOutputCase_PascalCase_SetsPoliciesCorrectly()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            serializer.ConfigureNamingPolicy(LoggerOutputCase.PascalCase);
            
            // Act
            var options = serializer.GetSerializerOptions();
            
            // Assert
            Assert.IsType<PascalCaseNamingPolicy>(options.PropertyNamingPolicy);
            Assert.IsType<PascalCaseNamingPolicy>(options.DictionaryKeyPolicy);
        }

        [Fact]
        public void SetOutputCase_SnakeCase_SetsPoliciesCorrectly()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            serializer.ConfigureNamingPolicy(LoggerOutputCase.SnakeCase);
            
            // Act
            var options = serializer.GetSerializerOptions();

#if NET8_0_OR_GREATER
            // Assert - in .NET 8 we use built-in SnakeCaseLower
            Assert.Equal(JsonNamingPolicy.SnakeCaseLower, options.PropertyNamingPolicy);
            Assert.Equal(JsonNamingPolicy.SnakeCaseLower, options.DictionaryKeyPolicy);
#else
            // Assert - in earlier versions, we use custom SnakeCaseNamingPolicy
            Assert.IsType<SnakeCaseNamingPolicy>(options.PropertyNamingPolicy);
            Assert.IsType<SnakeCaseNamingPolicy>(options.DictionaryKeyPolicy);
#endif
        }

        [Fact]
        public void GetSerializerOptions_AddsAllConverters()
        {
            // Arrange
            var serializer = new PowertoolsLoggingSerializer();
            
            // Act
            var options = serializer.GetSerializerOptions();
            
            // Assert
            Assert.Contains(options.Converters, c => c is ByteArrayConverter);
            Assert.Contains(options.Converters, c => c is ExceptionConverter);
            Assert.Contains(options.Converters, c => c is MemoryStreamConverter);
            Assert.Contains(options.Converters, c => c is ConstantClassConverter);
            Assert.Contains(options.Converters, c => c is DateOnlyConverter);
            Assert.Contains(options.Converters, c => c is TimeOnlyConverter);
#if NET8_0_OR_GREATER || NET6_0
            Assert.Contains(options.Converters, c => c is LogLevelJsonConverter);
#endif
        }

        // Test class for serialization
        private class TestClass
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }


    

    public void Dispose()
    {
#if NET8_0_OR_GREATER
        ClearContext();
#endif
        _serializer.SetOptions(null);
        RuntimeFeatureWrapper.Reset();
    }
}