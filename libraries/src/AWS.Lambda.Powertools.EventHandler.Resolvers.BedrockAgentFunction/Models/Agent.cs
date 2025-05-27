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
/// Contains information about the name, ID, alias, and version of the agent that the action group belongs to.
/// </summary>
public class Agent
{
    /// <summary>
    /// Gets or sets the name of the agent.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the agent.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the agent.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alias of the agent.
    /// </summary>
    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;
}