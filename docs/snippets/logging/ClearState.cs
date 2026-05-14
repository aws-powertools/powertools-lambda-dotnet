// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

// --8<-- [start:clear_state]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(ClearState = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        ...
        if (apigProxyEvent.Headers.ContainsKey("SomeSpecialHeader"))
        {
            Logger.AppendKey("SpecialKey", "value");
        }

        Logger.LogInformation("Collecting payment");
        ...
    }
}
// --8<-- [end:clear_state]
