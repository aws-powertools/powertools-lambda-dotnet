using System.Text.Json.Serialization;
using Amazon.Lambda.CloudWatchEvents.S3Events;

namespace AWS.Lambda.Powertools.Logging.Tests.Utilities;

[JsonSerializable(typeof(S3ObjectCreateEvent))]
[JsonSerializable(typeof(TestObject))]
internal partial class TestJsonContext : JsonSerializerContext
{
}

internal class TestObject
{
    public string FullName { get; set; }
    public int Age { get; set; }

    public Header Headers { get; set; }
}

internal class Header
{
    public string MyRequestIdHeader { get; set; }
}