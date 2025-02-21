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
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

/// <summary>
/// Provides extension methods for adding metrics middleware to the application pipeline.
/// </summary>
public static class MetricsMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware to capture and record metrics for HTTP requests.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder with the metrics middleware added.</returns>
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var metrics = context.RequestServices.GetRequiredService<IMetrics>();
            var metricsHelper = new MetricsHelper(metrics);
            await metricsHelper.CaptureColdStartMetrics(context);
            await next();
        });
    }
}