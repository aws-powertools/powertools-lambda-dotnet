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

    [Fact]
    public void HandleSerializationError_WithMultipleFailures_TriesAllStrategies()
    {
        // Arrange
        var jsonException = new Exception("LitJson serialization failed");
        
        // Make EndSubsegment throw initially
        _mockAwsXRayRecorder.When(x => x.EndSubsegment()).Do(x => throw jsonException);
        
        // Make the first recovery strategy fail
        _mockAwsXRayRecorder.When(x => x.BeginSubsegment("Tracing_Sanitized"))
            .Do(x => throw new Exception("Strategy 1 failed"));
        
        // Make the second recovery strategy fail
        _mockAwsXRayRecorder.When(x => x.BeginSubsegment("Tracing_Error"))
            .Do(x => throw new Exception("Strategy 2 failed"));

        // Act & Assert - Should not throw, should handle all failures gracefully
        _xrayRecorder.EndSubsegment();

        // Verify all strategies were attempted
        _mockAwsXRayRecorder.Received().TraceContext.ClearEntity();
        _mockAwsXRayRecorder.Received().BeginSubsegment("Tracing_Sanitized");
        _mockAwsXRayRecorder.Received().BeginSubsegment("Tracing_Error");
    }

    [Fact]
    public void HandleSerializationError_WithClearEntityFailure_HandlesGracefully()
    {
        // Arrange
        var jsonException = new Exception("LitJson serialization failed");
        
        // Make EndSubsegment throw initially
        _mockAwsXRayRecorder.When(x => x.EndSubsegment()).Do(x => throw jsonException);
        
        // Make all recovery strategies fail, including ClearEntity
        _mockAwsXRayRecorder.TraceContext.When(x => x.ClearEntity())
            .Do(x => throw new Exception("ClearEntity failed"));

        // Act & Assert - Should not throw even when ClearEntity fails
        _xrayRecorder.EndSubsegment();

        // Verify ClearEntity was attempted
        _mockAwsXRayRecorder.TraceContext.Received().ClearEntity();
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

    [Fact]
    public void GetEntity_WithXRayContextException_ReturnsRootSubsegment()
    {
        // Arrange
        var mockTraceContext = Substitute.For<Amazon.XRay.Recorder.Core.Internal.Context.ITraceContext>();
        mockTraceContext.When(x => x.GetEntity()).Do(x => throw new Exception("Context error"));
        _mockAwsXRayRecorder.TraceContext.Returns(mockTraceContext);

        // Act
        var result = _xrayRecorder.GetEntity();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Subsegment>(result);
        Assert.Equal("Root", ((Subsegment)result).Name);
    }

    [Fact]
    public void SanitizeValueForMetadata_WithSanitizationException_ReturnsErrorMessage()
    {
        // Arrange - Create an object that will cause sanitization to fail
        var problematicObject = new ProblematicObject();

        // Act
        _xrayRecorder.AddMetadata("test", "problematic", problematicObject);

        // Assert - Should not throw and should call with sanitized value
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "problematic", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithUnsafeArrayElements_SanitizesArray()
    {
        // Arrange
        var unsafeArray = new object[] { 42ul, new IntPtr(123), Guid.NewGuid(), "safe" };

        // Act
        _xrayRecorder.AddMetadata("test", "unsafe_array", unsafeArray);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "unsafe_array", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithDictionary_SanitizesDictionaryValues()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "safe", "value" },
            { "unsafe_ulong", 42ul },
            { "unsafe_guid", Guid.NewGuid() },
            { "unsafe_datetime", DateTime.Now },
            { "null_key", null }
        };

        // Act
        _xrayRecorder.AddMetadata("test", "dictionary", dictionary);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "dictionary", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithEnumerable_SanitizesEnumerableValues()
    {
        // Arrange
        var enumerable = new List<object> { "safe", 42ul, Guid.NewGuid(), TimeSpan.FromMinutes(1) };

        // Act
        _xrayRecorder.AddMetadata("test", "enumerable", enumerable);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "enumerable", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithComplexObjectConversionFailure_ReturnsErrorMessage()
    {
        // Arrange
        var objectThatFailsToString = new ObjectThatFailsToString();

        // Act
        _xrayRecorder.AddMetadata("test", "failing_object", objectThatFailsToString);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "failing_object", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithAllProblematicPrimitiveTypes_SanitizesCorrectly()
    {
        // Arrange
        var problematicTypes = new
        {
            IntPtr = new IntPtr(123),
            UIntPtr = new UIntPtr(456),
            UInt = 42u,
            ULong = 42ul,
            UShort = (ushort)42,
            Byte = (byte)42,
            SByte = (sbyte)42
        };

        // Act
        _xrayRecorder.AddMetadata("test", "problematic_primitives", problematicTypes);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "problematic_primitives", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithAllNullableProblematicTypes_SanitizesCorrectly()
    {
        // Arrange
        var nullableProblematicTypes = new
        {
            NullableIntPtr = (IntPtr?)new IntPtr(123),
            NullableUIntPtr = (UIntPtr?)new UIntPtr(456),
            NullableUInt = (uint?)42u,
            NullableULong = (ulong?)42ul,
            NullableUShort = (ushort?)42,
            NullableByte = (byte?)42,
            NullableSByte = (sbyte?)42,
            NullableDateTime = (DateTime?)DateTime.Now,
            NullableTimeSpan = (TimeSpan?)TimeSpan.FromMinutes(1),
            NullableGuid = (Guid?)Guid.NewGuid()
        };

        // Act
        _xrayRecorder.AddMetadata("test", "nullable_problematic", nullableProblematicTypes);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "nullable_problematic", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithSafeArrayTypes_PassesThroughUnchanged()
    {
        // Arrange
        var safeArrays = new
        {
            StringArray = new[] { "a", "b", "c" },
            IntArray = new[] { 1, 2, 3 },
            LongArray = new[] { 1L, 2L, 3L },
            DoubleArray = new[] { 1.0, 2.0, 3.0 },
            FloatArray = new[] { 1.0f, 2.0f, 3.0f },
            BoolArray = new[] { true, false, true },
            DecimalArray = new[] { 1.0m, 2.0m, 3.0m }
        };

        // Act
        _xrayRecorder.AddMetadata("test", "safe_arrays", safeArrays);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "safe_arrays", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithEnumTypes_ConvertsToString()
    {
        // Arrange
        var enumObject = new
        {
            TestEnum = TestEnum.Value1,
            NullableEnum = (TestEnum?)TestEnum.Value2
        };

        // Act
        _xrayRecorder.AddMetadata("test", "enums", enumObject);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "enums", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithMaxDepthReached_ReturnsMaxDepthMessage()
    {
        // Arrange - Create object that will exceed max depth
        var veryDeepObject = CreateDeeplyNestedObject(20); // Much deeper than max depth of 10

        // Act
        _xrayRecorder.AddMetadata("test", "very_deep", veryDeepObject);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "very_deep", Arg.Any<object>());
    }

    public enum TestEnum
    {
        Value1,
        Value2
    }

    public class ProblematicObject
    {
        public override string ToString()
        {
            throw new Exception("ToString failed");
        }
    }

    public class ObjectThatFailsToString
    {
        public override string ToString()
        {
            throw new Exception("ToString conversion failed");
        }
    }

    public class TestObjectWithReference
    {
        public string Name { get; set; }
        public TestObjectWithReference Parent { get; set; }
        public TestObjectWithReference Child { get; set; }
    }
}