using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers
{
    /// <summary>
    /// Maps parameters for Bedrock Agent function handlers
    /// </summary>
    internal class ParameterMapper
    {
        private readonly ParameterTypeValidator _validator = new();
        private readonly IJsonTypeInfoResolver? _typeResolver;

        public ParameterMapper(IJsonTypeInfoResolver? typeResolver = null)
        {
            _typeResolver = typeResolver;
        }

        /// <summary>
        /// Maps parameters for a handler method from a Bedrock function request
        /// </summary>
        /// <param name="methodInfo">The handler method</param>
        /// <param name="input">The Bedrock function request</param>
        /// <param name="context">The Lambda context</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection</param>
        /// <returns>Array of arguments to pass to the handler</returns>
        public object?[] MapParameters(
            MethodInfo methodInfo,
            BedrockFunctionRequest input,
            ILambdaContext? context,
            IServiceProvider? serviceProvider)
        {
            var parameters = methodInfo.GetParameters();
            var args = new object?[parameters.Length];
            var accessor = new ParameterAccessor(input.Parameters);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var paramType = parameter.ParameterType;

                if (paramType == typeof(ILambdaContext))
                {
                    args[i] = context;
                    continue; // Skip further processing for this parameter
                }
                else if (paramType == typeof(BedrockFunctionRequest))
                {
                    args[i] = input;
                    continue; // Skip further processing for this parameter
                }

                // Try to deserialize custom complex type from InputText
                if (!string.IsNullOrEmpty(input.InputText) &&
                    !paramType.IsPrimitive &&
                    paramType != typeof(string) &&
                    !paramType.IsEnum)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        if (_typeResolver != null)
                        {
                            options.TypeInfoResolver = _typeResolver;

                            // Get the JsonTypeInfo for the parameter type
                            var jsonTypeInfo = _typeResolver.GetTypeInfo(paramType, options);
                            if (jsonTypeInfo != null)
                            {
                                // Use the AOT-friendly overload with JsonTypeInfo
                                args[i] = JsonSerializer.Deserialize(input.InputText, jsonTypeInfo);

                                if (args[i] != null)
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            // Fallback to non-AOT deserialization with warning
#pragma warning disable IL2026, IL3050
                            args[i] = JsonSerializer.Deserialize(input.InputText, paramType, options);
#pragma warning restore IL2026, IL3050

                            if (args[i] != null)
                            {
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        // Deserialization failed, continue to regular parameter mapping
                    }
                }

                if (_validator.IsBedrockParameter(paramType))
                {
                    args[i] = MapBedrockParameter(paramType, parameter.Name ?? $"arg{i}", accessor);
                }
                else if (serviceProvider != null)
                {
                    // Resolve from DI
                    args[i] = serviceProvider.GetService(paramType);
                }
            }

            return args;
        }

        private object? MapBedrockParameter(Type paramType, string paramName, ParameterAccessor accessor)
        {
            // Array parameter handling
            if (paramType.IsArray)
            {
                return MapArrayParameter(paramType, paramName, accessor);
            }

            // Scalar parameter handling
            return MapScalarParameter(paramType, paramName, accessor);
        }

        private object? MapArrayParameter(Type paramType, string paramName, ParameterAccessor accessor)
        {
            var jsonArrayStr = accessor.Get<string>(paramName);

            if (string.IsNullOrEmpty(jsonArrayStr))
            {
                return null;
            }

            try
            {
                // AOT-compatible deserialization using source generation
                if (paramType == typeof(string[]))
                    return JsonSerializer.Deserialize(jsonArrayStr, BedrockFunctionResolverContext.Default.StringArray);
                if (paramType == typeof(int[]))
                    return JsonSerializer.Deserialize(jsonArrayStr, BedrockFunctionResolverContext.Default.Int32Array);
                if (paramType == typeof(long[]))
                    return JsonSerializer.Deserialize(jsonArrayStr, BedrockFunctionResolverContext.Default.Int64Array);
                if (paramType == typeof(double[]))
                    return JsonSerializer.Deserialize(jsonArrayStr, BedrockFunctionResolverContext.Default.DoubleArray);
                if (paramType == typeof(bool[]))
                    return JsonSerializer.Deserialize(jsonArrayStr,
                        BedrockFunctionResolverContext.Default.BooleanArray);
                if (paramType == typeof(decimal[]))
                    return JsonSerializer.Deserialize(jsonArrayStr,
                        BedrockFunctionResolverContext.Default.DecimalArray);
            }
            catch (JsonException)
            {
                // Return null on error
            }

            return null;
        }

        private object? MapScalarParameter(Type paramType, string paramName, ParameterAccessor accessor)
        {
            if (paramType == typeof(string))
                return accessor.Get<string>(paramName);
            if (paramType == typeof(int))
                return accessor.Get<int>(paramName);
            if (paramType == typeof(long))
                return accessor.Get<long>(paramName);
            if (paramType == typeof(double))
                return accessor.Get<double>(paramName);
            if (paramType == typeof(bool))
                return accessor.Get<bool>(paramName);
            if (paramType == typeof(decimal))
                return accessor.Get<decimal>(paramName);
            if (paramType == typeof(DateTime))
                return accessor.Get<DateTime>(paramName);
            if (paramType == typeof(Guid))
                return accessor.Get<Guid>(paramName);
            if (paramType.IsEnum)
            {
                // For enums, get as string and parse
                var strValue = accessor.Get<string>(paramName);
                return !string.IsNullOrEmpty(strValue) ? Enum.Parse(paramType, strValue) : null;
            }

            return null;
        }
    }
}