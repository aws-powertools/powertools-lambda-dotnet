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

namespace AWS.Lambda.Powertools.Tracing.Tests;

public class HandlerFunctions
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
    
    [Tracing(SegmentName = "## <<Main>$>g__Handler|0_0")]
    public void HandleWithInvalidSegmentName()
    {
        MethodWithInvalidSegmentName();
    }
    
    [Tracing(SegmentName = "Inval$#id | <Segment>")]
    private void MethodWithInvalidSegmentName()
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
    
    [Tracing(CaptureMode = TracingCaptureMode.Error)]
    public string[] HandleWithCaptureModeErrorInner(bool exception = false)
    {
        if (exception)
            throw new Exception("Failed", new Exception("Inner Exception!!"));
        return new[] { "A", "B" };
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    public string[] HandleWithCaptureModeDisabled(bool exception = false)
    {
        if (exception)
            throw new Exception("Failed");
        return new[] { "A", "B" };
    }
    
    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    public string DecoratedHandlerCaptureResponse()
    {
        DecoratedMethodCaptureDisabled();
        return "Hello World";
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    public string DecoratedMethodCaptureDisabled()
    {
        DecoratedMethodCaptureEnabled();
        return "DecoratedMethod Disabled";
    }
    
    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    private string DecoratedMethodCaptureEnabled()
    {
        return "DecoratedMethod Enabled";
    }
}