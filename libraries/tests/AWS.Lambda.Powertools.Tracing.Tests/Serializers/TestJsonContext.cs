
#if NET8_0_OR_GREATER

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Tracing.Tests.Serializers;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(TestPerson))]
[JsonSerializable(typeof(TestComplexObject))]
[JsonSerializable(typeof(TestResponse))]
public partial class TestJsonContext : JsonSerializerContext { }

public class TestPerson
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class TestComplexObject
{
    public string StringValue { get; set; }
    public int NumberValue { get; set; }
    public bool BoolValue { get; set; }
    public Dictionary<string, object> NestedObject { get; set; }
}

#endif

public class TestResponse
{
    public string Message { get; set; }
}