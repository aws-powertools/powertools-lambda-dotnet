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

using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler.Resolvers
{
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