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

using System;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

public class Handlers
{
    [Tracing()]
    public string[] Handle()
    {
        return new[] { "A", "B" };
    }
    
    [Tracing(SegmentName = "SegmentName")]
    public void HandleWithSegmentName()
    {
        
    }
    
    [Tracing(Namespace = "Namespace Defined")]
    public void HandleWithNamespace()
    {
        
    }
    
    [Tracing()]
    public void HandleThrowsException(string exception)
    {
        throw new Exception(exception);
    }

    [Tracing(CaptureMode = TracingCaptureMode.Response)]
    public string[] HandleWithCaptureModeResponse(bool exception = false)
    {
        if (exception)
            throw new Exception("Failed");
        
        return new[] { "A", "B" };
    }
    
    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    public string[] HandleWithCaptureModeResponseAndError(bool exception = false)
    {
        if (exception)
            throw new Exception("Failed");
        return new[] { "A", "B" };
    }

    [Tracing(CaptureMode = TracingCaptureMode.Error)]
    public string[] HandleWithCaptureModeError(bool exception = false)
    {
        if (exception)
            throw new Exception("Failed");
        return new[] { "A", "B" };
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    public string[] HandleWithCaptureModeDisabled(bool exception = false)
    {
        if (exception)
            throw new Exception("Failed");
        return new[] { "A", "B" };
    }
}