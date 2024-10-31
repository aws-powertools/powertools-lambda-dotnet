using System;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Tracing.Internal;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("Sequential")]
public class TracingAspectTests
{
    private readonly IXRayRecorder _mockXRayRecorder;
    private readonly IPowertoolsConfigurations _mockConfigurations;
    private readonly TracingAspect _handler;

    public TracingAspectTests()
    {
        // Setup mocks
        _mockXRayRecorder = Substitute.For<IXRayRecorder>();
        _mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        
        // Configure default behavior
        _mockConfigurations.IsLambdaEnvironment.Returns(true);
        _mockConfigurations.TracingDisabled.Returns(false);
        _mockConfigurations.Service.Returns("TestService");
        _mockConfigurations.IsServiceDefined.Returns(true);
        _mockConfigurations.TracerCaptureResponse.Returns(true);
        _mockConfigurations.TracerCaptureError.Returns(true);

        // Setup test handler with mocks
        _handler = new TracingAspect(_mockConfigurations, _mockXRayRecorder);
        
        // Reset static state
        TracingAspect.ResetForTest();
    }

    [Fact]
    public void Around_SyncMethod_HandlesResponseAndSegmentCorrectly()
    {
        // Arrange
        var attribute = new TracingAttribute();
        var methodName = "TestMethod";
        var result = "test result";
        Func<object[], object> target = _ => result;

        // Act
        _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Assert
        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).AddAnnotation("ColdStart", true);
        _mockXRayRecorder.Received(1).AddAnnotation("Service", "TestService");
        _mockXRayRecorder.Received(1).AddMetadata(
            Arg.Any<string>(),
            $"{methodName} response",
            result);
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task Around_AsyncMethod_HandlesResponseAndSegmentCorrectly()
    {
        // Arrange
        var attribute = new TracingAttribute();
        var methodName = "TestAsyncMethod";
        var result = new TestResponse { Message = "test async result" };
        Func<object[], object> target = _ => Task.FromResult<object>(result);

        // Act
        var taskResult = _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Wait for the async operation to complete
        if (taskResult is Task task)
        {
            await task;
        }

        // Assert with wait
        await Task.Delay(100); // Give time for the continuation to complete
        
        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).AddAnnotation("ColdStart", true);
        _mockXRayRecorder.Received(1).AddAnnotation("Service", "TestService");
        _mockXRayRecorder.Received(1).AddMetadata(
            Arg.Any<string>(),
            $"{methodName} response",
            result);
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task Around_VoidAsyncMethod_HandlesSegmentCorrectly()
    {
        // Arrange
        var attribute = new TracingAttribute();
        var methodName = "TestVoidAsyncMethod";
        Func<object[], object> target = _ => Task.CompletedTask;

        // Act
        var taskResult = _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Wait for the async operation to complete
        if (taskResult is Task task)
        {
            await task;
        }

        // Assert with wait
        await Task.Delay(100); // Give time for the continuation to complete

        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).AddAnnotation("ColdStart", true);
        _mockXRayRecorder.Received(1).AddAnnotation("Service", "TestService");
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task Around_AsyncMethodWithException_HandlesErrorCorrectly()
    {
        // Arrange
        var attribute = new TracingAttribute();
        var methodName = "TestExceptionAsyncMethod";
        var expectedException = new Exception("Test exception");
        Func<object[], object> target = _ => Task.FromException(expectedException);

        // Act & Assert
        var taskResult = _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Wait for the async operation to complete
        if (taskResult is Task task)
        {
            await Assert.ThrowsAsync<Exception>(() => task);
        }

        // Assert with wait
        await Task.Delay(100); // Give time for the continuation to complete

        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).AddMetadata(
            Arg.Any<string>(),
            $"{methodName} error",
            Arg.Is<string>(s => s.Contains(expectedException.Message)));
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public void Around_TracingDisabled_DoesNotCreateSegment()
    {
        // Arrange
        _mockConfigurations.TracingDisabled.Returns(true);
        var attribute = new TracingAttribute();
        var methodName = "TestMethod";
        Func<object[], object> target = _ => "result";

        // Act
        _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Assert
        _mockXRayRecorder.DidNotReceive().BeginSubsegment(Arg.Any<string>());
        _mockXRayRecorder.DidNotReceive().EndSubsegment();
    }

    private class TestResponse
    {
        public string Message { get; set; }
    }
}