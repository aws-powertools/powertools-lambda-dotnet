using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
        [RequiresUnreferencedCode(AotMessages.DelegateTool)]
        [RequiresDynamicCode(AotMessages.DelegateTool)]
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
        [RequiresUnreferencedCode(AotMessages.DelegateTool)]
        [RequiresDynamicCode(AotMessages.DelegateTool)]
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
        [RequiresUnreferencedCode(AotMessages.DelegateTool)]
        [RequiresDynamicCode(AotMessages.DelegateTool)]
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
        [RequiresUnreferencedCode(AotMessages.DelegateTool)]
        [RequiresDynamicCode(AotMessages.DelegateTool)]
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

        /// <summary>
        /// Registers a tool whose handler takes 1 parameter, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, TResult>(
            string name,
            string description,
            Func<T1, TResult> handler,
            string parameterName1)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0])),
                new ToolParameter(typeof(T1), parameterName1));
        }

        /// <summary>
        /// Registers a tool whose handler takes 2 parameters, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="T2">The type of the second handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, T2, TResult>(
            string name,
            string description,
            Func<T1, T2, TResult> handler,
            string parameterName1,
            string parameterName2)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0]), Arg<T2>(args[1])),
                new ToolParameter(typeof(T1), parameterName1),
                new ToolParameter(typeof(T2), parameterName2));
        }

        /// <summary>
        /// Registers a tool whose handler takes 3 parameters, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="T2">The type of the second handler parameter</typeparam>
        /// <typeparam name="T3">The type of the third handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
        /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, T2, T3, TResult>(
            string name,
            string description,
            Func<T1, T2, T3, TResult> handler,
            string parameterName1,
            string parameterName2,
            string parameterName3)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0]), Arg<T2>(args[1]), Arg<T3>(args[2])),
                new ToolParameter(typeof(T1), parameterName1),
                new ToolParameter(typeof(T2), parameterName2),
                new ToolParameter(typeof(T3), parameterName3));
        }

        /// <summary>
        /// Registers a tool whose handler takes 4 parameters, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="T2">The type of the second handler parameter</typeparam>
        /// <typeparam name="T3">The type of the third handler parameter</typeparam>
        /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
        /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
        /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, T2, T3, T4, TResult>(
            string name,
            string description,
            Func<T1, T2, T3, T4, TResult> handler,
            string parameterName1,
            string parameterName2,
            string parameterName3,
            string parameterName4)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0]), Arg<T2>(args[1]), Arg<T3>(args[2]), Arg<T4>(args[3])),
                new ToolParameter(typeof(T1), parameterName1),
                new ToolParameter(typeof(T2), parameterName2),
                new ToolParameter(typeof(T3), parameterName3),
                new ToolParameter(typeof(T4), parameterName4));
        }

        /// <summary>
        /// Registers a tool whose handler takes 5 parameters, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="T2">The type of the second handler parameter</typeparam>
        /// <typeparam name="T3">The type of the third handler parameter</typeparam>
        /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
        /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
        /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
        /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
        /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, T2, T3, T4, T5, TResult>(
            string name,
            string description,
            Func<T1, T2, T3, T4, T5, TResult> handler,
            string parameterName1,
            string parameterName2,
            string parameterName3,
            string parameterName4,
            string parameterName5)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0]), Arg<T2>(args[1]), Arg<T3>(args[2]), Arg<T4>(args[3]), Arg<T5>(args[4])),
                new ToolParameter(typeof(T1), parameterName1),
                new ToolParameter(typeof(T2), parameterName2),
                new ToolParameter(typeof(T3), parameterName3),
                new ToolParameter(typeof(T4), parameterName4),
                new ToolParameter(typeof(T5), parameterName5));
        }

        /// <summary>
        /// Registers a tool whose handler takes 6 parameters, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="T2">The type of the second handler parameter</typeparam>
        /// <typeparam name="T3">The type of the third handler parameter</typeparam>
        /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
        /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
        /// <typeparam name="T6">The type of the sixth handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
        /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
        /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
        /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
        /// <param name="parameterName6">The name of the Bedrock request parameter bound to the sixth handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, T2, T3, T4, T5, T6, TResult>(
            string name,
            string description,
            Func<T1, T2, T3, T4, T5, T6, TResult> handler,
            string parameterName1,
            string parameterName2,
            string parameterName3,
            string parameterName4,
            string parameterName5,
            string parameterName6)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0]), Arg<T2>(args[1]), Arg<T3>(args[2]), Arg<T4>(args[3]), Arg<T5>(args[4]), Arg<T6>(args[5])),
                new ToolParameter(typeof(T1), parameterName1),
                new ToolParameter(typeof(T2), parameterName2),
                new ToolParameter(typeof(T3), parameterName3),
                new ToolParameter(typeof(T4), parameterName4),
                new ToolParameter(typeof(T5), parameterName5),
                new ToolParameter(typeof(T6), parameterName6));
        }

        /// <summary>
        /// Registers a tool whose handler takes 7 parameters, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="T2">The type of the second handler parameter</typeparam>
        /// <typeparam name="T3">The type of the third handler parameter</typeparam>
        /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
        /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
        /// <typeparam name="T6">The type of the sixth handler parameter</typeparam>
        /// <typeparam name="T7">The type of the seventh handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
        /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
        /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
        /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
        /// <param name="parameterName6">The name of the Bedrock request parameter bound to the sixth handler parameter</param>
        /// <param name="parameterName7">The name of the Bedrock request parameter bound to the seventh handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, T2, T3, T4, T5, T6, T7, TResult>(
            string name,
            string description,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> handler,
            string parameterName1,
            string parameterName2,
            string parameterName3,
            string parameterName4,
            string parameterName5,
            string parameterName6,
            string parameterName7)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0]), Arg<T2>(args[1]), Arg<T3>(args[2]), Arg<T4>(args[3]), Arg<T5>(args[4]), Arg<T6>(args[5]), Arg<T7>(args[6])),
                new ToolParameter(typeof(T1), parameterName1),
                new ToolParameter(typeof(T2), parameterName2),
                new ToolParameter(typeof(T3), parameterName3),
                new ToolParameter(typeof(T4), parameterName4),
                new ToolParameter(typeof(T5), parameterName5),
                new ToolParameter(typeof(T6), parameterName6),
                new ToolParameter(typeof(T7), parameterName7));
        }

        /// <summary>
        /// Registers a tool whose handler takes 8 parameters, bound by name from the Bedrock request.
        /// This overload doesn't use reflection, so it works under Native AOT.
        /// </summary>
        /// <remarks>
        /// Parameters are converted the same way as for <see cref="Tool(string, string, Delegate)"/>.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/>, and services
        /// resolved from dependency injection, are bound by type and their name isn't used.
        /// </remarks>
        /// <typeparam name="T1">The type of the first handler parameter</typeparam>
        /// <typeparam name="T2">The type of the second handler parameter</typeparam>
        /// <typeparam name="T3">The type of the third handler parameter</typeparam>
        /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
        /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
        /// <typeparam name="T6">The type of the sixth handler parameter</typeparam>
        /// <typeparam name="T7">The type of the seventh handler parameter</typeparam>
        /// <typeparam name="T8">The type of the eighth handler parameter</typeparam>
        /// <typeparam name="TResult">The return type of the handler</typeparam>
        /// <param name="name">The name of the tool function</param>
        /// <param name="description">Description of the tool function</param>
        /// <param name="handler">The handler function</param>
        /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
        /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
        /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
        /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
        /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
        /// <param name="parameterName6">The name of the Bedrock request parameter bound to the sixth handler parameter</param>
        /// <param name="parameterName7">The name of the Bedrock request parameter bound to the seventh handler parameter</param>
        /// <param name="parameterName8">The name of the Bedrock request parameter bound to the eighth handler parameter</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// resolver.Tool("GetWeather", "Gets the weather forecast",
        ///     (string city, int days) => $"Weather for {city} for {days} days",
        ///     "city", "days");
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
            string name,
            string description,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> handler,
            string parameterName1,
            string parameterName2,
            string parameterName3,
            string parameterName4,
            string parameterName5,
            string parameterName6,
            string parameterName7,
            string parameterName8)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return BoundTool(name, args => handler(Arg<T1>(args[0]), Arg<T2>(args[1]), Arg<T3>(args[2]), Arg<T4>(args[3]), Arg<T5>(args[4]), Arg<T6>(args[5]), Arg<T7>(args[6]), Arg<T8>(args[7])),
                new ToolParameter(typeof(T1), parameterName1),
                new ToolParameter(typeof(T2), parameterName2),
                new ToolParameter(typeof(T3), parameterName3),
                new ToolParameter(typeof(T4), parameterName4),
                new ToolParameter(typeof(T5), parameterName5),
                new ToolParameter(typeof(T6), parameterName6),
                new ToolParameter(typeof(T7), parameterName7),
                new ToolParameter(typeof(T8), parameterName8));
        }

        internal BedrockAgentFunctionResolver BoundTool(string name, Func<object?[], object?> invoke,
            params ToolParameter[] parameters)
        {
            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = CreateBoundToolHandler(name, parameters, invoke);
            return this;
        }

        /// <summary>
        /// Creates a handler that maps the arguments for <paramref name="parameters"/> from the request, invokes the
        /// tool and converts its result. It doesn't use reflection on the tool, so it works under Native AOT.
        /// </summary>
        internal Func<BedrockFunctionRequest, ILambdaContext?, BedrockFunctionResponse> CreateBoundToolHandler(
            string functionName, ToolParameter[] parameters, Func<object?[], object?> invoke)
        {
            return (input, context) =>
            {
                try
                {
                    var serviceProvider = (this as DiBedrockAgentFunctionResolver)?.ServiceProvider;
                    var args = _parameterMapper.MapParameters(parameters, input, context, serviceProvider);
                    var result = invoke(args);
                    return _resultConverter.ProcessResult<object>(result, input, functionName, context);
                }
                catch (Exception ex)
                {
                    context?.Logger.LogError(ex.ToString());
                    // Unwrap the exceptions added by MethodInfo.Invoke and by blocking on a faulted task, so the
                    // message is the tool's own, as it is for Delegate tools.
                    var toolException = ex is TargetInvocationException or AggregateException
                        ? ex.InnerException ?? ex
                        : ex;
                    return BedrockFunctionResponse.WithText(
                        $"Error when invoking tool: {toolException.Message}",
                        input.ActionGroup,
                        functionName,
                        input.SessionAttributes,
                        input.PromptSessionAttributes,
                        new Dictionary<string, string>());
                }
            };
        }

        /// <summary>
        /// Converts a mapped argument to the handler parameter type. A missing argument becomes the type's default
        /// value, as it does when a Delegate tool is invoked.
        /// </summary>
        private static T Arg<T>(object? value) => value is null ? default! : (T)value;

        [RequiresUnreferencedCode(AotMessages.DelegateTool)]
        [RequiresDynamicCode(AotMessages.DelegateTool)]
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

        [RequiresUnreferencedCode(AotMessages.DelegateTool)]
        [RequiresDynamicCode(AotMessages.DelegateTool)]
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
