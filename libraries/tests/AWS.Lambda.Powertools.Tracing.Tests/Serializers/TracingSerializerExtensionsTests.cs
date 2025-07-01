
#if NET8_0_OR_GREATER
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Tracing.Serializers;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests.Serializers;

public class TracingSerializerExtensionsTests
{
    [Fact]
    public void WithTracing_InitializesSerializer_Successfully()
    {
        // Arrange
        var serializer = new SourceGeneratorLambdaJsonSerializer<TestJsonContext>();

        // Act
        var result = serializer.WithTracing();

        // Assert
        Assert.NotNull(result);
        
        // Verify the context was initialized by attempting to serialize
        var testObject = new TestPerson { Name = "Test", Age = 25 };
        var serialized = PowertoolsTracingSerializer.Serialize(testObject);
        Assert.Contains("\"Name\":\"Test\"", serialized);
    }
}
#endif