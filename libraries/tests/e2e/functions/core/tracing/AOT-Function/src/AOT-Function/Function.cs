using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Tracing.Serializers;
using AWS.Lambda.Powertools.Tracing;

namespace AOT_Function;

public static class Function
{
    private static async Task Main()
    {
        Func<APIGatewayProxyRequest, ILambdaContext, APIGatewayProxyResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler,
                new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>()
                    .WithTracing())
            .Build()
            .RunAsync();
    }

    [Tracing(Namespace = "Test")]
    public static APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
        ILambdaContext context)
    {
        BusinessLogic1();

        Tracing.WithSubsegment("BusinessLogic2", (subsegment) =>
        {
            // Some business logic
        });

        return new APIGatewayProxyResponse()
        {
            StatusCode = 200,
            Body = ToUpper(apigwProxyEvent.Body)
        };
    }

    [Tracing(SegmentName = "ConvertToUpper")]
    private static string ToUpper(string input)
    {
        Tracing.AddAnnotation("annotation", input);
        Tracing.AddMetadata("metadata", new MetadataObject
        {
            Id = 1,
            Value = input
        });

        return input.ToUpper();
    }

    [Tracing]
    private static void BusinessLogic1()
    {
        NestedBusinessLogic1();
        NestedBusinessLogic3();
    }

    [Tracing]
    private static void NestedBusinessLogic1()
    {
        Tracing.WithSubsegment("localNamespace", "Inside-NestedBusinessLogic1", (subsegment) =>
        {
            // Some business logic
            NestedBusinessLogic2();
        });
    }
    private static void NestedBusinessLogic2()
    {
        // Some business logic
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    private static void NestedBusinessLogic3()
    {
        // Some business logic
    }
    
}

public class MetadataObject
{
    public int Id { get; set; }
    public string? Value { get; set; }
}

[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(MetadataObject))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}