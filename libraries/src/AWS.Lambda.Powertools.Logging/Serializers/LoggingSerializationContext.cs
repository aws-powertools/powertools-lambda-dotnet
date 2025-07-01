using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Logging.Serializers;

#if NET8_0_OR_GREATER

/// <summary>
/// Custom JSON serializer context for AWS.Lambda.Powertools.Logging
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Int32))]
[JsonSerializable(typeof(Double))]
[JsonSerializable(typeof(DateOnly))]
[JsonSerializable(typeof(TimeOnly))]
[JsonSerializable(typeof(IEnumerable<object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(Byte[]))]
[JsonSerializable(typeof(MemoryStream))]
[JsonSerializable(typeof(LogEntry))]
public partial class PowertoolsLoggingSerializationContext : JsonSerializerContext
{
}


#endif