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

using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

public class FullExampleHandler
{
    [Tracing(Namespace = "ns", CaptureMode = TracingCaptureMode.ResponseAndError)]
    public async Task<string> Handle(string text, ILambdaContext context)
    {
        Tracing.AddAnnotation("annotation", "value");
        await BusinessLogic1();
        
        return await Task.FromResult(text.ToUpper());
    }
    
    [Tracing(SegmentName = "First Call")]
    private async Task BusinessLogic1()
    {
        await BusinessLogic2();
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    private async Task BusinessLogic2()
    {
        Tracing.AddMetadata("metadata", "value");
        
        Tracing.WithSubsegment("localNamespace", "GetSomething", (subsegment) => {
            GetSomething();
        });
        
        await Task.FromResult(0);
    }

    private void GetSomething()
    {
        Tracing.AddAnnotation("getsomething", "value");
    }
}