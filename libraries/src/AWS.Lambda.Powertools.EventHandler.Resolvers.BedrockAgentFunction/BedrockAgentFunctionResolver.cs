using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers;

// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler.Resolvers
{
    /// <summary>
    /// A resolver for Bedrock Agent functions that allows registering handlers for tool functions.
    /// </summary>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var resolver = new BedrockAgentFunctionResolver();
    /// resolver.Tool("GetWeather", (string city) => $"Weather in {city} is sunny");
    /// 
    /// // Lambda handler
    /// public BedrockFunctionResponse FunctionHandler(BedrockFunctionRequest input, ILambdaContext context)
    /// {
    ///     return resolver.Resolve(input, context);
    /// }
    /// </code>
    /// </example>
    public class BedrockAgentFunctionResolver
    {
        private readonly
            Dictionary<string, Func<BedrockFunctionRequest, ILambdaContext?, BedrockFunctionResponse>>
            _handlers = new();

        private readonly ParameterTypeValidator _parameterValidator = new();
        private readonly ResultConverter _resultConverter = new();
        private readonly ParameterMapper _parameterMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="BedrockAgentFunctionResolver"/> class.
        /// Optionally accepts a type resolver for JSON serialization.
        /// </summary>
        public BedrockAgentFunctionResolver(IJsonTypeInfoResolver? typeResolver = null)
        {
            _parameterMapper = new ParameterMapper(typeResolver);
            SystemWrapper.Instance.SetExecutionEnvironment(this);
        }
        
        /// <summary>
        /// Checks if another tool can be registered, and logs a warning if the maximum limit is reached
        /// or if a tool with the same name is already registered
        /// </summary>
        /// <param name="name">The name of the tool being registered</param>
        /// <returns>True if the tool can be registered, false if the maximum limit is reached</returns>
        private bool CanRegisterTool(string name)
        {
            if (_handlers.ContainsKey(name))
            {
                Console.WriteLine($"WARNING: Tool {name} already registered. Overwriting with new definition.");
            }
            
            return true;
        }

        /// <summary>
        /// Registers a handler that directly accepts BedrockFunctionRequest and returns BedrockFunctionResponse
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The handler function that accepts input and context and returns output</param>
        /// <param name="description">Optional description of the tool function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<BedrockFunctionRequest, ILambdaContext?, BedrockFunctionResponse> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = handler;
            return this;
        }

        /// <summary>
        /// Registers a handler that directly accepts BedrockFunctionRequest and returns BedrockFunctionResponse
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The handler function that accepts input and returns output</param>
        /// <param name="description">Optional description of the tool function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<BedrockFunctionRequest, BedrockFunctionResponse> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (input, _) => handler(input);
            return this;
        }

        /// <summary>
        /// Registers a parameter-less handler that returns BedrockFunctionResponse
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The handler function that returns output</param>
        /// <param name="description">Optional description of the tool function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<BedrockFunctionResponse> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (_, _) => handler();
            return this;
        }

        /// <summary>
        /// Registers a parameter-less handler with automatic string conversion
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The handler function that returns a string</param>
        /// <param name="description">Optional description of the tool function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<string> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (input, _) => BedrockFunctionResponse.WithText(
                handler(), 
                input.ActionGroup, 
                name,
                input.SessionAttributes,
                input.PromptSessionAttributes,
                new Dictionary<string, string>());
            return this;
        }

        /// <summary>
        /// Registers a parameter-less handler with automatic object conversion
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The handler function that returns an object</param>
        /// <param name="description">Optional description of the tool function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<object> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (input, _) =>
            {
                var result = handler();
                return _resultConverter.ConvertToOutput(result, input);
            };
            return this;
        }

        /// <summary>
        /// Registers a handler for a tool function with automatically converted return type (no description).
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The delegate handler function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Delegate handler)
        {
            return Tool<object>(name, "", handler);
        }

        /// <summary>
        /// Registers a handler for a tool function with description and automatically converted return type.
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The delegate handler function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool(
            string name,
            string description,
            Delegate handler)
        {
            return Tool<object>(name, description, handler);
        }

        /// <summary>
        /// Registers a handler for a tool function with typed return value (no description).
        /// </summary>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The delegate handler function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool<TResult>(
            string name,
            Delegate handler)
        {
            return Tool<TResult>(name, "", handler);
        }

        /// <summary>
        /// Registers a handler for a tool function with description and typed return value.
        /// </summary>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The delegate handler function</param>
        /// <returns>The resolver instance for method chaining</returns>
        public BedrockAgentFunctionResolver Tool<TResult>(
            string name,
            string description,
            Delegate handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = RegisterToolHandler<TResult>(handler, name);
            return this;
        }

        private Func<BedrockFunctionRequest, ILambdaContext?, BedrockFunctionResponse> RegisterToolHandler<TResult>(
            Delegate handler, string functionName)
        {
            return (input, context) =>
            {
                try
                {
                    // Map parameters from Bedrock input and DI
                    var serviceProvider = (this as DiBedrockAgentFunctionResolver)?.ServiceProvider;
                    var args = _parameterMapper.MapParameters(handler.Method, input, context, serviceProvider);
                    
                    // Execute the handler and process result
                    return ExecuteHandlerAndProcessResult<TResult>(handler, args, input, context, functionName);
                }
                catch (Exception ex)
                {
                    context?.Logger.LogError(ex.ToString());
                    var innerException = ex.InnerException ?? ex;
                    return BedrockFunctionResponse.WithText(
                        $"Error when invoking tool: {innerException.Message}",
                        input.ActionGroup,
                        functionName,
                        input.SessionAttributes,
                        input.PromptSessionAttributes,
                        new Dictionary<string, string>());
                }
            };
        }

        private BedrockFunctionResponse ExecuteHandlerAndProcessResult<TResult>(
            Delegate handler, 
            object?[] args, 
            BedrockFunctionRequest input, 
            ILambdaContext? context,
            string functionName)
        {
            try
            {
                // Execute the handler
                var result = handler.DynamicInvoke(args);

                // Process various result types
                return _resultConverter.ProcessResult<TResult>(result, input, functionName, context);
            }
            catch (Exception ex)
            {
                context?.Logger.LogError(ex.ToString());
                var innerException = ex.InnerException ?? ex;
                return BedrockFunctionResponse.WithText(
                    $"Error when invoking tool: {innerException.Message}",
                    input.ActionGroup,
                    functionName,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }
        }

        /// <summary>
        /// Resolves and processes a Bedrock Agent function invocation.
        /// </summary>
        /// <param name="input">The Bedrock Agent input containing the function name and parameters</param>
        /// <param name="context">Optional Lambda context</param>
        /// <returns>The output from the function execution</returns>
        public BedrockFunctionResponse Resolve(BedrockFunctionRequest input, ILambdaContext? context = null)
        {
            return ResolveAsync(input, context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously resolves and processes a Bedrock Agent function invocation.
        /// </summary>
        /// <param name="input">The Bedrock Agent input containing the function name and parameters</param>
        /// <param name="context">Optional Lambda context</param>
        /// <returns>A task that completes with the output from the function execution</returns>
        public async Task<BedrockFunctionResponse> ResolveAsync(BedrockFunctionRequest input,
            ILambdaContext? context = null)
        {
            return await Task.FromResult(HandleEvent(input, context));
        }

        private BedrockFunctionResponse HandleEvent(BedrockFunctionRequest input, ILambdaContext? context)
        {
            if (string.IsNullOrEmpty(input.Function))
            {
                return BedrockFunctionResponse.WithText(
                    "No tool specified in the request", 
                    input.ActionGroup, 
                    "",
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            if (_handlers.TryGetValue(input.Function, out var handler))
            {
                try
                {
                    return handler(input, context);
                }
                catch (Exception ex)
                {
                    context?.Logger.LogError(ex.ToString());
                    return BedrockFunctionResponse.WithText(
                        $"Error when invoking tool: {ex.Message}",
                        input.ActionGroup,
                        input.Function,
                        input.SessionAttributes,
                        input.PromptSessionAttributes,
                        new Dictionary<string, string>());
                }
            }

            context?.Logger.LogWarning($"Tool {input.Function} has not been registered.");
            return BedrockFunctionResponse.WithText(
                $"Error: Tool {input.Function} has not been registered in handler",
                input.ActionGroup, 
                input.Function,
                input.SessionAttributes,
                input.PromptSessionAttributes,
                new Dictionary<string, string>());
        }
    }
}
