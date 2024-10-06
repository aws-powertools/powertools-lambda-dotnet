using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Amazon.XRay.Recorder.Core;
using AWS.Lambda.Powertools.Tracing.Internal;
using AWS.Lambda.Powertools.Tracing.Tests.Handlers;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("Sequential")]
public sealed class HandlerTests : IDisposable
{
    public HandlerTests()
    {
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
    }
    
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
    public async Task Full_Example()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
        
        var handler = new FullExampleHandler();
        var context = new TestLambdaContext
        {
            FunctionName = "FullExampleLambda",
            FunctionVersion = "1",
            MemoryLimitInMB = 215,
            AwsRequestId = Guid.NewGuid().ToString("D")
        };

        // Act
        var facadeSegment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
        await handler.Handle("Hello World", context);
        var handleSegment = facadeSegment.Subsegments[0];
        
        // Assert
        Assert.True(handleSegment.IsAnnotationsAdded);
        Assert.True(handleSegment.IsSubsegmentsAdded);
        
        Assert.Equal("POWERTOOLS", handleSegment.Annotations["Service"]);
        Assert.True((bool)handleSegment.Annotations["ColdStart"]);
        Assert.Equal("value", handleSegment.Annotations["annotation"]);
        Assert.Equal("## Handle", handleSegment.Name);

        var firstCallSubsegment = handleSegment.Subsegments[0];
        
        Assert.Equal("First Call", firstCallSubsegment.Name);
        Assert.False(firstCallSubsegment.IsInProgress);
        Assert.False(firstCallSubsegment.IsAnnotationsAdded);
        // Assert.True(firstCallSubsegment.IsMetadataAdded);
        Assert.True(firstCallSubsegment.IsSubsegmentsAdded);
        
        var businessLogicSubsegment = firstCallSubsegment.Subsegments[0];
        
        Assert.Equal("## BusinessLogic2", businessLogicSubsegment.Name);
        Assert.True(businessLogicSubsegment.IsMetadataAdded);
        Assert.False(businessLogicSubsegment.IsInProgress);
        Assert.Single(businessLogicSubsegment.Metadata);
        var metadata = businessLogicSubsegment.Metadata["POWERTOOLS"];
        Assert.Contains("metadata", metadata.Keys.Cast<string>());
        Assert.Contains("value", metadata.Values.Cast<string>());
        Assert.True(businessLogicSubsegment.IsSubsegmentsAdded);
        
        var getSomethingSubsegment = businessLogicSubsegment.Subsegments[0];
        
        Assert.Equal("## GetSomething", getSomethingSubsegment.Name);
        Assert.Equal("localNamespace", getSomethingSubsegment.Namespace);
        Assert.True(getSomethingSubsegment.IsAnnotationsAdded);
        Assert.False(getSomethingSubsegment.IsSubsegmentsAdded);
        Assert.False(getSomethingSubsegment.IsInProgress);
        Assert.Equal("value", getSomethingSubsegment.Annotations["getsomething"]);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "");
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "");
        TracingAspectHandler.ResetForTest();
    }
}