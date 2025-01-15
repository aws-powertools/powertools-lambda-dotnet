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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Utils;
using AWS.Lambda.Powertools.Tracing.Internal;
using NSubstitute;
using Xunit;

#if NET8_0_OR_GREATER
using AWS.Lambda.Powertools.Tracing.Serializers;
using AWS.Lambda.Powertools.Tracing.Tests.Serializers;
#endif

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
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(true);
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

#if NET8_0_OR_GREATER
    [Fact]
    public void Around_SyncMethod_HandlesResponseAndSegmentCorrectly_AOT()
    {
        // Arrange
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var context = new TestJsonContext(new JsonSerializerOptions());
        PowertoolsTracingSerializer.AddSerializerContext(context);

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
            PowertoolsTracingSerializer.Serialize(result));
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task Around_AsyncMethod_HandlesResponseAndSegmentCorrectly_AOT()
    {
        // Arrange
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var context = new TestJsonContext(new JsonSerializerOptions());
        PowertoolsTracingSerializer.AddSerializerContext(context);

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
            PowertoolsTracingSerializer.Serialize(result));
        _mockXRayRecorder.Received(1).EndSubsegment();
    }
#endif

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
    
        // Create a completed task with exception before passing to Around
        var exceptionTask = Task.FromException(expectedException);
        Func<object[], object> target = _ => exceptionTask;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            var wrappedTask = _handler.Around(
                methodName, 
                Array.Empty<object>(), 
                target, 
                new Attribute[] { attribute }
            ) as Task;

            await wrappedTask!;
        });

        // Assert
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

    [Fact]
    public void TracingDisabled_WhenTracingDisabledTrue_ReturnsTrue()
    {
        // Arrange
        _mockConfigurations.TracingDisabled.Returns(true);

        // Act
        var result = _handler.Around(
            "TestMethod",
            Array.Empty<object>(),
            _ => null,
            new Attribute[] { new TracingAttribute() }
        );

        // Assert
        Assert.Null(result);
        _mockXRayRecorder.DidNotReceive().BeginSubsegment(Arg.Any<string>());
    }

    [Fact]
    public void TracingDisabled_WhenNotInLambdaEnvironment_ReturnsTrue()
    {
        // Arrange
        _mockConfigurations.IsLambdaEnvironment.Returns(false);

        // Act
        var result = _handler.Around(
            "TestMethod",
            Array.Empty<object>(),
            _ => null,
            new Attribute[] { new TracingAttribute() }
        );

        // Assert
        Assert.Null(result);
        _mockXRayRecorder.DidNotReceive().BeginSubsegment(Arg.Any<string>());
    }

    [Fact]
    public async Task WrapVoidTask_SuccessfulExecution_OnlyEndsSubsegment()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        const string methodName = "TestMethod";
        const string nameSpace = "TestNamespace";

        // Complete the task FIRST
        tcs.SetResult();

        // Act - now when Around calls GetResult(), the task is already complete
        var wrappedTask = _handler.Around(
            methodName,
            new object[] { tcs.Task },
            args => args[0],
            new Attribute[]
                { new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.Response } }
        ) as Task;

        // This should now complete quickly since the underlying task is already done
        await wrappedTask!;

        // Assert
        _mockXRayRecorder.DidNotReceive().AddMetadata(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object>()
        );
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task WrapVoidTask_WithException_HandlesExceptionAndEndsSubsegment()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        const string methodName = "TestMethod";
        const string nameSpace = "TestNamespace";
        var expectedException = new Exception("Test exception");

        // Complete the task with exception BEFORE passing to Around
        tcs.SetException(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            var wrappedTask = _handler.Around(
                methodName,
                new object[] { tcs.Task },
                args => args[0],
                new Attribute[]
                {
                    new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.ResponseAndError }
                }
            ) as Task;

            await wrappedTask!;
        });

        _mockXRayRecorder.Received(1).AddMetadata(
            Arg.Is(nameSpace),
            Arg.Is($"{methodName} error"),
            Arg.Is<string>(s => s.Contains("Test exception"))
        );
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task WrapVoidTask_WithCancellation_EndsSubsegment()
    {
        // Arrange
        TracingAspect.ResetForTest();

        var mockXRayRecorder = Substitute.For<IXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();

        mockConfigurations.IsLambdaEnvironment.Returns(true);
        mockConfigurations.TracingDisabled.Returns(false);
        mockConfigurations.Service.Returns("TestService");
        mockConfigurations.IsServiceDefined.Returns(true);
        mockConfigurations.TracerCaptureResponse.Returns(true);
        mockConfigurations.TracerCaptureError.Returns(true);

        var handler = new TracingAspect(mockConfigurations, mockXRayRecorder);

        // Create a cancellation token source and cancel it
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel it first
    
        // Now create the cancelled task
        var task = Task.FromCanceled(cts.Token);

        const string methodName = "TestMethod";
        const string nameSpace = "TestNamespace";

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            var wrappedTask = handler.Around(
                methodName,
                new object[] { task },
                args => args[0],
                new Attribute[]
                    { new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.ResponseAndError } }
            ) as Task;

            await wrappedTask!;
        });

        mockXRayRecorder.Received(1).EndSubsegment();
        mockXRayRecorder.Received(1).BeginSubsegment(Arg.Any<string>());
        mockXRayRecorder.Received(1).SetNamespace(nameSpace);
        mockXRayRecorder.Received(1).AddAnnotation("ColdStart", true);
        mockXRayRecorder.Received(1).AddAnnotation("Service", "TestService");
    }

    [Fact]
    public void CaptureResponse_WhenTracingDisabled_ReturnsFalse()
    {
        // Arrange
        _mockConfigurations.TracingDisabled.Returns(true);
        var attribute = new TracingAttribute { CaptureMode = TracingCaptureMode.Response };
        var methodName = "TestMethod";
        Func<object[], object> target = _ => "result";

        // Act
        _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Assert
        _mockXRayRecorder.DidNotReceive().AddMetadata(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object>());
    }

    [Theory]
    [InlineData(TracingCaptureMode.EnvironmentVariable, true)]
    [InlineData(TracingCaptureMode.Response, true)]
    [InlineData(TracingCaptureMode.ResponseAndError, true)]
    [InlineData(TracingCaptureMode.Error, false)]
    [InlineData(TracingCaptureMode.Disabled, false)]
    public void CaptureResponse_WithDifferentModes_ReturnsExpectedResult(TracingCaptureMode mode, bool expectedCapture)
    {
        // Arrange
        _mockConfigurations.TracerCaptureResponse.Returns(true);
        var attribute = new TracingAttribute { CaptureMode = mode };
        var methodName = "TestMethod";
        var result = "test result";
        Func<object[], object> target = _ => result;

        // Act
        _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Assert
        if (expectedCapture)
        {
            _mockXRayRecorder.Received(1).AddMetadata(
                Arg.Any<string>(),
                $"{methodName} response",
                result);
        }
        else
        {
            _mockXRayRecorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                $"{methodName} response",
                Arg.Any<object>());
        }
    }

    [Fact]
    public void CaptureError_WhenTracingDisabled_ReturnsFalse()
    {
        // Arrange
        _mockConfigurations.TracingDisabled.Returns(true);
        var attribute = new TracingAttribute { CaptureMode = TracingCaptureMode.Error };
        var methodName = "TestMethod";
        var expectedException = new Exception("Test exception");
        Func<object[], object> target = _ => throw expectedException;

        // Act & Assert
        Assert.Throws<Exception>(() =>
            _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute }));

        // Verify no error metadata was added
        _mockXRayRecorder.DidNotReceive().AddMetadata(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Theory]
    [InlineData(TracingCaptureMode.EnvironmentVariable, true)]
    [InlineData(TracingCaptureMode.Error, true)]
    [InlineData(TracingCaptureMode.ResponseAndError, true)]
    [InlineData(TracingCaptureMode.Response, false)]
    [InlineData(TracingCaptureMode.Disabled, false)]
    public void CaptureError_WithDifferentModes_ReturnsExpectedResult(TracingCaptureMode mode, bool expectedCapture)
    {
        // Arrange
        _mockConfigurations.TracerCaptureError.Returns(true);
        var attribute = new TracingAttribute { CaptureMode = mode };
        var methodName = "TestMethod";
        var expectedException = new Exception("Test exception");
        Func<object[], object> target = _ => throw expectedException;

        // Act & Assert
        Assert.Throws<Exception>(() =>
            _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute }));

        // Verify error metadata was added or not based on expected capture
        if (expectedCapture)
        {
            _mockXRayRecorder.Received(1).AddMetadata(
                Arg.Any<string>(),
                $"{methodName} error",
                Arg.Is<string>(s => s.Contains(expectedException.Message)));
        }
        else
        {
            _mockXRayRecorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                $"{methodName} error",
                Arg.Any<string>());
        }
    }

    [Fact]
    public async Task Around_AsyncMethodWithResult_HandlesTaskResultProperty()
    {
        // Arrange
        var attribute = new TracingAttribute();
        var methodName = "TestAsyncMethod";
        var expectedResult = "test result";
        var taskWithResult = Task.FromResult(expectedResult);
        Func<object[], object> target = _ => taskWithResult;

        // Act
        var taskResult = _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Wait for the async operation to complete
        if (taskResult is Task task)
        {
            await task;
        }

        // Assert with wait
        await Task.Delay(100); // Give time for the continuation to complete

        _mockXRayRecorder.Received(1).AddMetadata(
            "TestService", // This matches what's set in the test constructor
            $"{methodName} response",
            expectedResult);
    }

    [Fact]
    public async Task Around_AsyncMethodWithoutResult_HandlesNullTaskResultProperty()
    {
        // Arrange
        var attribute = new TracingAttribute();
        var methodName = "TestAsyncMethod";
        var taskWithoutResult = new Task(() => { }); // Task without Result property
        taskWithoutResult.Start();
        Func<object[], object> target = _ => taskWithoutResult;

        // Act
        var taskResult = _handler.Around(methodName, Array.Empty<object>(), target, new Attribute[] { attribute });

        // Wait for the async operation to complete
        if (taskResult is Task task)
        {
            await task;
        }

        // Assert with wait
        await Task.Delay(100); // Give time for the continuation to complete

        _mockXRayRecorder.DidNotReceive().AddMetadata(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object>()
        );
        _mockXRayRecorder.Received(1).EndSubsegment();
    }
    
    [Fact]
    public async Task Around_VoidTask_DoesNotAddResponseMetadata()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        const string methodName = "VoidTaskMethod";
        const string nameSpace = "TestNamespace";

        // Complete the task before passing to Around
        tcs.SetResult();

        // Act
        var wrappedTask = _handler.Around(
            methodName,
            new object[] { tcs.Task },
            args => args[0],
            new Attribute[]
                { new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.Response } }
        ) as Task;

        await wrappedTask!;

        // Assert
        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).EndSubsegment();
        // Verify that AddMetadata was NOT called with response
        _mockXRayRecorder.DidNotReceive().AddMetadata(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.EndsWith("response")),
            Arg.Any<object>()
        );
    }

    [Fact]
    public async Task Around_VoidTask_HandlesExceptionCorrectly()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        const string methodName = "VoidTaskMethod";
        const string nameSpace = "TestNamespace";
        var expectedException = new Exception("Test exception");

        // Fail the task before passing to Around
        tcs.SetException(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            var wrappedTask = _handler.Around(
                methodName,
                new object[] { tcs.Task },
                args => args[0],
                new Attribute[]
                    { new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.ResponseAndError } }
            ) as Task;

            await wrappedTask!;
        });

        // Assert
        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).AddMetadata(
            Arg.Is(nameSpace),
            Arg.Is($"{methodName} error"),
            Arg.Is<string>(s => s.Contains(expectedException.Message))
        );
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task Around_VoidTask_WithCancellation_EndsSegmentCorrectly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource();
        const string methodName = "VoidTaskMethod";
        const string nameSpace = "TestNamespace";

        // Cancel before passing to Around
        cts.Cancel();
        tcs.SetCanceled(cts.Token);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            var wrappedTask = _handler.Around(
                methodName,
                new object[] { tcs.Task },
                args => args[0],
                new Attribute[]
                    { new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.Response } }
            ) as Task;

            await wrappedTask!;
        });

        // Assert
        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task Around_TaskWithResult_AddsResponseMetadata()
    {
        // Arrange
        var tcs = new TaskCompletionSource<string>();
        const string methodName = "TaskWithResultMethod";
        const string nameSpace = "TestNamespace";
        const string result = "test result";

        // Complete the task before passing to Around
        tcs.SetResult(result);

        // Act
        var wrappedTask = _handler.Around(
            methodName,
            new object[] { tcs.Task },
            args => args[0],
            new Attribute[]
                { new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.Response } }
        ) as Task<string>;

        await wrappedTask!;

        // Assert
        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).AddMetadata(
            Arg.Is(nameSpace),
            Arg.Is($"{methodName} response"),
            Arg.Is<string>(s => s == result)
        );
        _mockXRayRecorder.Received(1).EndSubsegment();
    }

    [Fact]
    public async Task Around_NullResult_DoesNotAddResponseMetadata()
    {
        // Arrange
        var tcs = new TaskCompletionSource<string>();
        const string methodName = "NullResultMethod";
        const string nameSpace = "TestNamespace";

        // Complete the task with null before passing to Around
        tcs.SetResult(null!);

        // Act
        var wrappedTask = _handler.Around(
            methodName,
            new object[] { tcs.Task },
            args => args[0],
            new Attribute[]
                { new TracingAttribute { Namespace = nameSpace, CaptureMode = TracingCaptureMode.Response } }
        ) as Task<string>;

        await wrappedTask!;

        // Assert
        _mockXRayRecorder.Received(1).BeginSubsegment($"## {methodName}");
        _mockXRayRecorder.Received(1).EndSubsegment();
        // Verify that AddMetadata was NOT called with response
        _mockXRayRecorder.DidNotReceive().AddMetadata(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.EndsWith("response")),
            Arg.Any<object>()
        );
    }

    [Fact]
    public async Task Around_TracingDisabled_DoesNotAddSegments()
    {
        // Arrange
        _mockConfigurations.TracingDisabled.Returns(true);
        var tcs = new TaskCompletionSource();
        const string methodName = "DisabledTracingMethod";

        // Complete the task before passing to Around
        tcs.SetResult();

        // Act
        var wrappedTask = _handler.Around(
            methodName,
            new object[] { tcs.Task },
            args => args[0],
            new Attribute[]
                { new TracingAttribute() }
        ) as Task;

        await wrappedTask!;

        // Assert
        _mockXRayRecorder.DidNotReceive().BeginSubsegment(Arg.Any<string>());
        _mockXRayRecorder.DidNotReceive().EndSubsegment();
        _mockXRayRecorder.DidNotReceive().AddMetadata(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object>()
        );
    }
}