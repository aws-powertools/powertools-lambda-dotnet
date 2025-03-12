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

using Microsoft.AspNetCore.Http;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

/// <summary>
/// Represents a filter that captures and records metrics for HTTP endpoints.
/// </summary>
/// <remarks>
/// This filter is responsible for tracking cold starts and capturing metrics during HTTP request processing.
/// It integrates with the ASP.NET Core endpoint routing system to inject metrics collection at the endpoint level.
/// </remarks>
/// <inheritdoc cref="IEndpointFilter"/>
/// <inheritdoc cref="IDisposable"/>
public class MetricsFilter : IEndpointFilter, IDisposable
{
    private readonly ColdStartTracker _coldStartTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsFilter"/> class.
    /// </summary>
    public MetricsFilter(IMetrics metrics)
    {
        _coldStartTracker = new ColdStartTracker(metrics);
    }

    /// <summary>
    /// Invokes the filter asynchronously.
    /// </summary>
    /// <param name="context">The context for the endpoint filter invocation.</param>
    /// <param name="next">The delegate to invoke the next filter or endpoint.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the endpoint invocation.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            _coldStartTracker.TrackColdStart(context.HttpContext);
        }
        catch
        {
            // ignored
        }

        return await next(context);
    }

    /// <summary>
    /// Disposes of the resources used by the filter.
    /// </summary>
    public void Dispose()
    {
        _coldStartTracker.Dispose();
    }
}