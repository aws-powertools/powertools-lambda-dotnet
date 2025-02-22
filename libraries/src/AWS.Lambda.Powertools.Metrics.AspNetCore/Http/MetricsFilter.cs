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
public class MetricsFilter : IEndpointFilter
{
    private readonly MetricsHelper _metricsHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsFilter"/> class.
    /// </summary>
    /// <param name="metrics">The metrics instance to use for recording metrics.</param>
    public MetricsFilter(IMetrics metrics)
    {
        _metricsHelper = new MetricsHelper(metrics);
    }

    /// <summary>
    /// Invokes the filter asynchronously.
    /// </summary>
    /// <param name="context">The context for the endpoint filter invocation.</param>
    /// <param name="next">The delegate to invoke the next filter or endpoint.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the endpoint invocation.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        try
        {
            await _metricsHelper.CaptureColdStartMetrics(context.HttpContext);
            return result;
        }
        catch
        {
            // ignored
            return result;
        }
    }
}