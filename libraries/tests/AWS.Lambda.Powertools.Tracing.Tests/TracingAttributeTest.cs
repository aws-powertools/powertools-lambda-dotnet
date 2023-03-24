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
using System.Linq;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.Powertools.Tracing.Tests
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
            var service = Guid.NewGuid().ToString();
            
            var configurations1 = new Mock<IPowertoolsConfigurations>();
            configurations1.Setup(c => c.TracingDisabled).Returns(false);
            configurations1.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations1.Setup(c => c.IsServiceDefined).Returns(true);
            configurations1.Setup(c => c.Service).Returns(service);
            
            var configurations2 = new Mock<IPowertoolsConfigurations>();
            configurations2.Setup(c => c.TracingDisabled).Returns(false);
            configurations2.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations2.Setup(c => c.IsServiceDefined).Returns(false);

            var recorder1 = new Mock<IXRayRecorder>();
            var recorder2 = new Mock<IXRayRecorder>();
            var recorder3 = new Mock<IXRayRecorder>();
            var recorder4 = new Mock<IXRayRecorder>();
            
            TracingAspectHandler.ResetForTest();
            var handler1 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1.Object, recorder1.Object);
            var handler2 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1.Object, recorder2.Object);
            var handler3 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations2.Object, recorder3.Object);
            var handler4 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations2.Object, recorder4.Object);

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
            
            recorder1.Verify(v =>
                v.AddAnnotation(
                    It.Is<string>(i => i == "Service"),
                    It.Is<string>(i => i == service)
                ), Times.Once);

            recorder2.Verify(v =>
                v.AddAnnotation(
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                ), Times.Never);
            
            recorder2.Verify(v =>
                v.AddAnnotation(
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);

            recorder3.Verify(v =>
                v.AddAnnotation(
                    It.Is<string>(i => i == "ColdStart"),
                    It.Is<bool>(i => i == !isColdStart)
                ), Times.Once);
            
            recorder3.Verify(v =>
                v.AddAnnotation(
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);

            recorder4.Verify(v =>
                v.AddAnnotation(
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                ), Times.Never);
            
            recorder3.Verify(v =>
                v.AddAnnotation(
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
        }
    }
    
    [Collection("Sequential")]
    public class TracingAttributeDisableTest
    {
        [Fact]
        public void Tracing_WhenTracerDisabled_DisablesTracing()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations1 = new Mock<IPowertoolsConfigurations>();
            configurations1.Setup(c => c.TracingDisabled).Returns(true);
            configurations1.Setup(c => c.IsLambdaEnvironment).Returns(true);

            var configurations2 = new Mock<IPowertoolsConfigurations>();
            configurations2.Setup(c => c.TracingDisabled).Returns(true);
            configurations2.Setup(c => c.IsLambdaEnvironment).Returns(true);

            var recorder1 = new Mock<IXRayRecorder>();
            var recorder2 = new Mock<IXRayRecorder>();

            TracingAspectHandler.ResetForTest();
            var handler1 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1.Object, recorder1.Object);
            var handler2 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1.Object, recorder2.Object);

            var results = new[] {"A", "B"};
            var exception = new Exception("Test Exception");
            var eventArgs = new AspectEventArgs { Name = methodName };
            
            // Act
            // Cold Start Execution
            handler1.OnEntry(eventArgs);
            handler1.OnSuccess(eventArgs, results);
            object Act1() => handler1.OnException<object>(eventArgs, exception);
            handler1.OnExit(eventArgs);
            
            // Warm Start Execution
            handler2.OnEntry(eventArgs);
            handler2.OnSuccess(eventArgs, results);
            object Act2() => handler2.OnException<object>(eventArgs, exception);
            handler2.OnExit(eventArgs);

            // Assert
            recorder1.Verify(v => v.BeginSubsegment(It.IsAny<string>()), Times.Never);
            recorder1.Verify(v => v.EndSubsegment(), Times.Never);
            recorder1.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
            Assert.Throws<Exception>(Act1);
            recorder1.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ), Times.Never);
            
            recorder2.Verify(v => v.BeginSubsegment(It.IsAny<string>()), Times.Never);
            recorder2.Verify(v => v.EndSubsegment(), Times.Never);
            recorder2.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
            Assert.Throws<Exception>(Act2);
            recorder2.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ), Times.Never);
        }
    }
    
     [Collection("Sequential")]
    public class TracingAttributeLambdaEnvironmentTest
    {
        [Fact]
        public void Tracing_WhenOutsideOfLambdaEnv_DisablesTracing()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations1 = new Mock<IPowertoolsConfigurations>();
            configurations1.Setup(c => c.TracingDisabled).Returns(false);
            configurations1.Setup(c => c.IsLambdaEnvironment).Returns(false);

            var configurations2 = new Mock<IPowertoolsConfigurations>();
            configurations2.Setup(c => c.TracingDisabled).Returns(true);
            configurations2.Setup(c => c.IsLambdaEnvironment).Returns(true);

            var recorder1 = new Mock<IXRayRecorder>();
            var recorder2 = new Mock<IXRayRecorder>();

            TracingAspectHandler.ResetForTest();
            var handler1 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1.Object, recorder1.Object);
            var handler2 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1.Object, recorder2.Object);

            var results = new[] {"A", "B"};
            var exception = new Exception("Test Exception");
            var eventArgs = new AspectEventArgs { Name = methodName };
            
            // Act
            // Cold Start Execution
            handler1.OnEntry(eventArgs);
            handler1.OnSuccess(eventArgs, results);
            object Act1() => handler1.OnException<object>(eventArgs, exception);
            handler1.OnExit(eventArgs);
            
            // Warm Start Execution
            handler2.OnEntry(eventArgs);
            handler2.OnSuccess(eventArgs, results);
            object Act2() => handler2.OnException<object>(eventArgs, exception);
            handler2.OnExit(eventArgs);

            // Assert
            recorder1.Verify(v => v.BeginSubsegment(It.IsAny<string>()), Times.Never);
            recorder1.Verify(v => v.EndSubsegment(), Times.Never);
            recorder1.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
            Assert.Throws<Exception>(Act1);
            recorder1.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ), Times.Never);
            
            recorder2.Verify(v => v.BeginSubsegment(It.IsAny<string>()), Times.Never);
            recorder2.Verify(v => v.EndSubsegment(), Times.Never);
            recorder2.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
            Assert.Throws<Exception>(Act2);
            recorder2.Verify(v =>
                v.AddMetadata(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ), Times.Never);
        }
    }

    [Collection("Sequential")]
    public class TracingAttributeTest
    {
        public TracingAttributeTest()
        {
            TracingAspectHandler.ResetForTest();
        }

        #region OnEntry Tests
        
        [Fact]
        public void OnEntry_WhenSegmentNameIsNull_BeginSubsegmentWithMethodName()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == $"## {methodName}")
                ), Times.Once);
        }

        [Fact]
        public void OnEntry_WhenSegmentNameHasValue_BeginSubsegmentWithValue()
        {
            // Arrange
            var segmentName = Guid.NewGuid().ToString();
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(segmentName, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Verify(v =>
                v.BeginSubsegment(
                    It.Is<string>(i => i == segmentName)
                ), Times.Once);
        }

        [Fact]
        public void OnEntry_WhenNamespaceIsNull_SetNamespaceWithService()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            configurations.Setup(c => c.Service).Returns(service);
            var recorder = new Mock<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations.Object, recorder.Object);
            var eventArgs = new AspectEventArgs {Name = methodName};

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Verify(v =>
                v.SetNamespace(
                    It.Is<string>(i => i == service)
                ), Times.Once);
        }

        [Fact]
        public void OnEntry_WhenNamespaceHasValue_SetNamespaceWithValue()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            configurations.Setup(c => c.TracerCaptureResponse).Returns(true);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            configurations.Setup(c => c.TracerCaptureResponse).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            configurations.Setup(c => c.TracerCaptureError).Returns(true);
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");
            var message = GetException(exception);
            
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
                    It.Is<string>(i => i == message
                    )
                ), Times.Once);
        }

        [Fact]
        public void OnException_WhenTracerCaptureErrorEnvironmentVariableIsTrueFalse_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            configurations.Setup(c => c.TracerCaptureError).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");
            var message = GetException(exception);
            
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
                    It.Is<string>(i => i == message
                    )
                ), Times.Once);
        }
        
        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponseAndError_CapturesError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            var recorder = new Mock<IXRayRecorder>();
            var exception = new Exception("Test Exception");
            var message = GetException(exception);
            
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
                    It.Is<string>(i => i == message
                    )
                ), Times.Once);
        }
        
        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponse_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            configurations.Setup(c => c.TracerCaptureError).Returns(false);
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
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(true);
            configurations.Setup(c => c.TracingDisabled).Returns(false);
            configurations.Setup(c => c.TracerCaptureError).Returns(false);
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
        
        #region Utilities

        static string GetException(Exception exception)
        {
            var message =
                "Exception type " + exception.GetType() + Environment.NewLine +
                "Exception message: " + exception.Message + Environment.NewLine +
                "Stack trace: " + exception.StackTrace + Environment.NewLine;
            if (exception.InnerException != null)
            {
                message += "---BEGIN InnerException--- " + Environment.NewLine +
                           "Exception type " + exception.InnerException.GetType() + Environment.NewLine +
                           "Exception message: " + exception.InnerException.Message + Environment.NewLine +
                           "Stack trace: " + exception.InnerException.StackTrace + Environment.NewLine +
                           "---END Inner Exception";
            }
            return message;
        }

        #endregion
        
        #region OnExit Tests

        [Fact]
        public void OnExit_WhenOutsideOfLambdaEnvironment_DoesNotEndSubsegment()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.IsLambdaEnvironment).Returns(false);
            configurations.Setup(c => c.TracingDisabled).Returns(true);
            configurations.Setup(c => c.IsSamLocal).Returns(false);
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