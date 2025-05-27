using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

/// <summary>
/// Represents an agent in the Bedrock Agent function input.
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