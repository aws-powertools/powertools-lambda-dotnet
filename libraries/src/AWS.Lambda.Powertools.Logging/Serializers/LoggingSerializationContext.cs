using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Logging.Serializers;

/// <summary>
/// Custom JSON serializer context for AWS.Lambda.Powertools.Logging
/// </summary>
// LogEntry.Exception is typed System.Exception, so the source generator emits metadata for Exception's
// properties, including TargetSite, which has [RequiresUnreferencedCode]. That metadata is never used:
// ExceptionConverter is always registered (PowertoolsLoggingSerializer.AddConverters) and takes precedence,
// and it doesn't read TargetSite.
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
    Justification = "Only Exception.TargetSite in generated Exception metadata, which ExceptionConverter replaces at runtime.")]
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