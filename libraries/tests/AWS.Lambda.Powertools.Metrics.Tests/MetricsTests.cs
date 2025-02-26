using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using System;
using System.Collections.Generic;
using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

[Collection("Sequential")]
public class MetricsTests
{
    [Fact]
    public void Metrics_Set_Execution_Environment_Context()
    {
        // Arrange
        Metrics.ResetForTest();
        var assemblyName = "AWS.Lambda.Powertools.Metrics";
        var assemblyVersion = "1.0.0";

        var env = Substitute.For<IPowertoolsEnvironment>();
        env.GetAssemblyName(Arg.Any<Metrics>()).Returns(assemblyName);
        env.GetAssemblyVersion(Arg.Any<Metrics>()).Returns(assemblyVersion);

        var conf = new PowertoolsConfigurations(new SystemWrapper(env));

        var metrics = new Metrics(conf);

        // Assert
        env.Received(1).SetEnvironmentVariable(
            "AWS_EXECUTION_ENV", $"{Constants.FeatureContextIdentifier}/Metrics/{assemblyVersion}"
        );

        env.Received(1).GetEnvironmentVariable("AWS_EXECUTION_ENV");
    }

    [Fact]
    public void Before_With_Null_DefaultDimensions_Should_Not_Throw()
    {
        // Arrange
        MetricsAspect.ResetForTest();
        var metricsMock = Substitute.For<IMetrics>();
        var optionsMock = new MetricsOptions
        {
            CaptureColdStart = true,
            DefaultDimensions = null
        };
        metricsMock.Options.Returns(optionsMock);
        Metrics.UseMetricsForTests(metricsMock);

        var metricsAspect = new MetricsAspect();
        var method = typeof(MetricsTests).GetMethod(nameof(TestMethod));
        var trigger = new MetricsAttribute();

        // Act
        metricsAspect.Before(
            this,
            "TestMethod",
            new object[] { new TestLambdaContext() },
            typeof(MetricsTests),
            method,
            typeof(void),
            new Attribute[] { trigger }
        );

        // Assert
        metricsMock.Received(1).PushSingleMetric(
            "ColdStart",
            1.0,
            MetricUnit.Count,
            Arg.Any<string>(),
            Arg.Any<string>(),
            null
        );
    }

    [Fact]
    public void Before_When_CaptureStartNotSet_Should_Not_Push_Metrics()
    {
        // Arrange
        MetricsAspect.ResetForTest();
        var metricsMock = Substitute.For<IMetrics>();
        var optionsMock = new MetricsOptions
        {
            CaptureColdStart = null
        };
        metricsMock.Options.Returns(optionsMock);
        Metrics.UseMetricsForTests(metricsMock);

        var metricsAspect = new MetricsAspect();
        var method = typeof(MetricsTests).GetMethod(nameof(TestMethod));
        var trigger = new MetricsAttribute();

        // Act
        metricsAspect.Before(
            this,
            "TestMethod",
            new object[] { new TestLambdaContext() },
            typeof(MetricsTests),
            method,
            typeof(void),
            new Attribute[] { trigger }
        );

        // Assert
        metricsMock.DidNotReceive().PushSingleMetric(
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<MetricUnit>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>()
        );
    }

    [Fact]
    public void Before_When_RaiseOnEmptyMetricsNotSet_Should_Configure_Null()
    {
        // Arrange
        MetricsAspect.ResetForTest();
        var method = typeof(MetricsTests).GetMethod(nameof(TestMethod));
        var trigger = new MetricsAttribute();

        var metricsAspect = new MetricsAspect();

        // Act
        metricsAspect.Before(
            this,
            "TestMethod",
            new object[] { new TestLambdaContext() },
            typeof(MetricsTests),
            method,
            typeof(void),
            new Attribute[] { trigger }
        );

        // Assert
        var metrics = Metrics.Instance;
        Assert.False(trigger.IsRaiseOnEmptyMetricsSet);
        Assert.False(metrics.Options.RaiseOnEmptyMetrics);
    }

    // Helper method for the tests
    internal void TestMethod(ILambdaContext context)
    {
    }
    
    [Fact]
    public void When_Constructor_With_Namespace_And_Service_Should_Set_Both()
    {
        // Arrange
        var metricsMock = Substitute.For<IMetrics>();
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();

        // Act
        var metrics = new Metrics(powertoolsConfigMock, "TestNamespace", "TestService");

        // Assert
        Assert.Equal("TestNamespace", metrics.GetNamespace());
        Assert.Equal("TestService", metrics.Options.Service);
    }

    [Fact]
    public void When_Constructor_With_Null_Namespace_And_Service_Should_Not_Set()
    {
        // Arrange
        var metricsMock = Substitute.For<IMetrics>();
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        powertoolsConfigMock.MetricsNamespace.Returns((string)null);
        powertoolsConfigMock.Service.Returns("service_undefined");

        // Act
        var metrics = new Metrics(powertoolsConfigMock, null, null);

        // Assert
        Assert.Null(metrics.GetNamespace());
        Assert.Null(metrics.Options.Service);
    }
    
    [Fact]
    public void When_AddMetric_With_EmptyKey_Should_ThrowArgumentNullException()
    {
        // Arrange
        var metricsMock = Substitute.For<IMetrics>();
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        IMetrics metrics = new Metrics(powertoolsConfigMock);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => metrics.AddMetric("", 1.0));
        Assert.Equal("key", exception.ParamName);
        Assert.Contains("'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void When_AddMetric_With_InvalidKey_Should_ThrowArgumentNullException(string key)
    {
        // Arrange
        // var metricsMock = Substitute.For<IMetrics>();
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        IMetrics metrics = new Metrics(powertoolsConfigMock);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => metrics.AddMetric(key, 1.0));
        Assert.Equal("key", exception.ParamName);
        Assert.Contains("'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.", exception.Message);
    }
    
    [Fact]
    public void When_SetDefaultDimensions_With_InvalidKeyOrValue_Should_ThrowArgumentNullException()
    {
        // Arrange
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        IMetrics metrics = new Metrics(powertoolsConfigMock);
    
        var invalidDimensions = new Dictionary<string, string>
        {
            { "", "value" }, // empty key
            { "key", "" },  // empty value
            { " ", "value" }, // whitespace key
            { "key1", " " }, // whitespace value
            { "key2", null }  // null value
        };

        // Act & Assert
        foreach (var dimension in invalidDimensions)
        {
            var dimensions = new Dictionary<string, string> { { dimension.Key, dimension.Value } };
            var exception = Assert.Throws<ArgumentNullException>(() => metrics.SetDefaultDimensions(dimensions));
            Assert.Equal("Key", exception.ParamName);
            Assert.Contains("'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty values are not allowed.", exception.Message);
        }
    }
    
    [Fact]
    public void When_PushSingleMetric_With_EmptyName_Should_ThrowArgumentNullException()
    {
        // Arrange
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        IMetrics metrics = new Metrics(powertoolsConfigMock);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => metrics.PushSingleMetric("", 1.0, MetricUnit.Count));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains("'PushSingleMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void When_PushSingleMetric_With_InvalidName_Should_ThrowArgumentNullException(string name)
    {
        // Arrange
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        IMetrics metrics = new Metrics(powertoolsConfigMock);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => metrics.PushSingleMetric(name, 1.0, MetricUnit.Count));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains("'PushSingleMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.", exception.Message);
    }
}