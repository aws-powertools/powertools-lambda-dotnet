using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
/// Class MetricResolutionJsonConverter.
/// Implements the <see cref="System.Text.Json.Serialization.JsonConverter{T}" />
/// </summary>
/// <seealso cref="System.Text.Json.Serialization.JsonConverter{T}" />
public class MetricResolutionJsonConverter : JsonConverter<MetricResolution>
{
    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="T:Utf8JsonReader" /> to read from.</param>
    /// <param name="typeToConvert">The <see cref="T:System.Type" /> being converted.</param>
    /// <param name="options">The <see cref="T:Utf8JsonSerializerOptions" /> being used.</param>
    /// <returns>The object value.</returns>
    public override MetricResolution Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (int.TryParse(stringValue, out int value))
            {
                return (MetricResolution)value;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return (MetricResolution)reader.GetInt32();
        }

        throw new JsonException();
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="T:Utf8JsonWriter" /> to write to.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The <see cref="T:Utf8JsonSerializerOptions" /> being used.</param>
    public override void Write(Utf8JsonWriter writer, MetricResolution value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((int)value);
    }
}