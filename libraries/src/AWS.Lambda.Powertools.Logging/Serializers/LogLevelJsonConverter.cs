using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Serializers;

internal class LogLevelJsonConverter : JsonConverter<LogLevel>
{
    public override LogLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Enum.TryParse<LogLevel>(reader.GetString(),true, out var val) ? val : default;
    }

    public override void Write(Utf8JsonWriter writer, LogLevel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}