// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing;

// --8<-- [start:tracing_attribute]
public class Function
{
    [Tracing]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        await BusinessLogic1()
            .ConfigureAwait(false);

        await BusinessLogic2()
            .ConfigureAwait(false);
    }

    [Tracing]
    private async Task BusinessLogic1()
    {

    }

    [Tracing]
    private async Task BusinessLogic2()
    {

    }
}
// --8<-- [end:tracing_attribute]

// --8<-- [start:custom_segment_name]
public class Function
{
    [Tracing(SegmentName = "YourCustomName")]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        ...
    }
}
// --8<-- [end:custom_segment_name]

// --8<-- [start:disable_capture_mode]
public class Function
{
    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        ...
    }
}
// --8<-- [end:disable_capture_mode]
