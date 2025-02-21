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

using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Http;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;


/// <summary>
/// Helper class for capturing and recording metrics in ASP.NET Core applications.
/// </summary>
public class MetricsHelper
{
    private readonly IMetrics _metrics;
    private static bool _isColdStart = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsHelper"/> class.
    /// </summary>
    /// <param name="metrics">The metrics instance to use for recording metrics.</param>
    public MetricsHelper(IMetrics metrics)
    {
        _metrics = metrics;
    }

    /// <summary>
    /// Captures cold start metrics for the given HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CaptureColdStartMetrics(HttpContext context)
    {
        if (_metrics.Options.CaptureColdStart == null || !_metrics.Options.CaptureColdStart.Value || !_isColdStart)
            return Task.CompletedTask;

        var defaultDimensions = _metrics.Options.DefaultDimensions;
        _isColdStart = false;

        if (context.Items["LambdaContext"] is ILambdaContext lambdaContext)
        {
            defaultDimensions?.Add("FunctionName", lambdaContext.FunctionName);
            _metrics.SetDefaultDimensions(defaultDimensions);
        }

        _metrics.PushSingleMetric(
            "ColdStart",
            1.0,
            MetricUnit.Count,
            _metrics.Options.Namespace,
            _metrics.Options.Service,
            defaultDimensions
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets the cold start flag for testing purposes.
    /// </summary>
    internal static void ResetColdStart()
    {
        _isColdStart = true;
    }
}