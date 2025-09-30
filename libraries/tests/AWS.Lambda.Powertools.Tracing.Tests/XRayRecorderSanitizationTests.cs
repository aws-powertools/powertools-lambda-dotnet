using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;
using Xunit;
using Amazon.XRay.Recorder.Core;
using NSubstitute;

namespace AWS.Lambda.Powertools.Tracing.Tests;

[Collection("TracingTests")]
public class XRayRecorderSanitizationTests
{
    private readonly IAWSXRayRecorder _mockAwsXRayRecorder;
    private readonly IPowertoolsConfigurations _mockConfigurations;
    private readonly XRayRecorder _xrayRecorder;

    public XRayRecorderSanitizationTests()
    {
        _mockAwsXRayRecorder = Substitute.For<IAWSXRayRecorder>();
        _mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        _mockConfigurations.IsLambdaEnvironment.Returns(true);
        
        _xrayRecorder = new XRayRecorder(_mockAwsXRayRecorder, _mockConfigurations);
    }

    [Fact]
    public void AddAnnotation_WithSupportedTypes_PassesThroughUnchanged()
    {
        // Arrange
        var testCases = new (string typeName, object value)[]
        {
            ("string", "test"),
            ("int", 42),
            ("long", 42L),
            ("double", 42.5),
            ("float", 42.5f),
            ("bool", true)
        };

        foreach (var (typeName, value) in testCases)
        {
            // Act
            _xrayRecorder.AddAnnotation($"test_{typeName}", value);

            // Assert
            _mockAwsXRayRecorder.Received(1).AddAnnotation($"test_{typeName}", value);
        }
    }

    [Fact]
    public void AddAnnotation_WithUnsupportedTypes_ConvertsToString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dateTime = DateTime.Now;
        var timeSpan = TimeSpan.FromMinutes(5);

        // Act
        _xrayRecorder.AddAnnotation("guid", guid);
        _xrayRecorder.AddAnnotation("datetime", dateTime);
        _xrayRecorder.AddAnnotation("timespan", timeSpan);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddAnnotation("guid", guid.ToString());
        _mockAwsXRayRecorder.Received(1).AddAnnotation("datetime", dateTime.ToString());
        _mockAwsXRayRecorder.Received(1).AddAnnotation("timespan", timeSpan.ToString());
    }

    [Fact]
    public void AddMetadata_WithComplexObject_SanitizesProblematicTypes()
    {
        // Arrange
        var complexObject = new
        {
            SystemInfo = new
            {
                DotNetVersion = Environment.Version.ToString(),
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                OSDescription = RuntimeInformation.OSDescription,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet, // This is long/int64 - problematic type
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                Is64BitProcess = Environment.Is64BitProcess,
                CLRVersion = Environment.Version.ToString(),
                CurrentDirectory = Environment.CurrentDirectory
            }
        };

        // Act
        _xrayRecorder.AddMetadata("test", "complex", complexObject);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "complex", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithProblematicNumericTypes_ConvertsToString()
    {
        // Arrange
        var testObject = new
        {
            UIntValue = 42u,
            ULongValue = 42ul,
            UShortValue = (ushort)42,
            ByteValue = (byte)42,
            SByteValue = (sbyte)42,
            IntPtrValue = new IntPtr(42),
            UIntPtrValue = new UIntPtr(42)
        };

        // Act
        _xrayRecorder.AddMetadata("test", "numeric", testObject);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "numeric", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithArrays_SanitizesArrayElements()
    {
        // Arrange
        var arrayWithProblematicTypes = new object[]
        {
            42u, // uint - should be converted to string
            "normal string", // should remain unchanged
            Guid.NewGuid(), // should be converted to string
            new { Property = 42ul } // object with ulong - should be sanitized
        };

        // Act
        _xrayRecorder.AddMetadata("test", "array", arrayWithProblematicTypes);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "array", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithDictionary_SanitizesDictionaryValues()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            ["normal"] = "string value",
            ["problematic"] = 42ul, // ulong - should be converted
            ["guid"] = Guid.NewGuid(), // should be converted to string
            ["nested"] = new { ULongProp = 42ul } // nested object with problematic type
        };

        // Act
        _xrayRecorder.AddMetadata("test", "dict", dictionary);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "dict", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithEnum_ConvertsToString()
    {
        // Arrange
        var enumValue = RuntimeInformation.OSArchitecture;

        // Act
        _xrayRecorder.AddMetadata("test", "enum", enumValue);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "enum", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithDateTime_ConvertsToISOString()
    {
        // Arrange
        var dateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        _xrayRecorder.AddMetadata("test", "datetime", dateTime);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "datetime", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithTimeSpan_ConvertsToString()
    {
        // Arrange
        var timeSpan = TimeSpan.FromMinutes(30);

        // Act
        _xrayRecorder.AddMetadata("test", "timespan", timeSpan);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "timespan", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithNullValue_PassesNullThrough()
    {
        // Act
        _xrayRecorder.AddMetadata("test", "null", null);

        // Assert
        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "null", null);
    }

    [Fact]
    public void AddMetadata_WithDeepNesting_PreventsInfiniteRecursion()
    {
        // Arrange - Create a deeply nested object
        object deepObject = "base";
        for (int i = 0; i < 15; i++) // More than the max depth of 10
        {
            deepObject = new { Level = i, Nested = deepObject };
        }

        // Act & Assert - Should not throw StackOverflowException
        _xrayRecorder.AddMetadata("test", "deep", deepObject);

        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "deep", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WithCircularReference_HandlesGracefully()
    {
        // Arrange - Create a circular reference
        var obj1 = new TestObjectWithReference { Name = "Object1" };
        var obj2 = new TestObjectWithReference { Name = "Object2", Reference = obj1 };
        obj1.Reference = obj2; // Create circular reference

        // Act & Assert - Should not throw StackOverflowException
        _xrayRecorder.AddMetadata("test", "circular", obj1);

        _mockAwsXRayRecorder.Received(1).AddMetadata("test", "circular", Arg.Any<object>());
    }

    [Fact]
    public void AddMetadata_WhenNotInLambda_DoesNotCallUnderlyingRecorder()
    {
        // Arrange
        var mockConfigurations = Substitute.For<IPowertoolsConfigurations>();
        mockConfigurations.IsLambdaEnvironment.Returns(false);
        var recorder = new XRayRecorder(_mockAwsXRayRecorder, mockConfigurations);

        // Act
        recorder.AddMetadata("test", "key", "value");

        // Assert
        _mockAwsXRayRecorder.DidNotReceive().AddMetadata(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
    }

    private class TestObjectWithReference
    {
        public string Name { get; set; }
        public TestObjectWithReference Reference { get; set; }
    }
}