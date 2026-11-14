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


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.ExtraKeys.ExtraKeysDictionary
{
// --8<-- [start:extra_keys_dictionary]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        var orderId = apigProxyEvent.PathParameters["orderId"];

        using (Logger.ExtraKeys(new Dictionary<string, object> { { "orderId", orderId } }))
        {
            Logger.LogInformation("Processing order");
            await ProcessOrderAsync(orderId);
            Logger.LogInformation("Order processed"); // orderId included
        }
        // orderId is automatically removed

        Logger.LogInformation("Continuing without orderId");
        return new APIGatewayProxyResponse();
    }

    private Task ProcessOrderAsync(string orderId) => Task.CompletedTask;
}
// --8<-- [end:extra_keys_dictionary]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.ExtraKeys.ExtraKeysTuples
{
// --8<-- [start:extra_keys_tuples]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        var orderId = apigProxyEvent.PathParameters["orderId"];

        using (Logger.ExtraKeys(("orderId", orderId), ("customerId", "customer-123")))
        {
            Logger.LogInformation("Processing order");
            await ProcessOrderAsync(orderId);
            Logger.LogInformation("Order processed"); // orderId and customerId included
        }
        // Both keys are automatically removed
        return new APIGatewayProxyResponse();
    }

    private Task ProcessOrderAsync(string orderId) => Task.CompletedTask;
}
// --8<-- [end:extra_keys_tuples]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.ExtraKeys.ExtraKeysNested
{
// --8<-- [start:extra_keys_nested]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        using (Logger.ExtraKeys(("requestId", context.AwsRequestId)))
        {
            Logger.LogInformation("Starting request"); // requestId included

            using (Logger.ExtraKeys(("step", "validation")))
            {
                Logger.LogInformation("Validating"); // requestId AND step included
            }
            // step removed, requestId still present

            Logger.LogInformation("Request complete"); // only requestId
        }
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:extra_keys_nested]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.ExtraKeys.ExtraKeysSingleEntry
{
// --8<-- [start:extra_keys_single_entry]
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

        var lookupId = new Dictionary<string, object>()
        {
            { "LookupId", requestContextRequestId }
        };

        // Appended keys are added to all subsequent log entries in the current execution.
        // Call this method as early as possible in the Lambda handler.
        // Typically this is value would be passed into the function via the event.
        // Set the ClearState = true to force the removal of keys across invocations,
        Logger.AppendKeys(lookupId);
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:extra_keys_single_entry]
}
