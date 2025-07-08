using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

/// <summary>
/// Represents the response body part of a FunctionResponse.
/// </summary>
public class ResponseBody
{
    /// <summary>
    /// Gets or sets the text body.
    /// </summary>
    [JsonPropertyName("TEXT")]
    public TextBody Text { get; set; } = new TextBody();
}