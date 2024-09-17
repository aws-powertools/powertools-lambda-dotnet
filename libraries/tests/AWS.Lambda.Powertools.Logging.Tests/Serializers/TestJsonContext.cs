using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using Amazon.Lambda.CloudWatchEvents.S3Events;
using Amazon.Lambda.TestUtilities;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

[JsonSerializable(typeof(S3ObjectCreateEvent))]
[JsonSerializable(typeof(TestObject))]
[JsonSerializable(typeof(TestLambdaContext))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(ApplicationLoadBalancerRequest))]
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