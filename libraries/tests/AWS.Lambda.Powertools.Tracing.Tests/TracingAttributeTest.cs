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
using System.Text;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;
using NSubstitute;
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

            var configurations1 = Substitute.For<IPowertoolsConfigurations>();
            configurations1.TracingDisabled.Returns(false);
            configurations1.IsLambdaEnvironment.Returns(true);
            configurations1.IsServiceDefined.Returns(true);
            configurations1.Service.Returns(service);

            var configurations2 = Substitute.For<IPowertoolsConfigurations>();
            configurations2.TracingDisabled.Returns(false);
            configurations2.IsLambdaEnvironment.Returns(true);
            configurations2.IsServiceDefined.Returns(false);

            var recorder1 = Substitute.For<IXRayRecorder>();
            var recorder2 = Substitute.For<IXRayRecorder>();
            var recorder3 = Substitute.For<IXRayRecorder>();
            var recorder4 = Substitute.For<IXRayRecorder>();

            TracingAspectHandler.ResetForTest();
            var handler1 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1, recorder1);
            var handler2 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1, recorder2);
            var handler3 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations2, recorder3);
            var handler4 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations2, recorder4);

            var eventArgs = new AspectEventArgs { Name = methodName };

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
            recorder1.Received(1).AddAnnotation(
                Arg.Is<string>(i => i == "ColdStart"),
                Arg.Is<bool>(i => i == isColdStart)
            );

            recorder1.Received(1).AddAnnotation(
                Arg.Is<string>(i => i == "Service"),
                Arg.Is<string>(i => i == service)
            );

            recorder2.DidNotReceive().AddAnnotation(
                Arg.Any<string>(),
                Arg.Any<bool>()
            );

            recorder2.DidNotReceive().AddAnnotation(
                Arg.Any<string>(),
                Arg.Any<string>()
            );

            recorder3.Received(1).AddAnnotation(
                Arg.Is<string>(i => i == "ColdStart"),
                Arg.Is<bool>(i => i == !isColdStart)
            );

            recorder3.DidNotReceive().AddAnnotation(
                Arg.Any<string>(),
                Arg.Any<string>()
            );

            recorder4.DidNotReceive().AddAnnotation(
                Arg.Any<string>(),
                Arg.Any<bool>()
            );

            recorder4.DidNotReceive().AddAnnotation(
                Arg.Any<string>(),
                Arg.Any<string>()
            );
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
            var configurations1 = Substitute.For<IPowertoolsConfigurations>();
            configurations1.TracingDisabled.Returns(true);
            configurations1.IsLambdaEnvironment.Returns(true);

            var configurations2 = Substitute.For<IPowertoolsConfigurations>();
            configurations2.TracingDisabled.Returns(true);
            configurations2.IsLambdaEnvironment.Returns(true);

            var recorder1 = Substitute.For<IXRayRecorder>();
            var recorder2 = Substitute.For<IXRayRecorder>();

            TracingAspectHandler.ResetForTest();
            var handler1 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1, recorder1);
            var handler2 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations2, recorder2);

            var results = new[] { "A", "B" };
            var exception = new Exception("Test Exception");
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            // Cold Start Execution
            handler1.OnEntry(eventArgs);
            handler1.OnSuccess(eventArgs, results);
            void Act1() => handler1.OnException(eventArgs, exception);
            handler1.OnExit(eventArgs);

            // Warm Start Execution
            handler2.OnEntry(eventArgs);
            handler2.OnSuccess(eventArgs, results);
            void Act2() => handler2.OnException(eventArgs, exception);
            handler2.OnExit(eventArgs);

            // Assert
            recorder1.DidNotReceive().BeginSubsegment(Arg.Any<string>());
            recorder1.DidNotReceive().EndSubsegment();
            recorder1.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
            Assert.Throws<Exception>(Act1);
            recorder1.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Exception>()
            );

            recorder2.DidNotReceive().BeginSubsegment(Arg.Any<string>());
            recorder2.DidNotReceive().EndSubsegment();
            recorder2.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
            Assert.Throws<Exception>(Act2);
            recorder2.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Exception>()
            );
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
            var configurations1 = Substitute.For<IPowertoolsConfigurations>();
            configurations1.TracingDisabled.Returns(false);
            configurations1.IsLambdaEnvironment.Returns(false);

            var configurations2 = Substitute.For<IPowertoolsConfigurations>();
            configurations2.TracingDisabled.Returns(true);
            configurations2.IsLambdaEnvironment.Returns(true);

            var recorder1 = Substitute.For<IXRayRecorder>();
            var recorder2 = Substitute.For<IXRayRecorder>();

            TracingAspectHandler.ResetForTest();
            var handler1 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1, recorder1);
            var handler2 = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations1, recorder2);

            var results = new[] { "A", "B" };
            var exception = new Exception("Test Exception");
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            // Cold Start Execution
            handler1.OnEntry(eventArgs);
            handler1.OnSuccess(eventArgs, results);
            void Act1() => handler1.OnException(eventArgs, exception);
            handler1.OnExit(eventArgs);

            // Warm Start Execution
            handler2.OnEntry(eventArgs);
            handler2.OnSuccess(eventArgs, results);
            void Act2() => handler2.OnException(eventArgs, exception);
            handler2.OnExit(eventArgs);

            // Assert
            recorder1.DidNotReceive().BeginSubsegment(Arg.Any<string>());
            recorder1.DidNotReceive().EndSubsegment();
            recorder1.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
            Assert.Throws<Exception>(Act1);
            recorder1.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Exception>()
            );

            recorder2.DidNotReceive().BeginSubsegment(Arg.Any<string>());
            recorder2.DidNotReceive().EndSubsegment();
            recorder2.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
            Assert.Throws<Exception>(Act2);
            recorder2.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Exception>()
            );
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
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Received(1).BeginSubsegment(
                Arg.Is<string>(i => i == $"## {methodName}")
            );
        }

        [Fact]
        public void OnEntry_WhenSegmentNameHasValue_BeginSubsegmentWithValue()
        {
            // Arrange
            var segmentName = Guid.NewGuid().ToString();
            var methodName = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();

            var handler = new TracingAspectHandler(segmentName, null, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Received(1).BeginSubsegment(
                Arg.Is<string>(i => i == segmentName)
            );
        }

        [Fact]
        public void OnEntry_WhenNamespaceIsNull_SetNamespaceWithService()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            configurations.Service.Returns(service);
            var recorder = Substitute.For<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Received(1).SetNamespace(
                Arg.Is<string>(i => i == service)
            );
        }

        [Fact]
        public void OnEntry_WhenNamespaceHasValue_SetNamespaceWithValue()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            // Assert
            recorder.Received(1).SetNamespace(
                Arg.Is<string>(i => i == nameSpace)
            );
        }

        #endregion

        #region OnSuccess Tests

        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsTrue_CapturesResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            configurations.TracerCaptureResponse.Returns(true);
            var recorder = Substitute.For<IXRayRecorder>();
            var results = new[] { "A", "B" };

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Received(1).AddMetadata(
                Arg.Is<string>(i => i == nameSpace),
                Arg.Is<string>(i => i == $"{methodName} response"),
                Arg.Is<string[]>(i =>
                    i.First() == results.First() &&
                    i.Last() == results.Last()
                )
            );
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsFalse_DoesNotCaptureResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            configurations.TracerCaptureResponse.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var results = new[] { "A", "B" };

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsResponse_CapturesResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var results = new[] { "A", "B" };

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Response,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Received(1).AddMetadata(
                Arg.Is<string>(i => i == nameSpace),
                Arg.Is<string>(i => i == $"{methodName} response"),
                Arg.Is<string[]>(i =>
                    i.First() == results.First() &&
                    i.Last() == results.Last()
                )
            );
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsResponseAndError_CapturesResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var results = new[] { "A", "B" };

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.ResponseAndError,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.Received(1).AddMetadata(
                Arg.Is<string>(i => i == nameSpace),
                Arg.Is<string>(i => i == $"{methodName} response"),
                Arg.Is<string[]>(i =>
                    i.First() == results.First() &&
                    i.Last() == results.Last()
                )
            );
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsError_DoesNotCaptureResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var results = new[] { "A", "B" };

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Error,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsDisabled_DoesNotCaptureResponse()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var results = new[] { "A", "B" };

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Disabled,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnSuccess(eventArgs, results);

            // Assert
            recorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
        }

        #endregion

        #region OnException Tests

        [Fact]
        public void OnException_WhenTracerCaptureErrorEnvironmentVariableIsTrue_CapturesError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            configurations.TracerCaptureError.Returns(true);
            var recorder = Substitute.For<IXRayRecorder>();
            var exception = new Exception("Test Exception");
            var message = GetException(exception);

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            void Act() => handler.OnException(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>((Action)Act);
            recorder.Received(1).AddMetadata(
                Arg.Is<string>(i => i == nameSpace),
                Arg.Is<string>(i => i == $"{methodName} error"),
                Arg.Is<string>(i => i == message)
            );
        }

        [Fact]
        public void OnException_WhenTracerCaptureErrorEnvironmentVariableIsTrueFalse_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            configurations.TracerCaptureError.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            void Act() => handler.OnException(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>((Action)Act);
            recorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Exception>()
            );
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsError_CapturesError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var exception = new Exception("Test Exception");
            var message = GetException(exception);

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Error,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            void Act() => handler.OnException(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>((Action)Act);
            recorder.Received(1).AddMetadata(
                Arg.Is<string>(i => i == nameSpace),
                Arg.Is<string>(i => i == $"{methodName} error"),
                Arg.Is<string>(i => i == message)
            );
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponseAndError_CapturesError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var exception = new Exception("Test Exception");
            var message = GetException(exception);

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.ResponseAndError,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            void Act() => handler.OnException(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>((Action)Act);
            recorder.Received(1).AddMetadata(
                Arg.Is<string>(i => i == nameSpace),
                Arg.Is<string>(i => i == $"{methodName} error"),
                Arg.Is<string>(i => i == message)
            );
        }


        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponse_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            configurations.TracerCaptureError.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Response,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            void Act() => handler.OnException(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>((Action)Act);
            recorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Exception>()
            );
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsDisabled_DoesNotCaptureError()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var nameSpace = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(true);
            configurations.TracingDisabled.Returns(false);
            configurations.TracerCaptureError.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();
            var exception = new Exception("Test Exception");

            var handler = new TracingAspectHandler(null, nameSpace, TracingCaptureMode.Disabled,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            void Act() => handler.OnException(eventArgs, exception);

            // Assert
            Assert.Throws<Exception>((Action)Act);
            recorder.DidNotReceive().AddMetadata(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Exception>()
            );
        }

        #endregion

        #region Utilities

        static string GetException(Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Exception type: {exception.GetType()}");
            sb.AppendLine($"Exception message: {exception.Message}");
            sb.AppendLine($"Stack trace: {exception.StackTrace}");

            if (exception.InnerException != null)
            {
                sb.AppendLine("---BEGIN InnerException--- ");
                sb.AppendLine($"Exception type {exception.InnerException.GetType()}");
                sb.AppendLine($"Exception message: {exception.InnerException.Message}");
                sb.AppendLine($"Stack trace: {exception.InnerException.StackTrace}");
                sb.AppendLine("---END Inner Exception");
            }

            return sb.ToString();
        }

        #endregion

        #region OnExit Tests

        [Fact]
        public void OnExit_WhenOutsideOfLambdaEnvironment_DoesNotEndSubsegment()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.IsLambdaEnvironment.Returns(false);
            configurations.TracingDisabled.Returns(true);
            configurations.IsSamLocal.Returns(false);
            var recorder = Substitute.For<IXRayRecorder>();

            var handler = new TracingAspectHandler(null, null, TracingCaptureMode.EnvironmentVariable,
                configurations, recorder);
            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnExit(eventArgs);

            // Assert
            recorder.DidNotReceive().EndSubsegment();
        }

        #endregion
    }
}