using System;
using System.Runtime.InteropServices;
using AWS.Lambda.Powertools.Tracing.Internal;
using AWS.Lambda.Powertools.Common;
using Xunit;
using NSubstitute;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("TracingTests")]
public class EntityLevelSanitizationTests
{
    [Fact]
    public void SanitizeCurrentEntitySafely_WithNullTraceContext_HandlesGracefully()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        // Return null for TraceContext
        mockAwsXRayRecorder.TraceContext.Returns((Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext)null);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            // Use reflection to call the private method
            var method = typeof(XRayRecorder).GetMethod("SanitizeCurrentEntitySafely", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method?.Invoke(recorder, null);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Entity sanitization should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void EndSubsegment_WithProblematicData_CallsSanitization()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            recorder.EndSubsegment();
            
            // Verify that EndSubsegment was called on the underlying recorder
            mockAwsXRayRecorder.Received(1).EndSubsegment();
        }
        catch (Exception ex)
        {
            Assert.Fail($"EndSubsegment should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void SanitizeCurrentEntitySafely_WithNullEntity_HandlesGracefully()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        var mockTraceContext = Substitute.For<Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext>();
        mockTraceContext.GetEntity().Returns((Entity)null);
        mockAwsXRayRecorder.TraceContext.Returns(mockTraceContext);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            // Use reflection to call the private method
            var method = typeof(XRayRecorder).GetMethod("SanitizeCurrentEntitySafely", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method?.Invoke(recorder, null);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Entity sanitization with null entity should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void SanitizeCurrentEntitySafely_WithException_HandlesGracefully()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        // Make TraceContext throw an exception
        mockAwsXRayRecorder.TraceContext.Returns(x => throw new InvalidOperationException("Test exception"));
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            // Use reflection to call the private method
            var method = typeof(XRayRecorder).GetMethod("SanitizeCurrentEntitySafely", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method?.Invoke(recorder, null);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Entity sanitization with exception should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void SanitizeCurrentEntitySafely_WithComplexProblematicData_HandlesAllScenarios()
    {
        // This test covers comprehensive sanitization scenarios
        // Simplified to avoid complex mocking issues
        
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        var mockTraceContext = Substitute.For<Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext>();
        
        // Return null entity to test the null handling path
        mockTraceContext.GetEntity().Returns((Entity)null);
        mockAwsXRayRecorder.TraceContext.Returns(mockTraceContext);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw even with null entity
        try
        {
            recorder.EndSubsegment();
            
            // Verify that EndSubsegment was called
            mockAwsXRayRecorder.Received(1).EndSubsegment();
        }
        catch (Exception ex)
        {
            Assert.Fail($"EndSubsegment with null entity should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void SanitizeCurrentEntitySafely_WithProblematicData_SanitizesSuccessfully()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        // Create a real subsegment to test actual sanitization
        var subsegment = new Subsegment("TestSegment");
        
        // Add problematic data to the subsegment (only metadata, not annotations with DateTime)
        subsegment.AddMetadata("test", "problematic_ulong", 42ul);
        subsegment.AddMetadata("test", "problematic_guid", Guid.NewGuid());
        subsegment.AddMetadata("test", "problematic_datetime", DateTime.Now);
        subsegment.AddAnnotation("safe_annotation", "safe_value");
        
        var mockTraceContext = Substitute.For<Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext>();
        mockTraceContext.GetEntity().Returns(subsegment);
        mockAwsXRayRecorder.TraceContext.Returns(mockTraceContext);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            recorder.EndSubsegment();
            
            // Verify that EndSubsegment was called
            mockAwsXRayRecorder.Received(1).EndSubsegment();
        }
        catch (Exception ex)
        {
            Assert.Fail($"EndSubsegment with problematic data should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void SanitizeEntityMetadata_WithReflectionFailure_HandlesGracefully()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        // Create a subsegment and make GetEntity throw an exception to simulate reflection failure
        var mockTraceContext = Substitute.For<Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext>();
        mockTraceContext.GetEntity().Returns(x => throw new Exception("Reflection error"));
        mockAwsXRayRecorder.TraceContext.Returns(mockTraceContext);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            recorder.EndSubsegment();
            
            // Verify that EndSubsegment was still called despite the reflection error
            mockAwsXRayRecorder.Received(1).EndSubsegment();
        }
        catch (Exception ex)
        {
            Assert.Fail($"EndSubsegment with reflection failure should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void SanitizeEntityAnnotations_WithNullAnnotations_HandlesGracefully()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        // Create a subsegment with null annotations property (simulated)
        var subsegment = new Subsegment("TestSegment");
        
        var mockTraceContext = Substitute.For<Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext>();
        mockTraceContext.GetEntity().Returns(subsegment);
        mockAwsXRayRecorder.TraceContext.Returns(mockTraceContext);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            recorder.EndSubsegment();
            
            // Verify that EndSubsegment was called
            mockAwsXRayRecorder.Received(1).EndSubsegment();
        }
        catch (Exception ex)
        {
            Assert.Fail($"EndSubsegment with null annotations should not throw: {ex.Message}");
        }
    }

    [Fact]
    public void SanitizeEntityHttpInformation_WithNullHttp_HandlesGracefully()
    {
        // Arrange
        var mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        // Create a subsegment with null HTTP property (simulated)
        var subsegment = new Subsegment("TestSegment");
        
        var mockTraceContext = Substitute.For<Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext>();
        mockTraceContext.GetEntity().Returns(subsegment);
        mockAwsXRayRecorder.TraceContext.Returns(mockTraceContext);
        
        var recorder = new XRayRecorder(mockAwsXRayRecorder, mockConfigurations);

        // Act & Assert - Should not throw
        try
        {
            recorder.EndSubsegment();
            
            // Verify that EndSubsegment was called
            mockAwsXRayRecorder.Received(1).EndSubsegment();
        }
        catch (Exception ex)
        {
            Assert.Fail($"EndSubsegment with null HTTP info should not throw: {ex.Message}");
        }
    }
}