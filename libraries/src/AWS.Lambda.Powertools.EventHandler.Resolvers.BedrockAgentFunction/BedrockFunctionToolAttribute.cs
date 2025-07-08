// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler.Resolvers;

/// <summary>
/// Marks a method as a Bedrock Agent function tool.
/// </summary>
/// <example>
/// <code>
/// [BedrockFunctionTool(Name = "GetWeather", Description = "Gets the weather for a location")]
/// public static string GetWeather(string location, int days)
/// {
///     return $"Weather forecast for {location} for the next {days} days";
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class BedrockFunctionToolAttribute : Attribute
{
    /// <summary>
    /// The name of the tool. If not specified, the method name will be used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The description of the tool. Used to provide context about the tool's functionality.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Marks a class as containing Bedrock Agent function tools.
/// </summary>
/// <example>
/// <code>
/// [BedrockFunctionType]
/// public class WeatherTools
/// {
///     // Methods that can be registered as tools
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class BedrockFunctionTypeAttribute : Attribute
{
}