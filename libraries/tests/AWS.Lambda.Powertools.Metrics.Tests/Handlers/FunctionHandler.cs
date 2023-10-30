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

using System.Globalization;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

public class FunctionHandler
{
    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task<string> HandleSameKey(string input)
    {
        Metrics.AddMetric("MyMetric", 1);
        Metrics.AddMetadata("MyMetric", "meta");

        await Task.Delay(1);

        return input.ToUpper(CultureInfo.InvariantCulture);
    }
    
    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task<string> HandleTestSecondCall(string input)
    {
        Metrics.AddMetric("MyMetric", 1);
        Metrics.AddMetadata("MyMetadata", "meta");
        
        await Task.Delay(1);

        return input.ToUpper(CultureInfo.InvariantCulture);
    }
}