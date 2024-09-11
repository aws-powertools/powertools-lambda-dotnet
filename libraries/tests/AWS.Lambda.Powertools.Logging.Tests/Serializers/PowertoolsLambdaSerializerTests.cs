
#if NET8_0_OR_GREATER

using AWS.Lambda.Powertools.Logging.Serializers;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

[JsonSerializable(typeof(TestObject))]
public partial class TestJsonContext : JsonSerializerContext
{
}

public class TestObject
{
    public string FullName { get; set; }
    public int Age { get; set; }
}

public class PowertoolsLambdaSerializerTests : IDisposable
{
    [Fact]
    public void Constructor_ShouldNotThrowException()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => new PowertoolsLambdaSerializer(TestJsonContext.Default));
        Assert.Null(exception);
    }
    
    [Fact]
    public void Constructor_ShouldAddCustomerContext()
    {
        // Arrange
        var customerContext = new TestJsonContext();
    
        // Act
        var serializer = new PowertoolsLambdaSerializer(customerContext);
    
        // Assert
        Assert.True(PowertoolsLoggingSerializer.HasContext(customerContext));
    }

    // [Fact]
    // public void Deserialize_ValidJson_ShouldReturnDeserializedObject()
    // {
    //     // Arrange
    //     var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
    //     var json = "{\"Name\":\"John\",\"Age\":30}";
    //     var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
    //
    //     // Act
    //     var result = serializer.Deserialize<TestObject>(stream);
    //
    //     // Assert
    //     Assert.NotNull(result);
    //     Assert.Equal("John", result.Name);
    //     Assert.Equal(30, result.Age);
    // }
    
    [Theory]
    [InlineData(LoggerOutputCase.CamelCase,"{\"fullName\":\"John\",\"age\":30}", "John", 30)]
    [InlineData(LoggerOutputCase.PascalCase,"{\"FullName\":\"Jane\",\"Age\":25}", "Jane", 25)]
    [InlineData(LoggerOutputCase.SnakeCase,"{\"full_name\":\"Jane\",\"age\":25}", "Jane", 25)]
    public void Deserialize_ValidJson_ShouldReturnDeserializedObject(LoggerOutputCase outputCase,string json, string expectedName, int expectedAge)
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(outputCase);
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = serializer.Deserialize<TestObject>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedName, result.FullName);
        Assert.Equal(expectedAge, result.Age);
    }

    [Fact]
    public void Deserialize_InvalidType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.PascalCase);
        
        var json = "{\"FullName\":\"John\",\"Age\":30}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<UnknownType>(stream));
    }

    [Fact]
    public void Serialize_ValidObject_ShouldSerializeToStream()
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.PascalCase);
        
        var testObject = new TestObject { FullName = "Jane", Age = 25 };
        var stream = new MemoryStream();

        // Act
        serializer.Serialize(testObject, stream);

        // Assert
        stream.Position = 0;
        var result = new StreamReader(stream).ReadToEnd();
        Assert.Contains("\"FullName\":\"Jane\"", result);
        Assert.Contains("\"Age\":25", result);
    }

    [Fact]
    public void Serialize_InvalidType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        var unknownObject = new UnknownType();
        var stream = new MemoryStream();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => serializer.Serialize(unknownObject, stream));
    }

    private class UnknownType { }

    public void Dispose()
    {
        PowertoolsLoggingSerializer.ClearContext();
    }
}
#endif

