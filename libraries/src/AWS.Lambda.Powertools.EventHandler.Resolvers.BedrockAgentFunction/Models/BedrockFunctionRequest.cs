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
/// Represents the input for a Bedrock Agent function.
/// </summary>
public class BedrockFunctionRequest
{
    /// <summary>
    /// The version of the message that identifies the format of the event data going into the Lambda function and the expected format of the response from a Lambda function. Amazon Bedrock only supports version 1.0.
    /// </summary>
    [JsonPropertyName("messageVersion")]
    public string MessageVersion { get; set; } = "1.0";

    /// <summary>
    /// The name of the function as defined in the function details for the action group.
    /// </summary>
    [JsonPropertyName("function")]
    public string Function { get; set; } = string.Empty;

    /// <summary>
    /// Contains a list of objects. Each object contains the name, type, and value of a parameter in the API operation, as defined in the OpenAPI schema, or in the function.
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; } = new List<Parameter>();

    /// <summary>
    /// The unique identifier of the agent session.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Contains information about the name, ID, alias, and version of the agent that the action group belongs to.
    /// </summary>
    [JsonPropertyName("agent")]
    public Agent? Agent { get; set; }

    /// <summary>
    /// The name of the action group.
    /// </summary>
    [JsonPropertyName("actionGroup")]
    public string ActionGroup { get; set; } = string.Empty;

    /// <summary>
    /// Contains session attributes and their values. These attributes are stored over a session and provide context for the agent.
    /// For more information, see <see href="https://docs.aws.amazon.com/bedrock/latest/userguide/agents-session-state.html#session-state-attributes">Session and prompt session attributes</see>.
    /// </summary>
    [JsonPropertyName("sessionAttributes")]
    public Dictionary<string, string> SessionAttributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Contains prompt session attributes and their values. These attributes are stored over a turn and provide context for the agent.
    /// </summary>
    [JsonPropertyName("promptSessionAttributes")]
    public Dictionary<string, string> PromptSessionAttributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The user input for the conversation turn.
    /// </summary>
    [JsonPropertyName("inputText")]
    public string InputText { get; set; } = string.Empty;
}