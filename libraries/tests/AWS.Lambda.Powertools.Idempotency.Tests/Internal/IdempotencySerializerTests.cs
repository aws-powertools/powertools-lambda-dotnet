using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

public class IdempotencySerializerTests
{
    public IdempotencySerializerTests()
    {
#if NET8_0_OR_GREATER
        IdempotencySerializer.AddTypeInfoResolver(TestJsonSerializerContext.Default);
#endif
    }

    [Fact]
    public void Serialize_ValidObject_ReturnsJsonString()
    {
        // Arrange
        var testObject = new TestClass { Id = 1, Name = "Test" };

        // Act
        var result = IdempotencySerializer.Serialize(testObject, typeof(TestClass));

        // Assert
        Assert.Contains("\"Id\":1", result);
        Assert.Contains("\"Name\":\"Test\"", result);
    }

    [Fact]
    public void Deserialize_ValidJsonString_ReturnsObject()
    {
        // Arrange
        var json = "{\"Id\":1,\"Name\":\"Test\"}";

        // Act
        var result = IdempotencySerializer.Deserialize<TestClass>(json);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

#if NET8_0_OR_GREATER

    [Fact]
    public void GetTypeInfo_UnknownType_ThrowsException()
    {
        // Arrange
        var mockResolver = Substitute.For<IJsonTypeInfoResolver>();
        mockResolver.GetTypeInfo(typeof(TestClass), Arg.Any<JsonSerializerOptions>())
            .Returns((JsonTypeInfo)null);

        var options = new JsonSerializerOptions();
        options.TypeInfoResolver = mockResolver;

        // Use reflection to set the private _jsonOptions field
        var field = typeof(IdempotencySerializer).GetField("_jsonOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field!.SetValue(null, options);

        // Act & Assert
        var exception = Assert.Throws<SerializationException>(() => IdempotencySerializer.GetTypeInfo(typeof(TestClass)));
        Assert.Equal("Type AWS.Lambda.Powertools.Idempotency.Tests.Model.TestClass is not known to the serializer. Ensure it's included in the JsonSerializerContext.", exception.Message);
    }

    [Fact]
    public void AddTypeInfoResolver_AddsResolverToChain()
    {
        // Arrange
        var mockContext = new TestJsonSerializerContext();

        // Act
        IdempotencySerializer.AddTypeInfoResolver(mockContext);

        // Assert
        // Use reflection to get the private _jsonOptions field
        var field = typeof(IdempotencySerializer).GetField("_jsonOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var options = (JsonSerializerOptions)field!.GetValue(null);

        Assert.Contains(mockContext, options!.TypeInfoResolverChain);
    }
#endif
}