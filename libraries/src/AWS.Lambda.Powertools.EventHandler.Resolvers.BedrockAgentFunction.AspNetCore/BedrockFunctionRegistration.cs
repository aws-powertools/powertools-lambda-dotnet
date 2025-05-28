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

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;

/// <summary>
/// Helper class for function registration with fluent API pattern.
/// </summary>
internal class BedrockFunctionRegistration
{
    private readonly BedrockAgentFunctionResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="BedrockFunctionRegistration"/> class.
    /// </summary>
    /// <param name="resolver">The Bedrock agent function resolver.</param>
    public BedrockFunctionRegistration(BedrockAgentFunctionResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Adds a function to the Bedrock resolver.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="handler">The delegate handler.</param>
    /// <param name="description">Optional description of the function.</param>
    /// <returns>The function registration instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", (string city, int month) =>
    ///     $"Weather forecast for {city} in month {month}: Warm and sunny");
    ///     
    /// app.MapBedrockFunction("Calculate", (int x, int y) => 
    ///     $"Result: {x + y}");
    /// );
    /// </code>
    /// </example>
    public BedrockFunctionRegistration Add(string name, Delegate handler, string description = "")
    {
        _resolver.Tool(name, description, handler);
        return this;
    }
}