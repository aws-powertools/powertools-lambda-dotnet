using System;
using System.Linq;
using System.Text;
using Amazon.Lambda.TestUtilities;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common.Core;
using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.Powertools.Tracing.Tests
{
    public class TracingAttributeColdStartTest : TracingTestBase
    {
        private readonly HandlerFunctions _handler;

        public TracingAttributeColdStartTest()
        {
            _handler = new HandlerFunctions();
        }
        
        [Fact]
        public void OnEntry_WhenFirstCall_CapturesColdStart()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();

            // Act
            // Cold Start Execution
            // Start segment
            var segmentCold = GetOrCreateSegment();
            _handler.Handle();

            var subSegmentCold = segmentCold.Subsegments.Count > 0 ? segmentCold.Subsegments[0] : null;

            // Warm Start Execution - create a new segment to simulate new invocation
            // Clear just the AsyncLocal value to simulate new invocation in same container
            LambdaLifecycleTracker.Reset(resetContainer: false);
            
            // Create a new segment for warm start
            var segmentWarm = new Segment("TestLambdaFunction-Warm");
            segmentWarm.SetStartTimeToNow();
            AWSXRayRecorder.Instance.TraceContext.SetEntity(segmentWarm);
            
            _handler.Handle();
            var subSegmentWarm = segmentWarm.Subsegments.Count > 0 ? segmentWarm.Subsegments[0] : null;

            // Assert
            // Cold
            Assert.True(segmentCold.IsSubsegmentsAdded);
            Assert.Single(segmentCold.Subsegments);
            Assert.True(subSegmentCold.IsAnnotationsAdded);
            Assert.Equal(2, subSegmentCold.Annotations.Count());
            Assert.True((bool)subSegmentCold.Annotations.Single(x => x.Key == "ColdStart").Value);
            Assert.Equal("POWERTOOLS", subSegmentCold.Annotations.Single(x => x.Key == "Service").Value);

            // Warm
            Assert.True(segmentWarm.IsSubsegmentsAdded);
            Assert.Single(segmentWarm.Subsegments);
            Assert.True(subSegmentWarm.IsAnnotationsAdded);
            Assert.Equal(2, subSegmentWarm.Annotations.Count());
            Assert.False((bool)subSegmentWarm.Annotations.Single(x => x.Key == "ColdStart").Value);
            Assert.Equal("POWERTOOLS", subSegmentWarm.Annotations.Single(x => x.Key == "Service").Value);
        }

        [Fact]
        public void OnEntry_WhenFirstCall_And_Service_Not_Set_CapturesColdStart()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            SetupLambdaEnvironment();

            // Act
            // Cold Start Execution
            // Start segment
            var segmentCold = GetOrCreateSegment();
            _handler.Handle();
            var subSegmentCold = segmentCold.Subsegments.Count > 0 ? segmentCold.Subsegments[0] : null;

            // Warm Start Execution - create a new segment to simulate new invocation
            // Clear just the AsyncLocal value to simulate new invocation in same container
            LambdaLifecycleTracker.Reset(resetContainer: false);
            
            // Create a new segment for warm start
            var segmentWarm = new Segment("TestLambdaFunction-Warm");
            segmentWarm.SetStartTimeToNow();
            AWSXRayRecorder.Instance.TraceContext.SetEntity(segmentWarm);
            
            _handler.Handle();
            var subSegmentWarm = segmentWarm.Subsegments.Count > 0 ? segmentWarm.Subsegments[0] : null;

            // Assert
            // Cold
            Assert.True(segmentCold.IsSubsegmentsAdded);
            Assert.Single(segmentCold.Subsegments);
            Assert.True(subSegmentCold.IsAnnotationsAdded);
            Assert.Single(subSegmentCold.Annotations);
            Assert.True((bool)subSegmentCold.Annotations.Single(x => x.Key == "ColdStart").Value);

            // Warm
            Assert.True(segmentWarm.IsSubsegmentsAdded);
            Assert.Single(segmentWarm.Subsegments);
            Assert.True(subSegmentWarm.IsAnnotationsAdded);
            Assert.Single(subSegmentWarm.Annotations);
            Assert.False((bool)subSegmentWarm.Annotations.Single(x => x.Key == "ColdStart").Value);
        }

        public override void Dispose()
        {
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_ERROR", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "");
            TracingAspect.ResetForTest();
        }
    }

    public class TracingAttributeDisableTest : TracingTestBase
    {
        private readonly HandlerFunctions _handler;

        public TracingAttributeDisableTest()
        {
            _handler = new HandlerFunctions();
        }
        
        [Fact]
        public void Tracing_WhenTracerDisabled_DisablesTracing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "true");
            SetupLambdaEnvironment();

            // Act
            // Cold Start Execution
            // Use the existing segment from TracingTestBase
            var segmentCold = GetOrCreateSegment();
            _handler.Handle();

            // Warm Start Execution
            // Since tracing is disabled, the same segment should be used
            var segmentWarm = GetOrCreateSegment();
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

        public override void Dispose()
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

    [Collection("TracingTests")]
    public class TracingAttributeLambdaEnvironmentTest
    {
        private readonly HandlerFunctions _handler;
    
        public TracingAttributeLambdaEnvironmentTest()
        {
            _handler = new HandlerFunctions();
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
    
    public class TracingAttributeTest : TracingTestBase
    {
        private readonly HandlerFunctions _handler;

        public TracingAttributeTest()
        {
            _handler = new HandlerFunctions();
        }
        
        #region OnEntry Tests

        [Fact]
        public void OnEntry_WhenSegmentNameIsNull_BeginSubsegmentWithMethodName()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();
            _handler.Handle();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.NotNull(subSegment);
            Assert.Equal("## Handle", subSegment.Name);
        }

        [Fact]
        public void OnEntry_WhenSegmentNameHasValue_BeginSubsegmentWithValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();

            // Act
            var segment = GetOrCreateSegment();
            _handler.HandleWithSegmentName();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.NotNull(subSegment);
            Assert.Equal("SegmentName", subSegment.Name);
        }
        
        [Fact]
        public void OnEntry_WhenSegmentName_Is_Unsupported()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();

            // Act
            var segment = GetOrCreateSegment();
            _handler.HandleWithInvalidSegmentName();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            var childSegment = subSegment?.Subsegments?.Count > 0 ? subSegment.Subsegments[0] : null;
            
            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.NotNull(subSegment);
            Assert.True(subSegment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.Single(subSegment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.Equal("## Maing__Handler0_0", subSegment.Name);
            Assert.NotNull(childSegment);
            Assert.Equal("Inval#id  Segment", childSegment.Name);
        }

        [Fact]
        public void OnEntry_WhenNamespaceIsNull_SetNamespaceWithService()
        {
            // Arrange
            var serviceName = "POWERTOOLS";
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", serviceName);
            SetupLambdaEnvironment();

            // Act
            var segment = GetOrCreateSegment();
            _handler.Handle();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.Equal(serviceName, subSegment.Namespace);
        }

        [Fact]
        public void OnEntry_WhenNamespaceHasValue_SetNamespaceWithValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();

            // Act
            var segment = GetOrCreateSegment();
            _handler.HandleWithNamespace();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.Equal("Namespace Defined", subSegment.Namespace);
        }

        #endregion

        #region OnSuccess Tests

        
        [Fact]
        public void OnSuccess_When_NotSet_Defaults_CapturesResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();
            _handler.Handle();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.NotNull(subSegment);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));

            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("Handle response", metadata.Keys.Cast<string>().First());
            var handlerResponse = metadata.Values.Cast<string[]>().First();
            Assert.Equal("A", handlerResponse[0]);
            Assert.Equal("B", handlerResponse[1]);
        }
        
        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsTrue_CapturesResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();
            _handler.Handle();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
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
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();
            _handler.Handle();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.False(subSegment.IsMetadataAdded);
            Assert.Empty(subSegment.Metadata);
        }

        [Theory]
        [InlineData(TracingCaptureMode.Response, true)]
        [InlineData(TracingCaptureMode.ResponseAndError, true)]
        [InlineData(TracingCaptureMode.Error, false)]
        [InlineData(TracingCaptureMode.Disabled, false)]
        public void OnSuccess_WithDifferentCaptureModes_CapturesResponseCorrectly(TracingCaptureMode mode, bool shouldCaptureResponse)
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();
            switch (mode)
            {
                case TracingCaptureMode.Response:
                    _handler.HandleWithCaptureModeResponse();
                    break;
                case TracingCaptureMode.ResponseAndError:
                    _handler.HandleWithCaptureModeResponseAndError();
                    break;
                case TracingCaptureMode.Error:
                    _handler.HandleWithCaptureModeError();
                    break;
                case TracingCaptureMode.Disabled:
                    _handler.HandleWithCaptureModeDisabled();
                    break;
            }
            
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.Equal(shouldCaptureResponse, subSegment.IsMetadataAdded);
            
            if (shouldCaptureResponse)
            {
                Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));
                var metadata = subSegment.Metadata["POWERTOOLS"];
                var handlerResponse = metadata.Values.Cast<string[]>().First();
                Assert.Equal("A", handlerResponse[0]);
                Assert.Equal("B", handlerResponse[1]);
            }
        }
        
        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsFalse_ButDecoratorCapturesResponse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "false");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();
            _handler.DecoratedHandlerCaptureResponse();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));

            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("DecoratedHandlerCaptureResponse response", metadata.Keys.Cast<string>().First());
            var handlerResponse = metadata.Values.Cast<string>().First();
            Assert.Equal("Hello World", handlerResponse);

            var decoratedMethodSegmentDisabled = subSegment.Subsegments[0];
            Assert.False(decoratedMethodSegmentDisabled.IsMetadataAdded);
            Assert.Equal("## DecoratedMethodCaptureDisabled", decoratedMethodSegmentDisabled.Name);
            
            var decoratedMethodSegmentEnabled = decoratedMethodSegmentDisabled.Subsegments[0];
            Assert.True(decoratedMethodSegmentEnabled.IsMetadataAdded);
            
            var decoratedMethodSegmentEnabledMetadata = decoratedMethodSegmentEnabled.Metadata["POWERTOOLS"];
            var decoratedMethodSegmentEnabledResponse = decoratedMethodSegmentEnabledMetadata.Values.Cast<string>().First();
            Assert.Equal("DecoratedMethod Enabled", decoratedMethodSegmentEnabledResponse);
            Assert.Equal("## DecoratedMethodCaptureEnabled", decoratedMethodSegmentEnabled.Name);
        }
        
        [Fact]
        public void OnSuccess_WhenTracerCaptureResponseEnvironmentVariableIsTrue_ButDecoratorCapturesResponseDisabled()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();
            _handler.DecoratedMethodCaptureDisabled();
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;

            // Assert
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.False(subSegment.IsMetadataAdded);

            var decoratedMethodSegmentEnabled = subSegment.Subsegments[0];
            var metadata = decoratedMethodSegmentEnabled.Metadata["POWERTOOLS"];
            Assert.True(decoratedMethodSegmentEnabled.IsMetadataAdded);
            var decoratedMethodSegmentEnabledResponse = metadata.Values.Cast<string>().First();
            Assert.Equal("DecoratedMethod Enabled", decoratedMethodSegmentEnabledResponse);
            Assert.Equal("## DecoratedMethodCaptureEnabled", decoratedMethodSegmentEnabled.Name);
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
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleThrowsException("My Exception");
            });
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
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
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleThrowsException("My Exception");
            });
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.False(subSegment.IsMetadataAdded); // no metadata for errors added
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsError_CapturesError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeError(true);
            });
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));
            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("HandleWithCaptureModeError error", metadata.Keys.Cast<string>().First());
            var handlerErrorMessage = metadata.Values.Cast<string>().First();
            Assert.Contains(handlerErrorMessage, GetException(exception));
        }
        
        [Fact]
        public void OnException_WhenTracerCaptureModeIsError_CapturesError_Inner_Exception()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeErrorInner(true);
            });
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.True(subSegment.IsMetadataAdded);
            Assert.True(subSegment.Metadata.ContainsKey("POWERTOOLS"));
            var metadata = subSegment.Metadata["POWERTOOLS"];
            Assert.Equal("HandleWithCaptureModeErrorInner error", metadata.Keys.Cast<string>().First());
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Inner Exception!!",exception.InnerException.Message);
        }
        
        [Fact]
        public void OnException_When_Tracing_Disabled_Does_Not_CapturesError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "true");
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeError(true);
            });
            
            // Assert
            Assert.NotNull(exception);
            Assert.False(segment.IsSubsegmentsAdded);
            Assert.Empty(segment.Subsegments);
            Assert.False(segment.IsMetadataAdded);
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsResponseAndError_CapturesError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeResponseAndError(true);
            });
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
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
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeResponse(true);
            });
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
            Assert.False(subSegment.IsMetadataAdded); // no metadata for errors added
        }

        [Fact]
        public void OnException_WhenTracerCaptureModeIsDisabled_DoesNotCaptureError()
        {
            // Arrange
            Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "AWS");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "POWERTOOLS");
            SetupLambdaEnvironment();
            
            // Act
            var segment = GetOrCreateSegment();

            var exception = Record.Exception(() =>
            {
                _handler.HandleWithCaptureModeDisabled(true);
            });
            var subSegment = segment.Subsegments.Count > 0 ? segment.Subsegments[0] : null;
            
            // Assert
            Assert.NotNull(exception);
            Assert.True(segment.IsSubsegmentsAdded);
            Assert.Single(segment.Subsegments);
            Assert.NotNull(subSegment);
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
            
            var context = new TestLambdaContext
            {
                FunctionName = "FullExampleLambda",
                FunctionVersion = "1",
                MemoryLimitInMB = 215,
                AwsRequestId = Guid.NewGuid().ToString("D"),
                LogGroupName = "log-group",
                LogStreamName = "log-stream",
                InvokedFunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:FullExampleLambda",
                RemainingTime = TimeSpan.FromMinutes(5),
            };
            // Act
            _handler.HandleUnsupported(context);
            
            var segment = GetOrCreateSegment();
            
            // Assert
            Assert.True(segment.IsInProgress);
            Assert.False(segment.IsSubsegmentsAdded);
            Assert.False(segment.IsAnnotationsAdded);
        }

        #endregion

        public override void Dispose()
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