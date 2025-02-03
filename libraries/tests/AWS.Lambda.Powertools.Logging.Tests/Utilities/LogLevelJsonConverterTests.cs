using System.Text.Json;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Utilities;

public class LogLevelJsonConverterTests
{
    private readonly LogLevelJsonConverter _converter;
    private readonly JsonSerializerOptions _options;

    public LogLevelJsonConverterTests()
    {
        _converter = new LogLevelJsonConverter();
        _options = new JsonSerializerOptions
        {
            Converters = { _converter }
        };
    }

    [Theory]
    [InlineData("Information", LogLevel.Information)]
    [InlineData("Error", LogLevel.Error)]
    [InlineData("Warning", LogLevel.Warning)]
    [InlineData("Debug", LogLevel.Debug)]
    [InlineData("Trace", LogLevel.Trace)]
    [InlineData("Critical", LogLevel.Critical)]
    [InlineData("None", LogLevel.None)]
    public void Read_ValidLogLevel_ReturnsCorrectEnum(string input, LogLevel expected)
    {
        // Arrange
        var json = $"\"{input}\"";
        
        // Act
        var result = JsonSerializer.Deserialize<LogLevel>(json, _options);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("information", LogLevel.Information)]
    [InlineData("ERROR", LogLevel.Error)]
    [InlineData("Warning", LogLevel.Warning)]
    [InlineData("deBUG", LogLevel.Debug)]
    public void Read_CaseInsensitive_ReturnsCorrectEnum(string input, LogLevel expected)
    {
        // Arrange
        var json = $"\"{input}\"";
        
        // Act
        var result = JsonSerializer.Deserialize<LogLevel>(json, _options);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("InvalidLevel")]
    [InlineData("NotALevel")]
    public void Read_InvalidLogLevel_ReturnsDefault(string input)
    {
        // Arrange
        var json = $"\"{input}\"";
        
        // Act
        var result = JsonSerializer.Deserialize<LogLevel>(json, _options);
        
        // Assert
        Assert.Equal(default(LogLevel), result);
    }

    [Fact]
    public void Read_NullValue_ReturnsDefault()
    {
        // Arrange
        var json = "null";
        
        // Act
        var result = JsonSerializer.Deserialize<LogLevel>(json, _options);
        
        // Assert
        Assert.Equal(default(LogLevel), result);
    }

    [Theory]
    [InlineData(LogLevel.Information, "Information")]
    [InlineData(LogLevel.Error, "Error")]
    [InlineData(LogLevel.Warning, "Warning")]
    [InlineData(LogLevel.Debug, "Debug")]
    [InlineData(LogLevel.Trace, "Trace")]
    [InlineData(LogLevel.Critical, "Critical")]
    [InlineData(LogLevel.None, "None")]
    public void Write_ValidLogLevel_WritesCorrectString(LogLevel input, string expected)
    {
        // Act
        var result = JsonSerializer.Serialize(input, _options);
        
        // Assert
        Assert.Equal($"\"{expected}\"", result);
    }

    [Fact]
    public void Write_DefaultLogLevel_WritesCorrectString()
    {
        // Arrange
        var input = default(LogLevel);
        
        // Act
        var result = JsonSerializer.Serialize(input, _options);
        
        // Assert
        Assert.Equal($"\"{input}\"", result);
    }

    [Fact]
    public void Converter_CanConvert_LogLevelType()
    {
        // Act
        var canConvert = _converter.CanConvert(typeof(LogLevel));
        
        // Assert
        Assert.True(canConvert);
    }

    [Fact]
    public void SerializeAndDeserialize_RoundTrip_MaintainsValue()
    {
        // Arrange
        var originalValue = LogLevel.Information;
        
        // Act
        var serialized = JsonSerializer.Serialize(originalValue, _options);
        var deserialized = JsonSerializer.Deserialize<LogLevel>(serialized, _options);
        
        // Assert
        Assert.Equal(originalValue, deserialized);
    }
}