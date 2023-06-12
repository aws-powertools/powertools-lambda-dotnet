using System;
using System.IO;
using AWS.Lambda.Powertools.Common;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

[Collection("Sequential")]
public class ClearDimensionsTests
{
    [Fact]
    public void WhenClearAllDimensions_NoDimensionsInOutput()
    {
        // Arrange
        var methodName = Guid.NewGuid().ToString();
        var consoleOut = new StringWriter();
        Console.SetOut(consoleOut);

        var configurations = new Mock<IPowertoolsConfigurations>();

        var metrics = new Metrics(
            configurations.Object,
            nameSpace: "dotnet-powertools-test",
            service: "testService"
        );

        var handler = new MetricsAspectHandler(
            metrics,
            false
        );

        var eventArgs = new AspectEventArgs { Name = methodName };

        // Act
        handler.OnEntry(eventArgs);

        Metrics.ClearDefaultDimensions();
        Metrics.AddMetric($"Metric Name", 1, MetricUnit.Count);

        handler.OnExit(eventArgs);

        var metricsOutput = consoleOut.ToString();

        // Assert
        Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name\",\"Unit\":\"Count\"}],\"Dimensions\":[[]]", metricsOutput);

        // Reset
        handler.ResetForTest();
    }
}