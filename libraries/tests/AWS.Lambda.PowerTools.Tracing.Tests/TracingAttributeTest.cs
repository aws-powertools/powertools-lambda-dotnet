using System;
using System.Linq;
using Amazon.Lambda.PowerTools.Tracing;
using Amazon.Lambda.PowerTools.Tracing.Internal;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.PowerTools.Tracing.Tests
{
    [Collection("Sequential")]
    public class TracingAttributeColdStartTest
    {
        [Fact]
        public void OnEntry_WhenFirstCall_CapturesColdStart()
        {
            // Arrange
            const bool isColdStart = true;
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();

            var recorder1 = new Mock<IXRayRecorder>();
            var recorder2 = new Mock<IXRayRecorder>();
            var recorder3 = new Mock<IXRayRecorder>();
            var recorder4 = new Mock<IXRayRecorder>();

            var handler1 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder1.Object);
            var handler2 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder2.Object);
            var handler3 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder3.Object);
            var handler4 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder4.Object);

            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            // Cold Start Execution
            handler1.OnEntry(eventArgs);
            handler2.OnEntry(eventArgs);
            handler2.OnExit(eventArgs);
            handler1.OnExit(eventArgs);

            // Warm Start Execution
            handler3.OnEntry(eventArgs);
            handler4.OnEntry(eventArgs);
            handler4.OnExit(eventArgs);
            handler3.OnExit(eventArgs);

            // Assert
            recorder1.Verify(v =>
                v.AddAnnotation(
                    It.Is<string>(i => i == "ColdStart"),
                    It.Is<bool>(i => i == isColdStart)
                ), Times.Once);

            recorder2.Verify(v =>
                v.AddAnnotation(
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                ), Times.Never);

            recorder3.Verify(v =>
                v.AddAnnotation(
                    It.Is<string>(i => i == "ColdStart"),
                    It.Is<bool>(i => i == !isColdStart)
                ), Times.Once);

            recorder4.Verify(v =>
                v.AddAnnotation(
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                ), Times.Never);
        }
    }

    [Collection("Sequential")]
    public class TracingAttributeTest
    {
        #region OnEntry Tests

        [Fact]
        public void OnEntry_WhenSegmentNameIsNull_BeginSubsegmentWithMethodName()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == $"## {methodName}"),
                    It.IsAny<DateTime?>()
                ), Times.Once);
        }

        [Fact]
        public void OnEntry_WhenSegmentNameHasValue_BeginSubsegmentWithValue()
        {
            // Arrange
            var segmentName = Guid.NewGuid().ToString();
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(segmentName, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == segmentName),
                    It.IsAny<DateTime?>()
                ), Times.Once);
        }

        [Fact]
        public void OnEntry_WhenNamespaceIsNull_SetNamespaceWithServiceName()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var serviceName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.ServiceName
            ).Returns(serviceName);
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Verify(v =>
                v.SetNamespace(
                    It.Is<string>(i => i == serviceName)
                ), Times.Once);
        }

        [Fact]
        public void OnEntry_WhenNamespaceHasValue_SetNamespaceWithValue()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Verify(v =>
                v.SetNamespace(
                    It.Is<string>(i => i == nameSpace)
                ), Times.Once);
        }

        #endregion

        #region OnSuccess Tests

        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsTrue_CapturesResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.TracerCaptureResponse
            ).Returns(true);
            var recorder = new Mock<IXRayRecorder>();
            var results = new[] {"A", "B"};

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Verify(v =>
                v.AddMetadata(
                    It.Is<string>(i => i == nameSpace),
                    It.Is<string>(i => i == $"{methodName} response"),
                    It.Is<string[]>(i =>
                        i.First() == results.First() &&
                        i.Last() == results.Last()
                    )
                ), Times.Once);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsFalse_DoesNotCaptureResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.TracerCaptureResponse
            ).Returns(false);
            var recorder = new Mock<IXRayRecorder>();
            var results = new[] {"A", "B"};

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsResponse_CapturesResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();
            var results = new[] {"A", "B"};

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Response,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Verify(v =>
                v.AddMetadata(
                    It.Is<string>(i => i == nameSpace),
                    It.Is<string>(i => i == $"{methodName} response"),
                    It.Is<string[]>(i =>
                        i.First() == results.First() &&
                        i.Last() == results.Last()
                    )
                ), Times.Once);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsResponseAndError_CapturesResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();
            var results = new[] {"A", "B"};

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.ResponseAndError,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Verify(v =>
                v.AddMetadata(
                    It.Is<string>(i => i == nameSpace),
                    It.Is<string>(i => i == $"{methodName} response"),
                    It.Is<string[]>(i =>
                        i.First() == results.First() &&
                        i.Last() == results.Last()
                    )
                ), Times.Once);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsError_DoesNotCaptureResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();
            var results = new[] {"A", "B"};

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Error,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsDisabled_DoesNotCaptureResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();
            var results = new[] {"A", "B"};

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Disabled,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
        }

        #endregion

        #region OnException Tests

        [Fact]
        public void OnException_WhenTracerCaptureErrorEnvironmentVariableIsTrue_CapturesError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.TracerCaptureError
            ).Returns(true);
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            object Act() => handler.OnException<object>(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>(Act);
            recorder.Verify(v =>
                v.AddMetadata(
                    It.Is<string>(i => i == nameSpace),
                    It.Is<string>(i => i == $"{methodName} error"),
                    It.Is<Exception>(i => i == exception
                    )
                ), Times.Once);
        }

        [Fact]
        public void OnException_WhenTracerCaptureErrorEnvironmentVariableIsTrueFalse_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.TracerCaptureError
            ).Returns(false);
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            object Act() => handler.OnException<object>(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>(Act);
            recorder.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ), Times.Never);
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsError_CapturesError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Error,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            object Act() => handler.OnException<object>(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>(Act);
            recorder.Verify(v =>
                v.AddMetadata(
                    It.Is<string>(i => i == nameSpace),
                    It.Is<string>(i => i == $"{methodName} error"),
                    It.Is<Exception>(i => i == exception
                    )
                ), Times.Once);
        }
        
        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponseAndError_CapturesError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.ResponseAndError,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            object Act() => handler.OnException<object>(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>(Act);
            recorder.Verify(v =>
                v.AddMetadata(
                    It.Is<string>(i => i == nameSpace),
                    It.Is<string>(i => i == $"{methodName} error"),
                    It.Is<Exception>(i => i == exception
                    )
                ), Times.Once);
        }
        
        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponse_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.TracerCaptureError
            ).Returns(false);
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Response,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            object Act() => handler.OnException<object>(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>(Act);
            recorder.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ), Times.Never);
        }
        
        [Fact]
        public void OnException_WhenTracerCaptureModeIsDisabled_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.TracerCaptureError
            ).Returns(false);
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Disabled,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            object Act() => handler.OnException<object>(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>(Act);
            recorder.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ), Times.Never);
        }

        #endregion
        
        #region OnExit Tests
        
        [Fact]
        public void OnExit_WhenIsNotSamLocal_EndSubsegment()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.IsSamLocal
            ).Returns(false);
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnExit(eventArgs);

            // Assert
            recorder.Verify(v => v.EndSubsegment(), Times.Once);
        }
        
        [Fact]
        public void OnExit_WhenIsSamLocal_DoesNotEndSubsegment()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            configurations.Setup(c =>
                c.IsSamLocal
            ).Returns(true);
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnExit(eventArgs);

            // Assert
            recorder.Verify(v => v.EndSubsegment(), Times.Never);
        }
        
        #endregion
    }
}