using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.CallbackPattern.functional_api
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
// --8<-- [start:functional_api]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Tracing.WithSubsegment("loggingResponse", (subsegment) => {
            // Some business logic
        });

        Tracing.WithSubsegment("localNamespace", "loggingResponse", (subsegment) => {
            // Some business logic
        });
    return null;
    }
}
// --8<-- [end:functional_api]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.CallbackPattern.multi_threaded
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
// --8<-- [start:multi_threaded]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // Extract existing trace data
        var entity = Tracing.GetEntity();

        var task = Task.Run(() =>
        {
            Tracing.WithSubsegment("InlineLog", entity, (subsegment) =>
            {
                // Business logic in separate task
            });
        });
    return null;
    }
}
// --8<-- [end:multi_threaded]
}
