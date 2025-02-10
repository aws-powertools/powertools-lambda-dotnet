using System;
using System.Text.Json;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

[Collection("Sequential")]
public class SerializationTests
{
    [Fact]
    public void Metrics_Resolution_JsonConverter()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MetricResolutionJsonConverter());

        {
            var myInt = JsonSerializer.Deserialize<MetricResolution>(1, options);
            Assert.Equal(MetricResolution.High, myInt);
            Assert.Equal("1", JsonSerializer.Serialize(myInt, options));
        }

        {
            var myInt = JsonSerializer.Deserialize<MetricResolution>("1", options);
            Assert.Equal(MetricResolution.High, myInt);
            Assert.Equal("1", JsonSerializer.Serialize(myInt, options));
        }

        {
            var myInt = JsonSerializer.Deserialize<MetricResolution>("60", options);
            Assert.Equal(MetricResolution.Standard, myInt);
            Assert.Equal("60", JsonSerializer.Serialize(myInt, options));
        }

        {
            var myInt = JsonSerializer.Deserialize<MetricResolution>("0", options);
            Assert.Equal(MetricResolution.Default, myInt);
            Assert.Equal("0", JsonSerializer.Serialize(myInt, options));
        }

        {
            var myInt = JsonSerializer.Deserialize<MetricResolution>(@"""1""", options);
            Assert.Equal(MetricResolution.High, myInt);
            Assert.Equal("1", JsonSerializer.Serialize(myInt, options));
        }
        {
            var myInt = JsonSerializer.Deserialize<MetricResolution>(@"""60""", options);
            Assert.Equal(MetricResolution.Standard, myInt);
            Assert.Equal("60", JsonSerializer.Serialize(myInt, options));
        }
        {
            var myInt = JsonSerializer.Deserialize<MetricResolution>(@"""0""", options);
            Assert.Equal(MetricResolution.Default, myInt);
            Assert.Equal("0", JsonSerializer.Serialize(myInt, options));
        }
    }

    [Fact]
    public void Metrics_Resolution_JsonConverter_Exception()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MetricResolutionJsonConverter());

        {
            Action act = () => JsonSerializer.Deserialize<MetricResolution>("1.3", options);
            Assert.Throws<JsonException>(act);
        }
        
        {
            Action act = () => JsonSerializer.Deserialize<MetricResolution>(@"""1.3""", options);
            Assert.Throws<JsonException>(act);
        }
        
        {
            Action act = () => JsonSerializer.Deserialize<MetricResolution>(@"""abc""", options);
            Assert.Throws<JsonException>(act);
        }
    }
}