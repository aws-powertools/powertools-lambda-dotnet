/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Globalization;
using System.Text.Json;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

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
        private const int MaxTools = 5;

        private readonly
            Dictionary<string, Func<BedrockFunctionRequest, ILambdaContext?, BedrockFunctionResponse>>
            _handlers = new();

        private static readonly HashSet<Type> _bedrockParameterTypes = new()
        {
            typeof(string),
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(bool),
            typeof(decimal),
            typeof(DateTime),
            typeof(Guid),
            typeof(string[]),
            typeof(int[]),
            typeof(long[]),
            typeof(double[]),
            typeof(bool[]),
            typeof(decimal[])
        };

        private static bool IsBedrockParameter(Type type) =>
            _bedrockParameterTypes.Contains(type) || type.IsEnum ||
            (type.IsArray && _bedrockParameterTypes.Contains(type.GetElementType()!));

        /// <summary>
        /// Checks if another tool can be registered, and logs a warning if the maximum limit is reached
        /// or if a tool with the same name is already registered
        /// </summary>
        /// <param name="name">The name of the tool being registered</param>
        /// <returns>True if the tool can be registered, false if the maximum limit is reached</returns>
        private bool CanRegisterTool(string name)
        {
            if (_handlers.Count >= MaxTools && !_handlers.ContainsKey(name))
            {
                Console.WriteLine($"WARNING: Maximum number of tools ({MaxTools}) reached. Tool '{name}' will not be registered.");
                return false;
            }
            
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
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool(
        ///     "GetWeatherDetails",
        ///     (BedrockFunctionRequest input, ILambdaContext context) => {
        ///         context.Logger.LogLine($"Processing request for {input.Function}");
        ///         return new BedrockFunctionResponse { Text = "Weather details response" };
        ///     },
        ///     "Gets detailed weather information"
        /// );
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<BedrockFunctionRequest, ILambdaContext?, BedrockFunctionResponse> handler,
            string description = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

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
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool(
        ///     "GetWeatherDetails",
        ///     (BedrockFunctionRequest input) => {
        ///         var city = input.Parameters.FirstOrDefault(p => p.Name == "city")?.Value;
        ///         return new BedrockFunctionResponse { Text = $"Weather in {city} is sunny" };
        ///     },
        ///     "Gets weather for a city"
        /// );
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<BedrockFunctionRequest, BedrockFunctionResponse> handler,
            string description = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

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
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool(
        ///     "GetCurrentTime",
        ///     () => new BedrockFunctionResponse { Text = DateTime.Now.ToString() },
        ///     "Gets the current server time"
        /// );
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<BedrockFunctionResponse> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (input, context) => handler();
            return this;
        }

        /// <summary>
        /// Registers a parameter-less handler with automatic string conversion
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The handler function that returns a string</param>
        /// <param name="description">Optional description of the tool function</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool(
        ///     "GetGreeting",
        ///     () => "Hello, world!",
        ///     "Returns a greeting message"
        /// );
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<string> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (input, context) => BedrockFunctionResponse.WithText(
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
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool(
        ///     "GetServerStatus",
        ///     () => new { Status = "Online", Uptime = "99.9%" },
        ///     "Returns the server status information"
        /// );
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<object> handler,
            string description = "")
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (input, context) =>
            {
                var result = handler();
                return ConvertToOutput(result, input);
            };
            return this;
        }

        /// <summary>
        /// Registers a handler for a tool function with automatically converted return type (no description).
        /// </summary>
        /// <param name="name">The name of the tool function</param>
        /// <param name="handler">The delegate handler function</param>
        /// <returns>The resolver instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool(
        ///     "CalculateSum", 
        ///     (int a, int b) => a + b
        /// );
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool(
        ///     "GetWeather",
        ///     "Gets the weather forecast for a specific city", 
        ///     (string city, int days) => $"{days}-day forecast for {city}: Sunny"
        /// );
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.Tool&lt;int&gt;(
        ///     "CalculateArea", 
        ///     (int width, int height) => width * height
        /// );
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var resolver = new BedrockAgentFunctionResolver();
        /// 
        /// // Register a function with strongly typed parameters and return value
        /// resolver.Tool&lt;double&gt;(
        ///     "CalculateDistance",
        ///     "Calculates the distance between two points", 
        ///     (double x1, double y1, double x2, double y2) => {
        ///         return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        ///     }
        /// );
        /// 
        /// // Register a function that accepts Lambda context
        /// resolver.Tool&lt;string&gt;(
        ///     "LogAndReturn",
        ///     "Logs a message and returns it", 
        ///     (string message, ILambdaContext context) => {
        ///         context.Logger.LogLine($"Message received: {message}");
        ///         return message;
        ///     }
        /// );
        /// </code>
        /// </example>
        public BedrockAgentFunctionResolver Tool<TResult>(
            string name,
            string description,
            Delegate handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!CanRegisterTool(name))
                return this;

            _handlers[name] = (input, context) =>
            {
                var accessor = new ParameterAccessor(input.Parameters);
                var parameters = handler.Method.GetParameters();
                var args = new object?[parameters.Length];
                var bedrockParamIndex = 0;

                // Get service provider from resolver if available
                var serviceProvider = (this as DiBedrockAgentFunctionResolver)?.ServiceProvider;

                // Map parameters from Bedrock input and DI
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var paramType = parameter.ParameterType;

                    if (paramType == typeof(ILambdaContext))
                    {
                        args[i] = context;
                    }
                    else if (paramType == typeof(BedrockFunctionRequest))
                    {
                        args[i] = input;
                    }
                    else if (IsBedrockParameter(paramType))
                    {
                        var paramName = parameter.Name ?? $"arg{bedrockParamIndex}";

                        // AOT-compatible parameter access - direct type checks
                        // Array parameter handling
                        if (paramType.IsArray)
                        {
                            var jsonArrayStr = accessor.Get<string>(paramName);

                            if (!string.IsNullOrEmpty(jsonArrayStr))
                            {
                                try
                                {
                                    // AOT-compatible deserialization using source generation
                                    if (paramType == typeof(string[]))
                                        args[i] = JsonSerializer.Deserialize(jsonArrayStr,
                                            BedrockFunctionResolverContext.Default.StringArray);
                                    else if (paramType == typeof(int[]))
                                        args[i] = JsonSerializer.Deserialize(jsonArrayStr,
                                            BedrockFunctionResolverContext.Default.Int32Array);
                                    else if (paramType == typeof(long[]))
                                        args[i] = JsonSerializer.Deserialize(jsonArrayStr,
                                            BedrockFunctionResolverContext.Default.Int64Array);
                                    else if (paramType == typeof(double[]))
                                        args[i] = JsonSerializer.Deserialize(jsonArrayStr,
                                            BedrockFunctionResolverContext.Default.DoubleArray);
                                    else if (paramType == typeof(bool[]))
                                        args[i] = JsonSerializer.Deserialize(jsonArrayStr,
                                            BedrockFunctionResolverContext.Default.BooleanArray);
                                    else if (paramType == typeof(decimal[]))
                                        args[i] = JsonSerializer.Deserialize(jsonArrayStr,
                                            BedrockFunctionResolverContext.Default.DecimalArray);
                                    else
                                        args[i] = null; // Unsupported array type
                                }
                                catch (JsonException)
                                {
                                    args[i] = null;
                                }
                            }
                            else
                            {
                                args[i] = null;
                            }
                        }

                        if (paramType == typeof(string))
                            args[i] = accessor.Get<string>(paramName);
                        else if (paramType == typeof(int))
                            args[i] = accessor.Get<int>(paramName);
                        else if (paramType == typeof(long))
                            args[i] = accessor.Get<long>(paramName);
                        else if (paramType == typeof(double))
                            args[i] = accessor.Get<double>(paramName);
                        else if (paramType == typeof(bool))
                            args[i] = accessor.Get<bool>(paramName);
                        else if (paramType == typeof(decimal))
                            args[i] = accessor.Get<decimal>(paramName);
                        else if (paramType == typeof(DateTime))
                            args[i] = accessor.Get<DateTime>(paramName);
                        else if (paramType == typeof(Guid))
                            args[i] = accessor.Get<Guid>(paramName);
                        else if (paramType.IsEnum)
                        {
                            // For enums, get as string and parse
                            var strValue = accessor.Get<string>(paramName);
                            args[i] = !string.IsNullOrEmpty(strValue) ? Enum.Parse(paramType, strValue) : null;
                        }

                        bedrockParamIndex++;
                    }
                    else if (serviceProvider != null)
                    {
                        // Resolve from DI
                        args[i] = serviceProvider.GetService(paramType);
                    }
                }

                try
                {
                    // Execute the handler
                    var result = handler.DynamicInvoke(args);

                    // Direct return for BedrockFunctionResponse
                    if (result is BedrockFunctionResponse output)
                        return output;

                    // Handle async results with specific type checks (AOT-compatible)
                    if (result is Task<BedrockFunctionResponse> outputTask)
                        return outputTask.Result;
                    if (result is Task<string> stringTask)
                        return ConvertToOutput((TResult)(object)stringTask.Result, input);
                    if (result is Task<int> intTask)
                        return ConvertToOutput((TResult)(object)intTask.Result, input);
                    if (result is Task<bool> boolTask)
                        return ConvertToOutput((TResult)(object)boolTask.Result, input);
                    if (result is Task<double> doubleTask)
                        return ConvertToOutput((TResult)(object)doubleTask.Result, input);
                    if (result is Task<long> longTask)
                        return ConvertToOutput((TResult)(object)longTask.Result, input);
                    if (result is Task<decimal> decimalTask)
                        return ConvertToOutput((TResult)(object)decimalTask.Result, input);
                    if (result is Task<DateTime> dateTimeTask)
                        return ConvertToOutput((TResult)(object)dateTimeTask.Result, input);
                    if (result is Task<Guid> guidTask)
                        return ConvertToOutput((TResult)(object)guidTask.Result, input);
                    if (result is Task<object> objectTask)
                        return ConvertToOutput((TResult)objectTask.Result!, input);

                    // For regular Task with no result
                    if (result is Task task)
                    {
                        task.GetAwaiter().GetResult();
                        return BedrockFunctionResponse.WithText(
                            string.Empty, 
                            input.ActionGroup, 
                            name,
                            input.SessionAttributes,
                            input.PromptSessionAttributes,
                            new Dictionary<string, string>());
                    }

                    return ConvertToOutput(result, input);
                }
                catch (Exception ex)
                {
                    context?.Logger.LogError(ex.ToString());
                    var innerException = ex.InnerException ?? ex;
                    return BedrockFunctionResponse.WithText(
                        $"Error when invoking tool: {innerException.Message}",
                        input.ActionGroup, 
                        name,
                        input.SessionAttributes,
                        input.PromptSessionAttributes,
                        new Dictionary<string, string>());
                }
            };

            return this;
        }

        /// <summary>
        /// Resolves and processes a Bedrock Agent function invocation.
        /// </summary>
        /// <param name="input">The Bedrock Agent input containing the function name and parameters</param>
        /// <param name="context">Optional Lambda context</param>
        /// <returns>The output from the function execution</returns>
        /// <example>
        /// <code>
        /// // Lambda handler
        /// public BedrockFunctionResponse FunctionHandler(BedrockFunctionRequest input, ILambdaContext context)
        /// {
        ///     var resolver = new BedrockAgentFunctionResolver()
        ///         .Tool("GetWeather", (string city) => $"Weather in {city} is sunny")
        ///         .Tool("GetTime", () => DateTime.Now.ToString());
        ///     
        ///     return resolver.Resolve(input, context);
        /// }
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// // Async Lambda handler
        /// public async Task&lt;BedrockFunctionResponse&gt; FunctionHandler(BedrockFunctionRequest input, ILambdaContext context)
        /// {
        ///     var resolver = new BedrockAgentFunctionResolver()
        ///         .Tool("GetWeatherAsync", async (string city) => {
        ///             // Simulate API call
        ///             await Task.Delay(100);
        ///             return $"Weather in {city} is sunny";
        ///         })
        ///         .Tool("GetTime", () => DateTime.Now.ToString());
        ///     
        ///     return await resolver.ResolveAsync(input, context);
        /// }
        /// </code>
        /// </example>
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

        private BedrockFunctionResponse ConvertToOutput<T>(T result, BedrockFunctionRequest input)
        {
            string actionGroup = input.ActionGroup;
            string function = input.Function;

            if (result == null)
            {
                return BedrockFunctionResponse.WithText(
                    string.Empty, 
                    actionGroup, 
                    function,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            // If result is already an BedrockFunctionResponse, ensure action group and function are set
            if (result is BedrockFunctionResponse output)
            {
                // If the action group or function are not set in the output, use the provided values
                if (string.IsNullOrEmpty(output.Response.ActionGroup))
                {
                    output.Response.ActionGroup = actionGroup;
                }

                if (string.IsNullOrEmpty(output.Response.Function))
                {
                    output.Response.Function = function;
                }

                return output;
            }

            // For primitive types and strings, convert to string
            if (result is string str)
            {
                return BedrockFunctionResponse.WithText(
                    str, 
                    actionGroup, 
                    function,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            if (result is int intVal)
            {
                return BedrockFunctionResponse.WithText(
                    intVal.ToString(CultureInfo.InvariantCulture), 
                    actionGroup, 
                    function,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            if (result is double doubleVal)
            {
                return BedrockFunctionResponse.WithText(
                    doubleVal.ToString(CultureInfo.InvariantCulture), 
                    actionGroup,
                    function,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            if (result is bool boolVal)
            {
                return BedrockFunctionResponse.WithText(
                    boolVal.ToString(), 
                    actionGroup, 
                    function,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            if (result is long longVal)
            {
                return BedrockFunctionResponse.WithText(
                    longVal.ToString(CultureInfo.InvariantCulture), 
                    actionGroup,
                    function,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            if (result is decimal decimalVal)
            {
                return BedrockFunctionResponse.WithText(
                    decimalVal.ToString(CultureInfo.InvariantCulture), 
                    actionGroup,
                    function,
                    input.SessionAttributes,
                    input.PromptSessionAttributes,
                    new Dictionary<string, string>());
            }

            // For any other type, use ToString()
            return BedrockFunctionResponse.WithText(
                result.ToString() ?? string.Empty, 
                actionGroup, 
                function,
                input.SessionAttributes,
                input.PromptSessionAttributes,
                new Dictionary<string, string>());
        }
    }
}
