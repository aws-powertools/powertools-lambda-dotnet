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