using AWS.Lambda.Powertools.Common.Utils;
using System.Diagnostics.CodeAnalysis;
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
            return MapParameters(GetToolParameters(methodInfo), input, context, serviceProvider);
        }

        /// <summary>
        /// Describes the parameters of a handler method, using the parameter names declared on the method.
        /// </summary>
        internal static ToolParameter[] GetToolParameters(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var result = new ToolParameter[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                result[i] = new ToolParameter(parameters[i].ParameterType, parameters[i].Name ?? $"arg{i}");
            }

            return result;
        }

        /// <summary>
        /// Maps the arguments for a tool handler from a Bedrock function request.
        /// Parameters of type <see cref="ILambdaContext"/> and <see cref="BedrockFunctionRequest"/> are bound by
        /// type, Bedrock parameter types are bound by name from the request, and other types are deserialized from
        /// the request's input text or resolved from dependency injection.
        /// </summary>
        /// <param name="parameters">The handler parameters, in order</param>
        /// <param name="input">The Bedrock function request</param>
        /// <param name="context">The Lambda context</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection</param>
        /// <returns>Array of arguments to pass to the handler</returns>
        internal object?[] MapParameters(
            ToolParameter[] parameters,
            BedrockFunctionRequest input,
            ILambdaContext? context,
            IServiceProvider? serviceProvider)
        {
            var args = new object?[parameters.Length];
            var accessor = new ParameterAccessor(input.Parameters);

            for (var i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].Type;

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
                        args[i] = DeserializeInputText(input.InputText, paramType);
                        if (args[i] != null)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        // Deserialization failed, continue to regular parameter mapping
                    }
                }

                if (_validator.IsBedrockParameter(paramType))
                {
                    args[i] = MapBedrockParameter(paramType, parameters[i].Name, accessor);
                }
                else if (serviceProvider != null)
                {
                    // Resolve from DI
                    args[i] = serviceProvider.GetService(paramType);
                }
            }

            return args;
        }

        private object? DeserializeInputText(string inputText, Type paramType)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            if (_typeResolver != null)
            {
                options.TypeInfoResolver = _typeResolver;

                // Get the JsonTypeInfo for the parameter type and use the AOT-friendly overload
                var jsonTypeInfo = _typeResolver.GetTypeInfo(paramType, options);
                return jsonTypeInfo != null ? JsonSerializer.Deserialize(inputText, jsonTypeInfo) : null;
            }

            return DeserializeWithReflectionIfAvailable(inputText, paramType, options);
        }

        /// <summary>
        /// Deserializes with reflection-based System.Text.Json when it's available. In trimmed and Native AOT apps
        /// it isn't, so the parameter falls through to the regular mapping, as it did when deserialization failed.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
            Justification = "Only called when reflection-based serialization is enabled, which it isn't in trimmed " +
                            "and Native AOT apps (JsonSerializer.IsReflectionEnabledByDefault feature switch).")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
            Justification = "Only called when dynamic code is supported and reflection-based serialization is " +
                            "enabled, which they aren't in Native AOT apps.")]
        private static object? DeserializeWithReflectionIfAvailable(string inputText, Type paramType,
            JsonSerializerOptions options)
        {
            // JsonSerializer.IsReflectionEnabledByDefault is a feature switch that is false in trimmed and Native
            // AOT apps, so the trimmer and ILC remove the guarded call. The analyzer doesn't treat it as a guard,
            // hence the suppressions above. RuntimeFeatureWrapper lets tests simulate Native AOT.
            if (JsonSerializer.IsReflectionEnabledByDefault && RuntimeFeatureWrapper.IsDynamicCodeSupported)
            {
                return DeserializeWithReflection(inputText, paramType, options);
            }

            return null;
        }

        [RequiresUnreferencedCode("Deserializes a parameter type that isn't known at compile time.")]
        [RequiresDynamicCode("Deserializes a parameter type that isn't known at compile time.")]
        private static object? DeserializeWithReflection(string inputText, Type paramType,
            JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(inputText, paramType, options);
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

    /// <summary>
    /// A tool handler parameter: its type, and the name of the Bedrock request parameter it binds to.
    /// </summary>
    internal readonly record struct ToolParameter(Type Type, string Name);
}
