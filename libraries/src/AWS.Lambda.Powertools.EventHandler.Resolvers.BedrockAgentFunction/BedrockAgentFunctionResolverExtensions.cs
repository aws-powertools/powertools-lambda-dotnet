using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler.Resolvers
{
    /// <summary>
    /// Extension methods for Bedrock Agent Function Resolver.
    /// </summary>
    public static class BedrockResolverExtensions
    {
        /// <summary>
        /// Registers a Bedrock Agent Function Resolver with dependency injection support.
        /// </summary>
        /// <param name="services">The service collection to add the resolver to.</param>
        /// <param name="typeResolver"></param>
        /// <returns>The updated service collection.</returns>
        /// <example>
        /// <code>
        /// public void ConfigureServices(IServiceCollection services)
        /// {
        ///     services.AddBedrockResolver();
        ///     
        ///     // Now you can inject BedrockAgentFunctionResolver into your services
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddBedrockResolver(
            this IServiceCollection services,
            IJsonTypeInfoResolver? typeResolver = null)
        {
            services.AddSingleton<BedrockAgentFunctionResolver>(sp =>
                new DiBedrockAgentFunctionResolver(sp, typeResolver));
            return services;
        }

        /// <summary>
        /// Registers tools from a type marked with BedrockFunctionTypeAttribute.
        /// </summary>
        /// <typeparam name="T">The type containing tool methods marked with BedrockFunctionToolAttribute</typeparam>
        /// <param name="resolver">The resolver to register tools with</param>
        /// <returns>The resolver for method chaining</returns>
        /// <example>
        /// <code>
        /// // Define your tool class
        /// [BedrockFunctionType]
        /// public class WeatherTools
        /// {
        ///     [BedrockFunctionTool(Name = "GetWeather", Description = "Gets weather forecast")]
        ///     public static string GetWeather(string location, int days)
        ///     {
        ///         return $"Weather forecast for {location} for the next {days} days";
        ///     }
        /// }
        /// 
        /// // Register the tools
        /// var resolver = new BedrockAgentFunctionResolver();
        /// resolver.RegisterTool&lt;WeatherTools&gt;();
        /// </code>
        /// </example>
        /// <remarks>
        /// <typeparamref name="T"/> is annotated so that its public methods, and their parameter names, are kept in
        /// trimmed and Native AOT apps. The tool methods are invoked with <see cref="MethodBase.Invoke(object, object[])"/>,
        /// so this works under Native AOT.
        /// </remarks>
        public static BedrockAgentFunctionResolver RegisterTool<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
            this BedrockAgentFunctionResolver resolver)
            where T : class
        {
            var type = typeof(T);

            // Check if class has the BedrockFunctionType attribute
            if (!type.IsDefined(typeof(BedrockFunctionTypeAttribute), false))
                return resolver;

            // Look at all static methods with the tool attribute
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                var attr = method.GetCustomAttribute<BedrockFunctionToolAttribute>();
                if (attr == null) continue;

                var toolName = attr.Name ?? method.Name;

                // Invoke the method directly rather than creating a typed delegate for it: delegate types over value
                // types, such as Func<string, int, string>, have no native code under Native AOT.
                resolver.BoundTool(toolName, args => method.Invoke(null, args),
                    ParameterMapper.GetToolParameters(method));
            }

            return resolver;
        }
    }
}