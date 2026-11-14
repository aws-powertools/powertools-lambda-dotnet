using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace Docs.TracingSnippets.CallbackPattern.functional_api
{
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

namespace Docs.TracingSnippets.CallbackPattern.multi_threaded
{
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
