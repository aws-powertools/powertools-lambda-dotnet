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
    /// <param name="header">The header key-value pair from ConsumerRecord.Headers</param>
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
    
    public static string DecodedValue(this byte[]? headerBytes)
    {
        if (headerBytes == null || headerBytes.Length == 0)
        {
            return string.Empty;
        }
            
        return Encoding.UTF8.GetString(headerBytes);
    }
}