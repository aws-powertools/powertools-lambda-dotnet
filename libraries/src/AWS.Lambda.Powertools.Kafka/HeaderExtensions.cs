using System.Text;

namespace AWS.Lambda.Powertools.Kafka;

/// <summary>
/// Extension methods for Kafka headers in ConsumerRecord.
/// </summary>
public static class HeaderExtensions
{
    /// <summary>
    /// Gets the decoded value of a Kafka header from the ConsumerRecord's Headers dictionary.
    /// </summary>
    /// <param name="headers">The header key-value pair from ConsumerRecord.Headers</param>
    /// <returns>The decoded string value.</returns>
    public static Dictionary<string, string> DecodedValues(this Dictionary<string, byte[]> headers)
    {
        if (headers == null)
        {
            return new Dictionary<string, string>();
        }

        return headers.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.DecodedValue()
        );
    }
    
    /// <summary>
    /// Decodes a byte array from a Kafka header into a UTF-8 string.
    /// Returns an empty string if the byte array is null or empty.
    /// </summary>
    public static string DecodedValue(this byte[]? headerBytes)
    {
        if (headerBytes == null || headerBytes.Length == 0)
        {
            return string.Empty;
        }
            
        return Encoding.UTF8.GetString(headerBytes);
    }
}