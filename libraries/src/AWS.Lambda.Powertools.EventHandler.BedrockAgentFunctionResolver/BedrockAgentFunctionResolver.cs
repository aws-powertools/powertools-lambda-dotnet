using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.EventHandler
{
    public class BedrockAgentFunctionResolver
    {
        private readonly
            Dictionary<string, Func<ActionGroupInvocationInput, ILambdaContext?, ActionGroupInvocationOutput>>
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
            typeof(Guid)
        };

        private static bool IsBedrockParameter(Type type) =>
            _bedrockParameterTypes.Contains(type) || type.IsEnum;

        /// <summary>
        /// Registers a handler that directly accepts ActionGroupInvocationInput and returns ActionGroupInvocationOutput
        /// </summary>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<ActionGroupInvocationInput, ILambdaContext?, ActionGroupInvocationOutput> handler,
            string description = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[name] = handler;
            return this;
        }

        /// <summary>
        /// Registers a handler that directly accepts ActionGroupInvocationInput and returns ActionGroupInvocationOutput
        /// </summary>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<ActionGroupInvocationInput, ActionGroupInvocationOutput> handler,
            string description = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[name] = (input, _) => handler(input);
            return this;
        }

        /// <summary>
        /// Registers a handler for a tool function with automatically converted return type.
        /// </summary>
        public BedrockAgentFunctionResolver Tool(
            string name,
            string description = "",
            Delegate? handler = null)
        {
            // Delegate to the generic version with object as return type
            return Tool<object>(name, description, handler);
        }

        /// <summary>
        /// Registers a handler for a tool function with typed return value.
        /// </summary>
        public BedrockAgentFunctionResolver Tool<TResult>(
            string name,
            string description = "",
            Delegate? handler = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[name] = (input, context) =>
            {
                var accessor = new ParameterAccessor(input.Parameters);
                var parameters = handler.Method.GetParameters();
                var args = new object?[parameters.Length];
                var bedrockParamIndex = 0;

                // Get service provider from resolver if available
                var serviceProvider = (this as DIBedrockAgentFunctionResolver)?.ServiceProvider;

                // Map parameters from Bedrock input and DI
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var paramType = parameter.ParameterType;

                    if (paramType == typeof(ILambdaContext))
                    {
                        args[i] = context;
                    }
                    else if (paramType == typeof(ActionGroupInvocationInput))
                    {
                        args[i] = input;
                    }
                    else if (IsBedrockParameter(paramType))
                    {
                        var paramName = parameter.Name ?? $"arg{bedrockParamIndex}";

                        // AOT-compatible parameter access - direct type checks
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

                    // Direct return for ActionGroupInvocationOutput
                    if (result is ActionGroupInvocationOutput output)
                        return output;

                    // Handle async results with specific type checks (AOT-compatible)
                    if (result is Task<ActionGroupInvocationOutput> outputTask)
                        return outputTask.Result;
                    if (result is Task<string> stringTask)
                        return ConvertToOutput((TResult)(object)stringTask.Result);
                    if (result is Task<int> intTask)
                        return ConvertToOutput((TResult)(object)intTask.Result);
                    if (result is Task<bool> boolTask)
                        return ConvertToOutput((TResult)(object)boolTask.Result);
                    if (result is Task<double> doubleTask)
                        return ConvertToOutput((TResult)(object)doubleTask.Result);
                    if (result is Task<long> longTask)
                        return ConvertToOutput((TResult)(object)longTask.Result);
                    if (result is Task<decimal> decimalTask)
                        return ConvertToOutput((TResult)(object)decimalTask.Result);
                    if (result is Task<DateTime> dateTimeTask)
                        return ConvertToOutput((TResult)(object)dateTimeTask.Result);
                    if (result is Task<Guid> guidTask)
                        return ConvertToOutput((TResult)(object)guidTask.Result);
                    if (result is Task<object> objectTask)
                        return ConvertToOutput((TResult)objectTask.Result!);

                    // For regular Task with no result
                    if (result is Task task)
                    {
                        task.GetAwaiter().GetResult();
                        return new ActionGroupInvocationOutput { Text = string.Empty };
                    }

                    return ConvertToOutput((TResult)result!);
                }
                catch (Exception ex)
                {
                    context?.Logger.LogError($"Error executing function {name}: {ex.Message}");
                    return new ActionGroupInvocationOutput
                    {
                        Text = $"Error executing function: {ex.Message}"
                    };
                }
            };

            return this;
        }

        /// <summary>
        /// Registers a parameter-less handler that returns ActionGroupInvocationOutput
        /// </summary>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<ActionGroupInvocationOutput> handler,
            string description = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[name] = (input, context) => handler();
            return this;
        }

        /// <summary>
        /// Registers a parameter-less handler with automatic string conversion
        /// </summary>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<string> handler,
            string description = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[name] = (input, context) => new ActionGroupInvocationOutput { Text = handler() };
            return this;
        }

        /// <summary>
        /// Registers a parameter-less handler with automatic object conversion
        /// </summary>
        public BedrockAgentFunctionResolver Tool(
            string name,
            Func<object> handler,
            string description = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[name] = (input, context) =>
            {
                var result = handler();
                return ConvertToOutput(result);
            };
            return this;
        }

        /// <summary>
        /// Resolves and processes a Bedrock Agent function invocation.
        /// </summary>
        public ActionGroupInvocationOutput Resolve(ActionGroupInvocationInput input, ILambdaContext? context = null)
        {
            return ResolveAsync(input, context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously resolves and processes a Bedrock Agent function invocation.
        /// </summary>
        public async Task<ActionGroupInvocationOutput> ResolveAsync(ActionGroupInvocationInput input,
            ILambdaContext? context = null)
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

        private ActionGroupInvocationOutput ConvertToOutput<T>(T result)
        {
            if (result == null)
            {
                return new ActionGroupInvocationOutput { Text = string.Empty };
            }

            // If result is already an ActionGroupInvocationOutput, return it directly
            if (result is ActionGroupInvocationOutput output)
            {
                return output;
            }

            // For primitive types and strings, convert to string
            if (result is string str)
            {
                return new ActionGroupInvocationOutput { Text = str };
            }

            if (result is int intVal)
            {
                return new ActionGroupInvocationOutput { Text = intVal.ToString(CultureInfo.InvariantCulture) };
            }

            if (result is double doubleVal)
            {
                return new ActionGroupInvocationOutput { Text = doubleVal.ToString(CultureInfo.InvariantCulture) };
            }

            if (result is bool boolVal)
            {
                return new ActionGroupInvocationOutput { Text = boolVal.ToString() };
            }

            if (result is long longVal)
            {
                return new ActionGroupInvocationOutput { Text = longVal.ToString(CultureInfo.InvariantCulture) };
            }

            if (result is decimal decimalVal)
            {
                return new ActionGroupInvocationOutput { Text = decimalVal.ToString(CultureInfo.InvariantCulture) };
            }

            // For any other type, use ToString() instead of JSON serialization
            return new ActionGroupInvocationOutput { Text = result.ToString() ?? string.Empty };
        }
    }
}