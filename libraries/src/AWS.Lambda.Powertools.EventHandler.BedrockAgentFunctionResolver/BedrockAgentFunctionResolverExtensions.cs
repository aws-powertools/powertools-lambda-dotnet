using System;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.EventHandler
{
    // Service provider-aware resolver
    public class DIBedrockAgentFunctionResolver : BedrockAgentFunctionResolver
    {
        public IServiceProvider ServiceProvider { get; }

        public DIBedrockAgentFunctionResolver(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }

    public static class BedrockResolverExtensions
    {
        // Extension to register the resolver in DI
        public static IServiceCollection AddBedrockResolver(this IServiceCollection services)
        {
            services.AddSingleton<BedrockAgentFunctionResolver>(sp =>
                new DIBedrockAgentFunctionResolver(sp));
            return services;
        }
    }
}