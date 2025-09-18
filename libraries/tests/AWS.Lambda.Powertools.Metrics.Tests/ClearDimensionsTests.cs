using System;
using System.IO;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Metrics.Tests.Handlers;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

[Collection("Sequential")]
public class ClearDimensionsTests : IDisposable
{
    [Fact]
    public void WhenClearAllDimensions_NoDimensionsInOutput()
    {
        // Arrange
        var consoleOut = new StringWriter();
        ConsoleWrapper.SetOut(consoleOut);
        
        // Act
        var handler = new FunctionHandler();
        handler.ClearDimensions();

        var metricsOutput = consoleOut.ToString();

        // Assert
        Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name\",\"Unit\":\"Count\"}],\"Dimensions\":[[]]", metricsOutput);
    }

    public void Dispose()
    {
        // Reset metrics state after each test
        Metrics.ResetForTest();
        MetricsAspect.ResetForTest();
    }
}