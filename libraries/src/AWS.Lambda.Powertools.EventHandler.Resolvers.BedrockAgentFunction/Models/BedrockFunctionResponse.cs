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
/// The version of the message that identifies the format of the event data going into the Lambda function and the expected format of the response from a Lambda function. Amazon Bedrock only supports version 1.0.
/// </summary>
public class BedrockFunctionResponse
{
    /// <summary>
    /// Gets or sets the message version.
    /// </summary>
    [JsonPropertyName("messageVersion")]
    public string MessageVersion { get; } = "1.0";

    /// <summary>
    /// Gets or sets the response.
    /// </summary>
    [JsonPropertyName("response")]
    public Response Response { get; set; } = new Response();

    /// <summary>
    /// Contains session attributes and their values. For more information, <see href="https://docs.aws.amazon.com/bedrock/latest/userguide/agents-session-state.html#session-state-attributes"> Session and prompt session attributes.</see>
    /// </summary>
    [JsonPropertyName("sessionAttributes")]
    public Dictionary<string, string> SessionAttributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Contains prompt attributes and their values. For more information, <see href="https://docs.aws.amazon.com/bedrock/latest/userguide/agents-session-state.html#session-state-attributes"> Session and prompt session attributes.</see>
    /// </summary>
    [JsonPropertyName("promptSessionAttributes")]
    public Dictionary<string, string> PromptSessionAttributes { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Contains a list of query configurations for knowledge bases attached to the agent. For more information, <see href="https://docs.aws.amazon.com/bedrock/latest/userguide/agents-session-state.html#session-state-kb"> Knowledge base retrieval configurations.</see>
    /// </summary>
    [JsonPropertyName("knowledgeBasesConfiguration")]
    public Dictionary<string, string> KnowledgeBasesConfiguration { get; set; } = new Dictionary<string, string>();


    /// <summary>
    /// Creates a new instance of BedrockFunctionResponse with the specified text.
    /// </summary>
    public static BedrockFunctionResponse WithText(
        string? text, 
        string actionGroup = "", 
        string function = "",
        Dictionary<string, string>? sessionAttributes = null,
        Dictionary<string, string>? promptSessionAttributes = null,
        Dictionary<string, string>? knowledgeBasesConfiguration = null)
    {
        return new BedrockFunctionResponse
        {
            Response = new Response
            {
                ActionGroup = actionGroup,
                Function = function,
                FunctionResponse = new FunctionResponse
                {
                    ResponseBody = new ResponseBody
                    {
                        Text = new TextBody { Body = text ?? string.Empty }
                    }
                }
            },
            SessionAttributes = sessionAttributes ?? new Dictionary<string, string>(),
            PromptSessionAttributes = promptSessionAttributes ?? new Dictionary<string, string>(),
            KnowledgeBasesConfiguration = knowledgeBasesConfiguration ?? new Dictionary<string, string>()
        };
    }
}
