/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

public class PowertoolsLoggingSerializerTests : IDisposable
{

    public PowertoolsLoggingSerializerTests()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggingConstants.DefaultLoggerOutputCase);
#if NET8_0_OR_GREATER
        PowertoolsLoggingSerializer.ClearContext();
#endif
    }
    
    [Fact]
    public void SerializerOptions_ShouldNotBeNull()
    {
        var options = PowertoolsLoggingSerializer.GetSerializerOptions();
        Assert.NotNull(options);
    }

    [Fact]
    public void SerializerOptions_ShouldHaveCorrectDefaultSettings()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        
        var options = PowertoolsLoggingSerializer.GetSerializerOptions(); 
        
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
    public void SerializerOptions_ShouldHaveCorrectDefaultSettings_WhenDynamic()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);
        
        var options = PowertoolsLoggingSerializer.GetSerializerOptions();
        
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

        var json = JsonSerializer.Serialize(testObject, PowertoolsLoggingSerializer.GetSerializerOptions());
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
        var json = JsonSerializer.Serialize(testObject, PowertoolsLoggingSerializer.GetSerializerOptions());
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
            PowertoolsLoggingSerializer.Serialize(unknownObject, typeof(UnknownType)));

        Assert.Contains("is not known to the serializer", exception.Message);
        Assert.Contains(typeof(UnknownType).ToString(), exception.Message);
    }
    
    [Fact]
    public void Serialize_UnknownType_Should_Not_Throw_InvalidOperationException_When_Dynamic()
    {
        // Arrange
        var unknownObject = new UnknownType{ SomeProperty = "Hello"};

        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);
        // Act & Assert
        var expected =
            PowertoolsLoggingSerializer.Serialize(unknownObject, typeof(UnknownType));

        Assert.Equal("{\"some_property\":\"Hello\"}", expected);
    }

    private class UnknownType
    {
        public string SomeProperty { get; set; }
    }
#endif

    private string SerializeTestObject(LoggerOutputCase? outputCase)
    {
        if (outputCase.HasValue)
        {
            PowertoolsLoggingSerializer.ConfigureNamingPolicy(outputCase.Value);
        }

        LogEntry testObject = new LogEntry { ColdStart = true };
        return JsonSerializer.Serialize(testObject, PowertoolsLoggingSerializer.GetSerializerOptions());
    }

    public void Dispose()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggingConstants.DefaultLoggerOutputCase);
#if NET8_0_OR_GREATER
        PowertoolsLoggingSerializer.ClearContext();
#endif
        PowertoolsLoggingSerializer.ClearOptions();
        RuntimeFeatureWrapper.Reset();
    }
}