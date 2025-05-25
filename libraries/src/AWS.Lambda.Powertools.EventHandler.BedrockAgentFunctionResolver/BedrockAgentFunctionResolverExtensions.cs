using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler
{
    /// <summary>
    /// Extended Bedrock Agent Function Resolver with dependency injection support.
    /// </summary>
    public class DiBedrockAgentFunctionResolver : BedrockAgentFunctionResolver
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

    /// <summary>
    /// Extension methods for Bedrock Agent Function Resolver.
    /// </summary>
    public static class BedrockResolverExtensions
    {
        /// <summary>
        /// Registers a Bedrock Agent Function Resolver with dependency injection support.
        /// </summary>
        /// <param name="services">The service collection to add the resolver to.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddBedrockResolver(this IServiceCollection services)
        {
            services.AddSingleton<BedrockAgentFunctionResolver>(sp =>
                new DiBedrockAgentFunctionResolver(sp));
            return services;
        }
    }
}