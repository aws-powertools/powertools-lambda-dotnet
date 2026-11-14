using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.ClearState.ClearState
{
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
        // ...
        if (apigProxyEvent.Headers.ContainsKey("SomeSpecialHeader"))
        {
            Logger.AppendKey("SpecialKey", "value");
        }

        Logger.LogInformation("Collecting payment");
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:clear_state]
}
