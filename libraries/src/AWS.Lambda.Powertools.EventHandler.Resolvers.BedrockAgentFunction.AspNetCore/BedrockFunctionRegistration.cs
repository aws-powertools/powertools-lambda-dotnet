namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;

/// <summary>
/// Helper class for function registration with fluent API pattern.
/// </summary>
internal class BedrockFunctionRegistration
{
    private readonly BedrockAgentFunctionResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="BedrockFunctionRegistration"/> class.
    /// </summary>
    /// <param name="resolver">The Bedrock agent function resolver.</param>
    public BedrockFunctionRegistration(BedrockAgentFunctionResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Adds a function to the Bedrock resolver.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="handler">The delegate handler.</param>
    /// <param name="description">Optional description of the function.</param>
    /// <returns>The function registration instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", (string city, int month) =>
    ///     $"Weather forecast for {city} in month {month}: Warm and sunny");
    ///     
    /// app.MapBedrockFunction("Calculate", (int x, int y) => 
    ///     $"Result: {x + y}");
    /// );
    /// </code>
    /// </example>
    public BedrockFunctionRegistration Add(string name, Delegate handler, string description = "")
    {
        _resolver.Tool(name, description, handler);
        return this;
    }
}