using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

/// <summary>
/// Represents the input for a Bedrock Agent function.
/// </summary>
public class BedrockFunctionRequest
{
    /// <summary>
    /// Gets or sets the message version.
    /// </summary>
    [JsonPropertyName("messageVersion")]
    public string MessageVersion { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    [JsonPropertyName("function")]
    public string Function { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters for the function.
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; } = new List<Parameter>();

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent information.
    /// </summary>
    [JsonPropertyName("agent")]
    public Agent? Agent { get; set; }

    /// <summary>
    /// Gets or sets the action group.
    /// </summary>
    [JsonPropertyName("actionGroup")]
    public string ActionGroup { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session attributes.
    /// </summary>
    [JsonPropertyName("sessionAttributes")]
    public Dictionary<string, string> SessionAttributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the prompt session attributes.
    /// </summary>
    [JsonPropertyName("promptSessionAttributes")]
    public Dictionary<string, string> PromptSessionAttributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the input text.
    /// </summary>
    [JsonPropertyName("inputText")]
    public string InputText { get; set; } = string.Empty;
}