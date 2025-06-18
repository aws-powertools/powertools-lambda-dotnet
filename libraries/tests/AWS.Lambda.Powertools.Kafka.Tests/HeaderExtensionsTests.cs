using System.Text;
using Xunit;

namespace AWS.Lambda.Powertools.Kafka.Tests
{
    public class HeaderExtensionsTests
    {
        [Fact]
        public void DecodedValues_WithValidHeaders_DecodesCorrectly()
        {
            // Arrange
            var headers = new Dictionary<string, byte[]>
            {
                { "header1", Encoding.UTF8.GetBytes("value1") },
                { "header2", Encoding.UTF8.GetBytes("value2") }
            };

            // Act
            var decoded = headers.DecodedValues();

            // Assert
            Assert.Equal(2, decoded.Count);
            Assert.Equal("value1", decoded["header1"]);
            Assert.Equal("value2", decoded["header2"]);
        }

        [Fact]
        public void DecodedValues_WithEmptyDictionary_ReturnsEmptyDictionary()
        {
            // Arrange
            var headers = new Dictionary<string, byte[]>();

            // Act
            var decoded = headers.DecodedValues();

            // Assert
            Assert.Empty(decoded);
        }

        [Fact]
        public void DecodedValues_WithNullDictionary_ReturnsEmptyDictionary()
        {
            // Arrange
            Dictionary<string, byte[]> headers = null;

            // Act
            var decoded = headers.DecodedValues();

            // Assert
            Assert.Empty(decoded);
        }

        [Fact]
        public void DecodedValue_WithValidBytes_DecodesCorrectly()
        {
            // Arrange
            var bytes = Encoding.UTF8.GetBytes("test-value");

            // Act
            var decoded = bytes.DecodedValue();

            // Assert
            Assert.Equal("test-value", decoded);
        }

        [Fact]
        public void DecodedValue_WithEmptyBytes_ReturnsEmptyString()
        {
            // Arrange
            var bytes = Array.Empty<byte>();

            // Act
            var decoded = bytes.DecodedValue();

            // Assert
            Assert.Equal("", decoded);
        }

        [Fact]
        public void DecodedValue_WithNullBytes_ReturnsEmptyString()
        {
            // Act
            var decoded = ((byte[])null).DecodedValue();

            // Assert
            Assert.Equal("", decoded);
        }
    }
}