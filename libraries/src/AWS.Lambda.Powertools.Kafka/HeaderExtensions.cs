/*
 * Copyright JsonCons.Net authors. All Rights Reserved.
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

using System.Text;

#if KAFKA_JSON
namespace AWS.Lambda.Powertools.Kafka.Json;
#elif KAFKA_AVRO
namespace AWS.Lambda.Powertools.Kafka.Avro;
#elif KAFKA_PROTOBUF
namespace AWS.Lambda.Powertools.Kafka.Protobuf;
#else
namespace AWS.Lambda.Powertools.Kafka;
#endif

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