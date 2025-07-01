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