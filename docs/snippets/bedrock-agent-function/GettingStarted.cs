// This file is referenced by docs/core/event_handler/bedrock_agent_function.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.BedrockAgentFunction;

// --8<-- [start:executable_assembly]
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

var resolver = new BedrockAgentFunctionResolver();

resolver
    .Tool("GetWeather", (string city) => $"The weather in {city} is sunny")
    .Tool("CalculateSum", (int a, int b) => $"The sum of {a} and {b} is {a + b}")
    .Tool("GetCurrentTime", () => $"The current time is {DateTime.Now}");

// The function handler that will be called for each Lambda event
var handler = async (BedrockFunctionRequest input, ILambdaContext context) =>
{
    return await resolver.ResolveAsync(input, context);
};

// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types.
await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();
// --8<-- [end:executable_assembly]

// --8<-- [start:class_library]
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyLambdaFunction
{
    public class Function
    {
        private readonly BedrockAgentFunctionResolver _resolver;

        public Function()
        {
            _resolver = new BedrockAgentFunctionResolver();

            // Register simple tool functions
            _resolver
                .Tool("GetWeather", (string city) => $"The weather in {city} is sunny")
                .Tool("CalculateSum", (int a, int b) => $"The sum of {a} and {b} is {a + b}")
                .Tool("GetCurrentTime", () => $"The current time is {DateTime.Now}");
        }

        // Lambda handler function
        public BedrockFunctionResponse FunctionHandler(
            BedrockFunctionRequest input, ILambdaContext context)
        {
            return _resolver.Resolve(input, context);
        }
    }
}
// --8<-- [end:class_library]
