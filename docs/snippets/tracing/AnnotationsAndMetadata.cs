using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.AnnotationsAndMetadata.add_annotation
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
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

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.AnnotationsAndMetadata.add_metadata
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
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
