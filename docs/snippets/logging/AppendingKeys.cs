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


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AppendingKeys.AppendKeys
{
// --8<-- [start:append_keys]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(LogEvent = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
        ILambdaContext context)
    {
        var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;

    var lookupInfo = new Dictionary<string, object>()
    {
        {"LookupInfo", new Dictionary<string, object>{{ "LookupId", requestContextRequestId }}}
    };

    // Appended keys are added to all subsequent log entries in the current execution.
    // Call this method as early as possible in the Lambda handler.
    // Typically this is value would be passed into the function via the event.
    // Set the ClearState = true to force the removal of keys across invocations,
    Logger.AppendKeys(lookupInfo);

    Logger.LogInformation("Getting ip address from external service");

        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:append_keys]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AppendingKeys.RemoveKeys
{
// --8<-- [start:remove_keys]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(LogEvent = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // ...
        Logger.AppendKey("test", "willBeLogged");
        // ...
        var customKeys = new Dictionary<string, string>
        {
            {"test1", "value1"},
            {"test2", "value2"}
        };

        Logger.AppendKeys(customKeys);
        // ...
        Logger.RemoveKeys("test");
        Logger.RemoveKeys("test1", "test2");
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:remove_keys]
}
