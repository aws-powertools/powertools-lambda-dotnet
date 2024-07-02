using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

public sealed class HandlerTests : IDisposable
{
    [Fact]
    public async Task Stack_Trace_Included_When_Decorator_Present()
    {
        // Arrange
        var handler = new ExceptionFunctionHandler();

        // Act
        Task Handle() => handler.Handle("whatever");
        
        // Assert
        var tracedException = await Assert.ThrowsAsync<NullReferenceException>(Handle);
        Assert.StartsWith("at AWS.Lambda.Powertools.Tracing.Tests.Handlers.ExceptionFunctionHandler.ThisThrows()", tracedException.StackTrace?.TrimStart());

    }
    
    [Fact]
    public async Task When_Decorator_Present_In_Generic_Method_Should_Not_Throw_When_Type_Changes()
    {
        // Arrange
        var handler = new FunctionHandlerForGeneric();

        // Act
        await handler.Handle("whatever");
        
        // Assert
    }
    
    [Fact]
    public void When_Handler_Is_Decorated()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
        var handler = new Handlers();
        var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

        // Act
        handler.Handle();
        var subSegment = segment.Subsegments[0];
        
        // Assert
        Assert.True(segment.IsSubsegmentsAdded);
        Assert.True(subSegment.IsAnnotationsAdded);
        Assert.True(subSegment.Annotations.Any());
        Assert.Equal("ColdStart", subSegment.Annotations.First().Key);
        Assert.Equal(true, subSegment.Annotations.First().Value);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "");
    }
}