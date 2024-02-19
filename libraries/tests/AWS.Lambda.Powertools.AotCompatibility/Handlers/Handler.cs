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
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;

namespace AWS.Lambda.Powertools.AotCompatibility.Handlers;

public class Handler
{
    [Logging(LogEvent = true)]
    [Metrics(CaptureColdStart = true, Namespace = "PT Demo NS")]
    public async Task<string> Handle(string input, ILambdaContext context)
    {
        Logger.LogInformation("Hello world!");
        
        Metrics.Metrics.AddMetric("Metric1", 1, MetricUnit.Count);

        await Task.Delay(1);
        return input;
    }
}