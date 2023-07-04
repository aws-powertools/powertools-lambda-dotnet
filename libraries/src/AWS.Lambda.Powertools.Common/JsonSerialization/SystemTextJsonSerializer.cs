namespace AWS.Lambda.Powertools.Common;

using System.Text.Json;

public class SystemTextJsonSerializer: IPowerToolsSerializer
{
    /// <inheritdoc />
    public void InternalSerialize<T>(Utf8JsonWriter writer, T response, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, response, options);
    }

    /// <inheritdoc />
    public string InternalSerializeAsString<T>(T response, JsonSerializerOptions options = null)
    {
        return JsonSerializer.Serialize(
            response,
            options);
    }
}