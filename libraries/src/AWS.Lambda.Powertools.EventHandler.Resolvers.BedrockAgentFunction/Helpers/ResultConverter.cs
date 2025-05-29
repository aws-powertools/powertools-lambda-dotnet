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
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers
{
    /// <summary>
    /// Converts handler results to BedrockFunctionResponse
    /// </summary>
    internal class ResultConverter
    {
        /// <summary>
        /// Processes results from handler functions and converts to BedrockFunctionResponse
        /// </summary>
        public BedrockFunctionResponse ProcessResult<TResult>(
            object? result, 
            BedrockFunctionRequest input,
            string functionName,
            ILambdaContext? context)
        {
            // Direct return for BedrockFunctionResponse
            if (result is BedrockFunctionResponse output)
                return EnsureResponseMetadata(output, input, functionName);

            // Handle async results with specific type checks (AOT-compatible)
            if (result is Task<BedrockFunctionResponse> outputTask)
                return EnsureResponseMetadata(outputTask.Result, input, functionName);

            // Handle various Task<T> types
            if (result is Task task)
            {
                return HandleTaskResult<TResult>(task, input);
            }

            // Handle regular (non-task) results
            return ConvertToOutput(result, input);
        }

        private BedrockFunctionResponse HandleTaskResult<TResult>(Task task, BedrockFunctionRequest input)
        {
            // For Task<string>
            if (task is Task<string> stringTask)
                return ConvertToOutput((TResult)(object)stringTask.Result, input);

            // For Task<int>
            if (task is Task<int> intTask)
                return ConvertToOutput((TResult)(object)intTask.Result, input);
                
            // For Task<bool>
            if (task is Task<bool> boolTask)
                return ConvertToOutput((TResult)(object)boolTask.Result, input);
                
            // For Task<double>
            if (task is Task<double> doubleTask)
                return ConvertToOutput((TResult)(object)doubleTask.Result, input);
                
            // For Task<long>
            if (task is Task<long> longTask)
                return ConvertToOutput((TResult)(object)longTask.Result, input);
                
            // For Task<decimal>
            if (task is Task<decimal> decimalTask)
                return ConvertToOutput((TResult)(object)decimalTask.Result, input);
                
            // For Task<DateTime>
            if (task is Task<DateTime> dateTimeTask)
                return ConvertToOutput((TResult)(object)dateTimeTask.Result, input);
                
            // For Task<Guid>
            if (task is Task<Guid> guidTask)
                return ConvertToOutput((TResult)(object)guidTask.Result, input);
                
            // For Task<object>
            if (task is Task<object> objectTask)
                return ConvertToOutput((TResult)objectTask.Result, input);

            // For regular Task with no result
            task.GetAwaiter().GetResult();
            return BedrockFunctionResponse.WithText(
                string.Empty,
                input.ActionGroup,
                input.Function,
                input.SessionAttributes,
                input.PromptSessionAttributes,
                new Dictionary<string, string>());
        }

        /// <summary>
        /// Converts a result to a BedrockFunctionResponse
        /// </summary>
        public BedrockFunctionResponse ConvertToOutput<T>(T result, BedrockFunctionRequest input)
        {
            var function = input.Function;

            if (EqualityComparer<T>.Default.Equals(result, default(T)))
            {
                return CreateEmptyResponse(input);
            }

            // If result is already a BedrockFunctionResponse, ensure metadata is set
            if (result is BedrockFunctionResponse output)
            {
                return EnsureResponseMetadata(output, input, function);
            }

            // Handle primitive types
            return ConvertPrimitiveToOutput(result, input);
        }
        
        private BedrockFunctionResponse ConvertPrimitiveToOutput<T>(T result, BedrockFunctionRequest input)
        {
            var actionGroup = input.ActionGroup;
            var function = input.Function;
            
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
                result?.ToString() ?? string.Empty,
                actionGroup,
                function,
                input.SessionAttributes,
                input.PromptSessionAttributes,
                new Dictionary<string, string>());
        }

        private BedrockFunctionResponse CreateEmptyResponse(BedrockFunctionRequest input)
        {
            return BedrockFunctionResponse.WithText(
                string.Empty,
                input.ActionGroup,
                input.Function,
                input.SessionAttributes,
                input.PromptSessionAttributes,
                new Dictionary<string, string>());
        }

        private BedrockFunctionResponse EnsureResponseMetadata(
            BedrockFunctionResponse response,
            BedrockFunctionRequest input,
            string functionName)
        {
            // If the action group or function are not set in the output, use the provided values
            if (string.IsNullOrEmpty(response.Response.ActionGroup))
            {
                response.Response.ActionGroup = input.ActionGroup;
            }

            if (string.IsNullOrEmpty(response.Response.Function))
            {
                response.Response.Function = functionName;
            }

            return response;
        }
    }
}
