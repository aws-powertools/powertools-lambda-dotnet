// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing;

// --8<-- [start:add_annotation]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    [Tracing]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Tracing.AddAnnotation("annotation", "value");
    }
}
// --8<-- [end:add_annotation]

// --8<-- [start:add_metadata]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    [Tracing]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Tracing.AddMetadata("content", "value");
    }
}
// --8<-- [end:add_metadata]
