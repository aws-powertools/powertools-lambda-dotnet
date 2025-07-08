using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler.Resolvers;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(long[]))]
[JsonSerializable(typeof(double[]))]
[JsonSerializable(typeof(bool[]))]
[JsonSerializable(typeof(decimal[]))]
internal partial class BedrockFunctionResolverContext : JsonSerializerContext
{
}