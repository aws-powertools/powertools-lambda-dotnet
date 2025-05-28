namespace AWS.Lambda.Powertools.EventHandler.Resolvers;

/// <summary>
/// Extended Bedrock Agent Function Resolver with dependency injection support.
/// </summary>
internal class DiBedrockAgentFunctionResolver : BedrockAgentFunctionResolver
{
    /// <summary>
    /// Gets the service provider used for dependency injection.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiBedrockAgentFunctionResolver"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public DiBedrockAgentFunctionResolver(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}