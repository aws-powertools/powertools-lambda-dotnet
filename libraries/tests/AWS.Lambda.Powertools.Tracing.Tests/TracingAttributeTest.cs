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
using Amazon.XRay.Recorder.Core;
using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.Powertools.Tracing.Tests
{
    [Collection("Sequential")]
    public class TracingAttributeColdStartTest : IDisposable
    {
        private readonly Handlers.Handlers _handler;

        public TracingAttributeColdStartTest()
        {
            _handler = new Handlers.Handlers();
        }
        
        [Fact]
        public void OnEntry_WhenFirstCall_CapturesColdStart()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");

            // Act
            // Cold Start Execution
            // Start segment
            var segmentCold = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();

            var subSegmentCold = segmentCold.Subsegments[0];

            // Warm Start Execution
            // Start segment
            var segmentWarm = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();
            var subSegmentWarm = segmentWarm.Subsegments[0];

            // Assert
            // Cold
            Assert.True(segmentCold.IsSubsegmentsAdded);
            Assert.Single(segmentCold.Subsegments);
            Assert.True(subSegmentCold.IsAnnotationsAdded);
            Assert.Equal(2, subSegmentCold.Annotations.Count());
            Assert.Equal(true, subSegmentCold.Annotations.Single(x => x.Key == "ColdStart").Value);
            Assert.Equal("POWERTOOLS", subSegmentCold.Annotations.Single(x => x.Key == "Service").Value);

            // Warm
            Assert.True(segmentWarm.IsSubsegmentsAdded);
            Assert.Single(segmentWarm.Subsegments);
            Assert.True(subSegmentWarm.IsAnnotationsAdded);
            Assert.Equal(2, subSegmentWarm.Annotations.Count());
            Assert.Equal(false, subSegmentWarm.Annotations.Single(x => x.Key == "ColdStart").Value);
            Assert.Equal("POWERTOOLS", subSegmentWarm.Annotations.Single(x => x.Key == "Service").Value);
        }

        [Fact]
        public void OnEntry_WhenFirstCall_And_Service_Not_Set_CapturesColdStart()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");

            // Act
            // Cold Start Execution
            // Start segment
            var segmentCold = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();
            var subSegmentCold = segmentCold.Subsegments[0];

            // Warm Start Execution
            // Start segment
            var segmentWarm = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();
            var subSegmentWarm = segmentWarm.Subsegments[0];

            // Assert
            // Cold
            Assert.True(segmentCold.IsSubsegmentsAdded);
            Assert.Single(segmentCold.Subsegments);
            Assert.True(subSegmentCold.IsAnnotationsAdded);
            Assert.Single(subSegmentCold.Annotations);
            Assert.Equal(true, subSegmentCold.Annotations.Single(x => x.Key == "ColdStart").Value);

            // Warm
            Assert.True(segmentWarm.IsSubsegmentsAdded);
            Assert.Single(segmentWarm.Subsegments);
            Assert.True(subSegmentWarm.IsAnnotationsAdded);
            Assert.Single(subSegmentWarm.Annotations);
            Assert.Equal(false, subSegmentWarm.Annotations.Single(x => x.Key == "ColdStart").Value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "");
            TracingAspect.ResetForTest();
        }
    }

    [Collection("Sequential")]
    public class TracingAttributeDisableTest : IDisposable
    {
        private readonly Handlers.Handlers _handler;

        public TracingAttributeDisableTest()
        {
            _handler = new Handlers.Handlers();
        }
        
        [Fact]
        public void Tracing_WhenTracerDisabled_DisablesTracing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "true");

            // Act
            // Cold Start Execution
            // Start segment
            var segmentCold = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();

            // Warm Start Execution
            // Start segment
            var segmentWarm = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();

            // Assert
            Assert.False(segmentCold.IsAnnotationsAdded);
            Assert.Empty(segmentCold.Annotations);
            Assert.False(segmentCold.IsSubsegmentsAdded);
            Assert.False(segmentCold.IsMetadataAdded);

            Assert.False(segmentWarm.IsAnnotationsAdded);
            Assert.Empty(segmentWarm.Annotations);
            Assert.False(segmentWarm.IsSubsegmentsAdded);
            Assert.False(segmentWarm.IsMetadataAdded);
        }

        public void Dispose()
        {
            ClearEnvironment();
        }

        private static void ClearEnvironment()
        {
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "");
            TracingAspect.ResetForTest();
        }
    }

    [Collection("Sequential")]
    public class TracingAttributeLambdaEnvironmentTest
    {
        private readonly Handlers.Handlers _handler;
    
        public TracingAttributeLambdaEnvironmentTest()
        {
            _handler = new Handlers.Handlers();
        }
        
        [Fact]
        public void Tracing_WhenOutsideOfLambdaEnv_DisablesTracing()
        {
            // Arrange
            
            // Need to manually create the initial segment
            AWSXRayRecorder.Instance.BeginSegment("foo");
    
            // Act
            // Cold Start Execution
            _handler.Handle();
            var segmentCold = AWSXRayRecorder.Instance.TraceContext.GetEntity();
    
            // Assert
            Assert.False(AWSXRayRecorder.IsLambda());
            Assert.False(segmentCold.IsAnnotationsAdded);
            Assert.Empty(segmentCold.Annotations);
            Assert.False(segmentCold.IsSubsegmentsAdded);
            Assert.False(segmentCold.IsMetadataAdded);

            AWSXRayRecorder.Instance.EndSegment();
        }
    }
    
    [Collection("Sequential")]
    public class TracingAttributeTest : IDisposable
    {
        private readonly Handlers.Handlers _handler;

        public TracingAttributeTest()
        {
            _handler = new Handlers.Handlers();
        }
        
        #region OnEntry Tests

        [Fact]
        public void OnEntry_WhenSegmentNameIsNull_BeginSubsegmentWithMethodName()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.Equal("## Handle", subSegment.Name);
        }

        [Fact]
        public void OnEntry_WhenSegmentNameHasValue_BeginSubsegmentWithValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");

            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.HandleWithSegmentName();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.Equal("SegmentName", subSegment.Name);
        }

        [Fact]
        public void OnEntry_WhenNamespaceIsNull_SetNamespaceWithService()
        {
            // Arrange
            var serviceName = "POWERTOOLS";
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", serviceName);

            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.Equal(serviceName, subSegment.Namespace);
        }

        [Fact]
        public void OnEntry_WhenNamespaceHasValue_SetNamespaceWithValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");

            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.HandleWithNamespace();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.Equal("Namespace Defined", subSegment.Namespace);
        }

        #endregion

        #region OnSuccess Tests

        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsTrue_CapturesResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));

            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("Handle response", metadata.Keys.Cast<string>().First());
            var handlerResponse = metadata.Values.Cast<string[]>().First();
            Assert.Equal("A", handlerResponse[0]);
            Assert.Equal("B", handlerResponse[1]);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsFalse_DoesNotCaptureResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "false");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.Handle();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.False(subSegment.IsMetadataAdded);
            Assert.Empty(subSegment.Metadata);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsResponse_CapturesResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.HandleWithCaptureModeResponse();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));

            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("HandleWithCaptureModeResponse response", metadata.Keys.Cast<string>().First());
            var handlerResponse = metadata.Values.Cast<string[]>().First();
            Assert.Equal("A", handlerResponse[0]);
            Assert.Equal("B", handlerResponse[1]);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsResponseAndError_CapturesResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.HandleWithCaptureModeResponseAndError();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));

            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("HandleWithCaptureModeResponseAndError response", metadata.Keys.Cast<string>().First());
            var handlerResponse = metadata.Values.Cast<string[]>().First();
            Assert.Equal("A", handlerResponse[0]);
            Assert.Equal("B", handlerResponse[1]);
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsError_DoesNotCaptureResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.HandleWithCaptureModeError();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.False(subSegment.IsMetadataAdded); // does not add metadata
        }

        [Fact]
        public void OnSuccess_WhenTracerCaptureModeIsDisabled_DoesNotCaptureResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _handler.HandleWithCaptureModeDisabled();
            var subSegment = segment.Subsegments[0];

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.False(subSegment.IsMetadataAdded); // does not add metadata
        }

        #endregion

        #region OnException Tests

        [Fact]
        public void OnException_WhenTracerCaptureErrorEnvironmentVariableIsTrue_CapturesError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", "true");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            var exception = Record.Exception(() =>
            {
                _handler.HandleThrowsException("My Exception");
            });
            var subSegment = segment.Subsegments[0];
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));
            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("HandleThrowsException error", metadata.Keys.Cast<string>().First());
            var handlerErrorMessage = metadata.Values.Cast<string>().First();
            Assert.Contains(handlerErrorMessage, GetException(exception));
        }

        [Fact]
        public void OnException_WhenTracerCaptureErrorEnvironmentVariableIsFalse_DoesNotCaptureError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", "false");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            var exception = Record.Exception(() =>
            {
                _handler.HandleThrowsException("My Exception");
            });
            var subSegment = segment.Subsegments[0];
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.False(subSegment.IsMetadataAdded); // no metadata for errors added
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsError_CapturesError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeError(true);
            });
            var subSegment = segment.Subsegments[0];
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));
            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("HandleWithCaptureModeError error", metadata.Keys.Cast<string>().First());
            var handlerErrorMessage = metadata.Values.Cast<string>().First();
            Assert.Contains(handlerErrorMessage, GetException(exception));
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponseAndError_CapturesError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeResponseAndError(true);
            });
            var subSegment = segment.Subsegments[0];
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));
            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("HandleWithCaptureModeResponseAndError error", metadata.Keys.Cast<string>().First());
            var handlerErrorMessage = metadata.Values.Cast<string>().First();
            Assert.Contains(handlerErrorMessage, GetException(exception));
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponse_DoesNotCaptureError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeResponse(true);
            });
            var subSegment = segment.Subsegments[0];
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.False(subSegment.IsMetadataAdded); // no metadata for errors added
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsDisabled_DoesNotCaptureError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            
            // Act
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeDisabled(true);
            });
            var subSegment = segment.Subsegments[0];
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.False(subSegment.IsMetadataAdded); // no metadata for errors added
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
            
            AWSXRayRecorder.Instance.BeginSegment("foo");
            
            // Act
            _handler.Handle();
            
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            
            // Assert
            Assert.True(segment.IsInProgress);
            Assert.False(segment.IsSubsegmentsAdded);
            Assert.False(segment.IsAnnotationsAdded);
        }

        #endregion

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "");
            TracingAspect.ResetForTest();
        }
    }
}