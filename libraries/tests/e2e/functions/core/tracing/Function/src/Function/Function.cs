using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Tracing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Function;

public class Function
{
    [Tracing(Namespace = "Test")]
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        BusinessLogic1();

        Tracing.WithSubsegment("loggingResponse", (subsegment) =>
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
    private string ToUpper(string input)
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
    private void BusinessLogic1()
    {
        NestedBusinessLogic1();
    }

    [Tracing]
    private void NestedBusinessLogic1()
    {
        Tracing.WithSubsegment("localNamespace", "loggingResponse", (subsegment) =>
        {
            // Some business logic
            NestedBusinessLogic2();
        });
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    private void NestedBusinessLogic2()
    {
    }

    class MetadataObject
    {
        public int Id { get; set; }
        public string? Value { get; set; }
    }
}