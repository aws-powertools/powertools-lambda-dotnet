using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace Docs.TracingSnippets.AnnotationsAndMetadata.add_annotation
{
// --8<-- [start:add_annotation]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    [Tracing]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Tracing.AddAnnotation("annotation", "value");
    return null;
    }
}
// --8<-- [end:add_annotation]
}

namespace Docs.TracingSnippets.AnnotationsAndMetadata.add_metadata
{
// --8<-- [start:add_metadata]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    [Tracing]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Tracing.AddMetadata("content", "value");
    return null;
    }
}
// --8<-- [end:add_metadata]
}
