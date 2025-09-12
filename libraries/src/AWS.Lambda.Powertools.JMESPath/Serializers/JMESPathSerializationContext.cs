using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.JMESPath.Serializers;

/// <summary>
/// The source generated JsonSerializerContext to be used to Serialize JMESPath types 
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(JsonElement))]
public partial class JmesPathSerializationContext : JsonSerializerContext
{
}