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

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

/// <summary>
/// Represents the response part of an BedrockFunctionResponse.
/// </summary>
public class Response
{
    /// <summary>
    /// Gets or sets the action group.
    /// </summary>
    [JsonPropertyName("actionGroup")]
    public string ActionGroup { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function.
    /// </summary>
    [JsonPropertyName("function")]
    public string Function { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function response.
    /// </summary>
    [JsonPropertyName("functionResponse")]
    public FunctionResponse FunctionResponse { get; set; } = new FunctionResponse();
}