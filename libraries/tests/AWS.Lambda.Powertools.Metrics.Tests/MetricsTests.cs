using System;
using System.Collections.Generic;
using System.IO;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
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

        _ = new Metrics(conf);

        // Assert
        env.Received(1).SetEnvironmentVariable(
            "AWS_EXECUTION_ENV", $"{Constants.FeatureContextIdentifier}/Metrics/{assemblyVersion}"
        );

        env.Received(1).GetEnvironmentVariable("AWS_EXECUTION_ENV");
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

    [Fact]
    public void When_MetricsDisabled_Should_Not_AddMetric()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.MetricsDisabled.Returns(true);

        IMetrics metrics = new Metrics(conf);
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        metrics.AddMetric("test", 1.0);
        metrics.Flush();

        // Assert
        Assert.Empty(stringWriter.ToString());

        // Cleanup
        stringWriter.Dispose();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
    }

    [Fact]
    public void When_MetricsDisabled_Should_Not_PushSingleMetric()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.MetricsDisabled.Returns(true);

        IMetrics metrics = new Metrics(conf);
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        metrics.PushSingleMetric("test", 1.0, MetricUnit.Count);

        // Assert
        Assert.Empty(stringWriter.ToString());

        // Cleanup
        stringWriter.Dispose();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
    }

    // Helper method for the tests
    internal void TestMethod(ILambdaContext context)
    {
    }

    [Fact]
    public void When_Constructor_With_Null_Namespace_And_Service_Should_Not_Set()
    {
        // Arrange
        Substitute.For<IMetrics>();
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        powertoolsConfigMock.MetricsNamespace.Returns((string)null);
        powertoolsConfigMock.Service.Returns("service_undefined");

        // Act
        var metrics = new Metrics(powertoolsConfigMock);

        // Assert
        Assert.Null(metrics.GetNamespace());
        Assert.Null(metrics.Options.Service);
    }

    [Fact]
    public void When_AddMetric_With_EmptyKey_Should_ThrowArgumentNullException()
    {
        // Arrange
        Substitute.For<IMetrics>();
        var powertoolsConfigMock = Substitute.For<IPowertoolsConfigurations>();
        IMetrics metrics = new Metrics(powertoolsConfigMock);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => metrics.AddMetric("", 1.0));
        Assert.Equal("key", exception.ParamName);
        Assert.Contains("'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.",
            exception.Message);
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
        Assert.Contains("'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.",
            exception.Message);
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
            { "key", "" }, // empty value
            { " ", "value" }, // whitespace key
            { "key1", " " }, // whitespace value
            { "key2", null } // null value
        };

        // Act & Assert
        foreach (var dimension in invalidDimensions)
        {
            var dimensions = new Dictionary<string, string> { { dimension.Key, dimension.Value } };
            var exception = Assert.Throws<ArgumentNullException>(() => metrics.SetDefaultDimensions(dimensions));
            Assert.Equal("Key", exception.ParamName);
            Assert.Contains(
                "'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty values are not allowed.",
                exception.Message);
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
        Assert.Contains(
            "'PushSingleMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.",
            exception.Message);
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
        var exception =
            Assert.Throws<ArgumentNullException>(() => metrics.PushSingleMetric(name, 1.0, MetricUnit.Count));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains(
            "'PushSingleMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.",
            exception.Message);
    }


    [Fact]
    public void When_ColdStart_Should_Use_DefaultDimensions_From_Options()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "dotnet-powertools-test",
            DefaultDimensions = new Dictionary<string, string>
            {
                { "Environment", "Test" },
                { "Region", "us-east-1" }
            }
        };

        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        IMetrics metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);

        var context = new TestLambdaContext
        {
            FunctionName = "TestFunction"
        };

        // Act
        metrics.CaptureColdStartMetric(context);

        // Assert
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Environment\",\"Region\",\"FunctionName\"]]}]},\"Environment\":\"Test\",\"Region\":\"us-east-1\",\"FunctionName\":\"TestFunction\",\"ColdStart\":1}"))
        );
    }

    [Fact]
    public void When_ColdStart_And_DefaultDimensions_Is_Null_Should_Only_Add_Service_And_FunctionName()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "dotnet-powertools-test",
            DefaultDimensions = null
        };

        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        IMetrics metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);

        var context = new TestLambdaContext
        {
            FunctionName = "TestFunction"
        };

        // Act
        metrics.CaptureColdStartMetric(context);

        // Assert
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"FunctionName\"]]}]},\"FunctionName\":\"TestFunction\",\"ColdStart\":1}"))
        );
    }
    
    [Fact]
    public void Namespace_Should_Return_OptionsNamespace()
    {
        // Arrange
        Metrics.ResetForTest();
        var metricsMock = Substitute.For<IMetrics>();
        var optionsMock = new MetricsOptions
        {
            Namespace = "TestNamespace"
        };
    
        metricsMock.Options.Returns(optionsMock);
        Metrics.UseMetricsForTests(metricsMock);

        // Act
        var result = Metrics.Namespace;

        // Assert
        Assert.Equal("TestNamespace", result);
    }

    [Fact]
    public void Service_Should_Return_OptionsService()
    {
        // Arrange
        Metrics.ResetForTest();
        var metricsMock = Substitute.For<IMetrics>();
        var optionsMock = new MetricsOptions
        {
            Service = "TestService"
        };
    
        metricsMock.Options.Returns(optionsMock);
        Metrics.UseMetricsForTests(metricsMock);

        // Act
        var result = Metrics.Service;

        // Assert
        Assert.Equal("TestService", result);
    }

    [Fact]
    public void Namespace_Should_Return_Null_When_Not_Set()
    {
        // Arrange
        Metrics.ResetForTest();
        var metricsMock = Substitute.For<IMetrics>();
        var optionsMock = new MetricsOptions();
    
        metricsMock.Options.Returns(optionsMock);
        Metrics.UseMetricsForTests(metricsMock);

        // Act
        var result = Metrics.Namespace;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Service_Should_Return_Null_When_Not_Set()
    {
        // Arrange
        Metrics.ResetForTest();
        var metricsMock = Substitute.For<IMetrics>();
        var optionsMock = new MetricsOptions();
    
        metricsMock.Options.Returns(optionsMock);
        Metrics.UseMetricsForTests(metricsMock);

        // Act
        var result = Metrics.Service;

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void WithFunctionName_Should_Set_FunctionName_In_Options()
    {
        // Arrange
        var builder = new MetricsBuilder();
        var expectedFunctionName = "TestFunction";

        // Act
        var result = builder.WithFunctionName(expectedFunctionName);
        var metrics = result.Build();

        // Assert
        Assert.Equal(expectedFunctionName, metrics.Options.FunctionName);
        Assert.Same(builder, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithFunctionName_Should_Allow_NullOrEmpty_FunctionName(string functionName)
    {
        // Arrange
        var builder = new MetricsBuilder();

        // Act
        var result = builder.WithFunctionName(functionName);
        var metrics = result.Build();

        // Assert
        // Assert
        Assert.Null(metrics.Options.FunctionName); // All invalid values should result in null
        Assert.Same(builder, result);
    }

    [Fact]
    public void Build_Should_Preserve_FunctionName_When_Set_Through_Builder()
    {
        // Arrange
        var builder = new MetricsBuilder()
            .WithNamespace("TestNamespace")
            .WithService("TestService")
            .WithFunctionName("TestFunction");

        // Act
        var metrics = builder.Build();

        // Assert
        Assert.Equal("TestFunction", metrics.Options.FunctionName);
    }
}