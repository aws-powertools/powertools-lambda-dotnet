using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.Core;

// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler
{
    /// <summary>
    /// Resolver for Amazon Bedrock Agent functions.
    /// Routes function calls to appropriate handlers based on function name.
    /// </summary>
    public class BedrockAgentFunctionResolver
    {
        private readonly Dictionary<string, Func<ActionGroupInvocationInput, ILambdaContext?, ActionGroupInvocationOutput>> _handlers = new();

        /// <summary>
        /// Registers a handler for a tool function without parameters.
        /// </summary>
        /// <param name="name">The function name to handle</param>
        /// <param name="handler">Function handler without parameters</param>
        /// <param name="description">Optional description of the function</param>
        public BedrockAgentFunctionResolver Tool(string name, Func<ActionGroupInvocationOutput> handler, string description = "")
        {
            _handlers[name] = (_, _) => handler();
            return this;
        }

        /// <summary>
        /// Registers a handler for a tool function with input.
        /// </summary>
        /// <param name="name">The function name to handle</param>
        /// <param name="handler">Function handler with input</param>
        /// <param name="description">Optional description of the function</param>
        public BedrockAgentFunctionResolver Tool(string name, Func<ActionGroupInvocationInput, ActionGroupInvocationOutput> handler, string description = "")
        {
            _handlers[name] = (input, _) => handler(input);
            return this;
        }

        /// <summary>
        /// Registers a handler for a tool function with input and context.
        /// </summary>
        /// <param name="name">The function name to handle</param>
        /// <param name="handler">Function handler with input and context</param>
        /// <param name="description">Optional description of the function</param>
        public BedrockAgentFunctionResolver Tool(string name, Func<ActionGroupInvocationInput, ILambdaContext, ActionGroupInvocationOutput> handler, string description = "")
        {
            _handlers[name] = (input, context) => handler(input, context ?? throw new ArgumentNullException(nameof(context)));
            return this;
        }

        /// <summary>
        /// Resolves and processes a Bedrock Agent function invocation.
        /// </summary>
        /// <param name="input">The function invocation input</param>
        /// <param name="context">Lambda execution context</param>
        public ActionGroupInvocationOutput Resolve(ActionGroupInvocationInput input, ILambdaContext? context = null)
        {
            return ResolveAsync(input, context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously resolves and processes a Bedrock Agent function invocation.
        /// </summary>
        /// <param name="input">The function invocation input</param>
        /// <param name="context">Lambda execution context</param>
        public async Task<ActionGroupInvocationOutput> ResolveAsync(ActionGroupInvocationInput input, ILambdaContext? context = null)
        {
            return await Task.FromResult(HandleEvent(input, context));
        }

        private ActionGroupInvocationOutput HandleEvent(ActionGroupInvocationInput input, ILambdaContext? context)
        {
            if (string.IsNullOrEmpty(input.Function))
            {
                return new ActionGroupInvocationOutput
                {
                    Text = "No function specified in the request"
                };
            }

            if (_handlers.TryGetValue(input.Function, out var handler))
            {
                try
                {
                    return handler(input, context);
                }
                catch (Exception ex)
                {
                    context?.Logger.LogError($"Error executing function {input.Function}: {ex.Message}");
                    return new ActionGroupInvocationOutput
                    {
                        Text = $"Error executing function: {ex.Message}"
                    };
                }
            }

            context?.Logger.LogWarning($"No handler registered for function: {input.Function}");
            return new ActionGroupInvocationOutput
            {
                Text = $"No handler registered for function: {input.Function}"
            };
        }
    }
}