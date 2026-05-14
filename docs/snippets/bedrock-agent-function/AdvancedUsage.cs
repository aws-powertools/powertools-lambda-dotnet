// This file is referenced by docs/core/event_handler/bedrock_agent_function.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.BedrockAgentFunction;

// --8<-- [start:custom_type_serialization]
resolver.Tool(
    name: "PriceCalculator",
    description: "Calculate total price with tax",
    handler: (MyCustomType myCustomType) =>
    {
        var withTax = myCustomType.Price * 1.2m;
        return $"Total price with tax: {withTax.ToString("F2", CultureInfo.InvariantCulture)}";
    }
);
// --8<-- [end:custom_type_serialization]

// --8<-- [start:custom_type_serialization_aot]
var resolver = new BedrockAgentFunctionResolver(MycustomSerializationContext.Default);
resolver.Tool(
    name: "PriceCalculator",
    description: "Calculate total price with tax",
    handler: (MyCustomType myCustomType) =>
    {
        var withTax = myCustomType.Price * 1.2m;
        return $"Total price with tax: {withTax.ToString("F2", CultureInfo.InvariantCulture)}";
    }
);

[JsonSerializable(typeof(MyCustomType))]
public partial class MycustomSerializationContext : JsonSerializerContext
{
}
// --8<-- [end:custom_type_serialization_aot]

// --8<-- [start:accessing_lambda_context]
resolver.Tool(
    "LogRequest",
    "Logs request information and returns confirmation",
    (string requestId, ILambdaContext context) =>
    {
        context.Logger.LogLine($"Processing request {requestId}");
        return $"Request {requestId} logged successfully";
    });
// --8<-- [end:accessing_lambda_context]

// --8<-- [start:handling_errors]
resolver.Tool("CustomFailure", () =>
{
    // Return a custom FAILURE response
    return new BedrockFunctionResponse
    {
        Response = new Response
        {
            ActionGroup = "TestGroup",
            Function = "CustomFailure",
            FunctionResponse = new FunctionResponse
            {
                ResponseBody = new ResponseBody
                {
                    Text = new TextBody
                    {
                        Body = "Critical error occurred: Database unavailable"
                    }
                },
                ResponseState = ResponseState.FAILURE  // Mark as FAILURE to abort the conversation
            }
        }
    };
});
// --8<-- [end:handling_errors]

// --8<-- [start:session_attributes]
// Create a counter tool that reads and updates session attributes
resolver.Tool("CounterTool", (BedrockFunctionRequest request) =>
{
    // Read the current count from session attributes
    int currentCount = 0;
    if (request.SessionAttributes != null &&
        request.SessionAttributes.TryGetValue("counter", out var countStr) &&
        int.TryParse(countStr, out var count))
    {
        currentCount = count;
    }

    // Increment the counter
    currentCount++;

    // Create a new dictionary with updated counter
    var updatedSessionAttributes = new Dictionary<string, string>(request.SessionAttributes ?? new Dictionary<string, string>())
    {
        ["counter"] = currentCount.ToString(),
        ["lastAccessed"] = DateTime.UtcNow.ToString("o")
    };

    // Return response with updated session attributes
    return new BedrockFunctionResponse
    {
        Response = new Response
        {
            ActionGroup = request.ActionGroup,
            Function = request.Function,
            FunctionResponse = new FunctionResponse
            {
                ResponseBody = new ResponseBody
                {
                    Text = new TextBody { Body = $"Current count: {currentCount}" }
                }
            }
        },
        SessionAttributes = updatedSessionAttributes,
        PromptSessionAttributes = request.PromptSessionAttributes
    };
});
// --8<-- [end:session_attributes]

// --8<-- [start:async_functions]
_resolver.Tool(
    "FetchUserData",
    "Fetches user data from external API",
    async (string userId, ILambdaContext ctx) =>
    {
        // Log the request
        ctx.Logger.LogLine($"Fetching data for user {userId}");

        // Simulate API call
        await Task.Delay(100);

        // Return user information
        return new { Id = userId, Name = "John Doe", Status = "Active" }.ToString();
    });
// --8<-- [end:async_functions]

// --8<-- [start:direct_access_request]
_resolver.Tool(
    "ProcessRawRequest",
    "Processes the raw Bedrock Agent request",
    (BedrockFunctionRequest input) =>
    {
        var functionName = input.Function;
        var parameterCount = input.Parameters.Count;
        return $"Received request for {functionName} with {parameterCount} parameters";
    });
// --8<-- [end:direct_access_request]
