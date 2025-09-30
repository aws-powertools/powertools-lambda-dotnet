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

    // Individual type tests consolidated into comprehensive tests in XRayRecorderSanitizationAdvancedTests.cs

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