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

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers
{
    /// <summary>
    /// Validates parameter types for Bedrock Agent functions
    /// </summary>
    internal class ParameterTypeValidator
    {
        private static readonly HashSet<Type> BedrockParameterTypes = new()
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

        /// <summary>
        /// Checks if a type is a valid Bedrock parameter type
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is valid for Bedrock parameters</returns>
        public bool IsBedrockParameter(Type type) =>
            BedrockParameterTypes.Contains(type) || type.IsEnum ||
            (type.IsArray && BedrockParameterTypes.Contains(type.GetElementType()!));
    }
}
