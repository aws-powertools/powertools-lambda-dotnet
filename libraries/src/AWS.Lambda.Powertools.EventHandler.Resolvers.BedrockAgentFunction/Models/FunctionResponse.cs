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

using System.Text.Json.Serialization;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

/// <summary>
/// Represents the function response part of a Response.
/// </summary>
public class FunctionResponse
{
    /// <summary>
    /// Contains an object that defines the response from execution of the function. The key is the content type (currently only TEXT is supported) and the value is an object containing the body of the response.
    /// </summary>
    [JsonPropertyName("responseBody")]
    public ResponseBody ResponseBody { get; set; } = new ResponseBody();

    /// <summary>
    /// (Optional) – Set to one of the following states to define the agent's behavior after processing the action:
    ///
    /// FAILURE – The agent throws a DependencyFailedException for the current session. Applies when the function execution fails because of a dependency failure.
    /// REPROMPT – The agent passes a response string to the model to reprompt it. Applies when the function execution fails because of invalid input.
    /// </summary>
    [JsonPropertyName("responseState")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseState? ResponseState { get; set; }
}

/// <summary>
/// Represents the response state of a function response.
/// </summary>
public enum ResponseState
{
    FAILURE,
    REPROMPT
}