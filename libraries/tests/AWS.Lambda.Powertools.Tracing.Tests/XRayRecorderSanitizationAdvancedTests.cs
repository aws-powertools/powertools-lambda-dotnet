using System;
using System.Collections.Generic;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("TracingTests")]
public class XRayRecorderSanitizationAdvancedTests
{
    private readonly IAWSXRayRecorder _mockAwsXRayRecorder;
    private readonly IPowertoolsConfigurations _mockConfigurations;
    private readonly XRayRecorder _xrayRecorder;

    public XRayRecorderSanitizationAdvancedTests()
    {
        _mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        _mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        _mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        _xrayRecorder = new XRayRecorder(_mockAwsXRayRecorder, _mockConfigurations);
    }

    [Fact]
    public void EndSubsegment_WithSerializationError_TriggersRecovery()
    {
        // Arrange
        var jsonException = new Exception("LitJson serialization failed");
        _mockAwsXRayRecorder.When(x => x.EndSubsegment()).Do(x => throw jsonException);

        // Act & Assert - Should not throw, should handle gracefully
        _xrayRecorder.EndSubsegment();

        // Verify recovery was attempted
        _mockAwsXRayRecorder.Received().TraceContext.ClearEntity();
        _mockAwsXRayRecorder.Received().BeginSubsegment("Tracing_Sanitized");
    }

    [Fact]
    public void EndSubsegment_WithNonSerializationError_UsesOriginalErrorHandling()
    {
        // Arrange
        var regularException = new Exception("Regular error");
        _mockAwsXRayRecorder.When(x => x.EndSubsegment()).Do(x => throw regularException);

        // Act & Assert - Should not throw, should handle gracefully
        _xrayRecorder.EndSubsegment();

        // Verify original error handling was used
        _mockAwsXRayRecorder.Received().BeginSubsegment("Error in Tracing utility - see Exceptions tab");
        _mockAwsXRayRecorder.Received().AddException(regularException);
        _mockAwsXRayRecorder.Received().MarkError();
    }

    [Fact]
    public void AddMetadata_WithCircularReference_HandlesGracefully()
    {
        // Arrange
        var parent = new TestObjectWithReference { Name = "Parent" };
        var child = new TestObjectWithReference { Name = "Child", Parent = parent };
        parent.Child = child; // Create circular reference

        // Act & Assert - Should not throw
        _xrayRecorder.AddMetadata("test", "circular", parent);

        // Verify it was called with sanitized data
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "circular", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithDeepNesting_PreventsInfiniteRecursion()
    {
        // Arrange - Create deeply nested object
        var deepObject = CreateDeeplyNestedObject(15); // Deeper than max depth

        // Act & Assert - Should not throw
        _xrayRecorder.AddMetadata("test", "deep", deepObject);

        // Verify it was called
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "deep", Arg.Any<object>());
    }

    [Fact]
    public void AddException_WithProblematicExceptionData_SanitizesException()
    {
        // Arrange
        var problematicException = new Exception("Test exception");
        
        // Mock the underlying recorder to throw a JSON error when adding the exception
        _mockAwsXRayRecorder.When(x => x.AddException(problematicException))
            .Do(x => throw new Exception("LitJson error"));

        // Act & Assert - Should not throw, should handle gracefully
        _xrayRecorder.AddException(problematicException);

        // Verify it attempted to add the original exception and then added a sanitized one
        _mockAwsXRayRecorder.Received(1).AddException(problematicException);
        _mockAwsXRayRecorder.Received(1).AddException(Arg.Is<Exception>(ex => 
            ex.Message.Contains("[Sanitized Exception]")));
    }

    [Fact]
    public void AddMetadata_WithMixedProblematicTypes_SanitizesCorrectly()
    {
        // Arrange
        var mixedObject = new
        {
            SafeString = "safe",
            SafeInt = 42,
            ProblematicULong = 42ul,
            ProblematicGuid = Guid.NewGuid(),
            ProblematicDateTime = DateTime.Now,
            NestedObject = new
            {
                NestedUInt = 42u,
                NestedTimeSpan = TimeSpan.FromMinutes(5)
            },
            ArrayWithMixed = new object[] { "safe", 42ul, Guid.NewGuid() }
        };

        // Act
        _xrayRecorder.AddMetadata("test", "mixed", mixedObject);

        // Assert - Verify the call was made (sanitization happens internally)
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "mixed", Arg.Any<object>());
    }

    private static object CreateDeeplyNestedObject(int depth)
    {
        if (depth <= 0)
            return "leaf";

        return new
        {
            Level = depth,
            Nested = CreateDeeplyNestedObject(depth - 1),
            ProblematicValue = 42ul // Add problematic type at each level
        };
    }

    public class TestObjectWithReference
    {
        public string Name { get; set; }
        public TestObjectWithReference Parent { get; set; }
        public TestObjectWithReference Child { get; set; }
    }
}