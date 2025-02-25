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
}