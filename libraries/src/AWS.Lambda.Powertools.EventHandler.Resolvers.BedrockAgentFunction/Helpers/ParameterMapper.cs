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

using System.Reflection;
using System.Text.Json;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers
{
    /// <summary>
    /// Maps parameters for Bedrock Agent function handlers
    /// </summary>
    public class ParameterMapper
    {
        private readonly ParameterTypeValidator _validator = new();
        
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
            var bedrockParamIndex = 0;

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
                else if (_validator.IsBedrockParameter(paramType))
                {
                    args[i] = MapBedrockParameter(paramType, parameter.Name ?? $"arg{bedrockParamIndex}", accessor);
                    bedrockParamIndex++;
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
                    return JsonSerializer.Deserialize(jsonArrayStr, BedrockFunctionResolverContext.Default.BooleanArray);
                if (paramType == typeof(decimal[]))
                    return JsonSerializer.Deserialize(jsonArrayStr, BedrockFunctionResolverContext.Default.DecimalArray);
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
