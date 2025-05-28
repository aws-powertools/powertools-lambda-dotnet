/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
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
        public static IServiceCollection AddBedrockResolver(this IServiceCollection services)
        {
            services.AddSingleton<BedrockAgentFunctionResolver>(sp =>
                new DiBedrockAgentFunctionResolver(sp));
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
        public static BedrockAgentFunctionResolver RegisterTool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
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

                string toolName = attr.Name ?? method.Name;
                string description = attr.Description ??  
                                     string.Empty;

                // Create delegate from the static method
                var del = Delegate.CreateDelegate(
                    GetDelegateType(method), 
                    method);
            
                // Call the Tool method directly instead of using reflection
                resolver.Tool(toolName, description, del);
            }

            return resolver;
        }

        private static Type GetDelegateType(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToList();
            parameterTypes.Add(method.ReturnType);
    
            return Expression.GetDelegateType(parameterTypes.ToArray());
        }
    }
}