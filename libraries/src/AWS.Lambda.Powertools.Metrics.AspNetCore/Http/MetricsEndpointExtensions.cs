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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

/// <summary>
/// Provides extension methods for adding metrics to route handlers.
/// </summary>
public static class MetricsEndpointExtensions
{
    /// <summary>
    /// Adds a metrics filter to the specified route handler builder.
    /// This will capture cold start (if CaptureColdStart is enabled) metrics and flush metrics on function exit.
    /// </summary>
    /// <param name="builder">The route handler builder to add the metrics filter to.</param>
    /// <returns>The route handler builder with the metrics filter added.</returns>
    public static RouteHandlerBuilder WithMetrics(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<MetricsFilter>();
        return builder;
    }
}
