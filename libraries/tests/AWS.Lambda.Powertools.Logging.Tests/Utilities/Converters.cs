using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Utilities;

public class ByteArrayConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ByteArrayConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new ByteArrayConverter());
    }

    [Fact]
    public void Write_WhenByteArrayIsNull_WritesNullValue()
    {
        // Arrange
        var testObject = new TestClass { Data = null };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);

        // Assert
        Assert.Contains("\"data\":null", json);
    }

    [Fact]
    public void Write_WithByteArray_WritesBase64String()
    {
        // Arrange
        byte[] testData = { 1, 2, 3, 4, 5 };
        var testObject = new TestClass { Data = testData };
        var expectedBase64 = Convert.ToBase64String(testData);

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);

        // Assert
        Assert.Contains($"\"data\":\"{expectedBase64}\"", json);
    }

    [Fact]
    public void Read_WithBase64String_ReturnsByteArray()
    {
        // Arrange
        byte[] expectedData = { 1, 2, 3, 4, 5 };
        var base64 = Convert.ToBase64String(expectedData);
        var json = $"{{\"data\":\"{base64}\"}}";

        // Act
        var result = JsonSerializer.Deserialize<TestClass>(json, _options);

        // Assert
        Assert.Equal(expectedData, result.Data);
    }

    [Fact]
    public void Read_WithInvalidType_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"data\":123}";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TestClass>(json, _options));
    }

    [Fact]
    public void Read_WithEmptyString_ReturnsEmptyByteArray()
    {
        // Arrange
        var json = "{\"data\":\"\"}";

        // Act
        var result = JsonSerializer.Deserialize<TestClass>(json, _options);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }

    [Fact]
    public void WriteAndRead_RoundTrip_PreservesData()
    {
        // Arrange
        byte[] originalData = Encoding.UTF8.GetBytes("Test data with special chars: !@#$%^&*()");
        var testObject = new TestClass { Data = originalData };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);
        var deserializedObject = JsonSerializer.Deserialize<TestClass>(json, _options);

        // Assert
        Assert.Equal(originalData, deserializedObject.Data);
    }

    private class TestClass
    {
        [JsonPropertyName("data")] public byte[] Data { get; set; }
    }

    [Fact]
    public void ByteArrayConverter_Write_ShouldHandleNullValue()
    {
        // Arrange
        var converter = new ByteArrayConverter();
        var options = new JsonSerializerOptions();
        var testObject = new { Data = (byte[])null };

        // Act
        var json = JsonSerializer.Serialize(testObject, options);

        // Assert
        Assert.Contains("\"Data\":null", json);
    }

    [Fact]
    public void ByteArrayConverter_Read_ShouldHandleNullToken()
    {
        // Arrange
        var converter = new ByteArrayConverter();
        var json = "{\"Data\":null}";
        var options = new JsonSerializerOptions();
        options.Converters.Add(converter);

        // Act
        var result = JsonSerializer.Deserialize<TestByteArrayClass>(json, options);

        // Assert
        Assert.Null(result.Data);
    }

    [Fact]
    public void ByteArrayConverter_Read_ShouldHandleStringToken()
    {
        // Arrange
        var converter = new ByteArrayConverter();
        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        var base64String = Convert.ToBase64String(expectedBytes);
        var json = $"{{\"Data\":\"{base64String}\"}}";

        var options = new JsonSerializerOptions();
        options.Converters.Add(converter);

        // Act
        var result = JsonSerializer.Deserialize<TestByteArrayClass>(json, options);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal(expectedBytes, result.Data);
    }

    [Fact]
    public void ByteArrayConverter_Read_ShouldThrowOnInvalidToken()
    {
        // Arrange
        var converter = new ByteArrayConverter();
        var json = "{\"Data\":123}"; // Number instead of string

        var options = new JsonSerializerOptions();
        options.Converters.Add(converter);

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TestByteArrayClass>(json, options));

        Assert.Contains("Expected string value for byte array", ex.Message);
    }

// Helper class for testing byte array deserialization
    private class TestByteArrayClass
    {
        public byte[] Data { get; set; }
    }
}