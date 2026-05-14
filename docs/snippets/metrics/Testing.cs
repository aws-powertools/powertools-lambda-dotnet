// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics;

// --8<-- [start:lambda_function_testing]
using System.Collections.Generic;
using Amazon.Lambda.Core;

public class MetricsnBuilderHandler
{
    private readonly IMetrics _metrics;

    // Allow injection of IMetrics for testing
    public MetricsnBuilderHandler(IMetrics metrics = null)
    {
        _metrics = metrics ?? new MetricsBuilder()
            .WithCaptureColdStart(true)
            .WithService("testService")
            .WithNamespace("dotnet-powertools-test")
            .WithDefaultDimensions(new Dictionary<string, string>
            {
                { "Environment", "Prod1" },
                { "Another", "One" }
            }).Build();
    }

    [Metrics]
    public void Handler(ILambdaContext context)
    {
        _metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }
}
// --8<-- [end:lambda_function_testing]

// --8<-- [start:unit_tests]
[Fact]
    public void Handler_With_Builder_Should_Configure_In_Constructor()
    {
        // Arrange
        var handler = new MetricsnBuilderHandler();

        // Act
        handler.Handler(new TestLambdaContext
        {
            FunctionName = "My_Function_Name"
        });

        // Get the output and parse it
        var metricsOutput = _consoleOut.ToString();

        // Assert cold start
        Assert.Contains(
            "\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\",\"Environment\",\"Another\",\"FunctionName\"]]}]},\"Service\":\"testService\",\"Environment\":\"Prod1\",\"Another\":\"One\",\"FunctionName\":\"My_Function_Name\",\"ColdStart\":1}",
            metricsOutput);
        // Assert successful Memory metrics
        Assert.Contains(
            "\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"SuccessfulBooking\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\",\"Environment\",\"Another\",\"FunctionName\"]]}]},\"Service\":\"testService\",\"Environment\":\"Prod1\",\"Another\":\"One\",\"FunctionName\":\"My_Function_Name\",\"SuccessfulBooking\":1}",
            metricsOutput);
    }

    [Fact]
    public void Handler_With_Builder_Should_Configure_In_Constructor_Mock()
    {
        var metricsMock = Substitute.For<IMetrics>();

        metricsMock.Options.Returns(new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "dotnet-powertools-test",
            Service = "testService",
            DefaultDimensions = new Dictionary<string, string>
            {
                { "Environment", "Prod" },
                { "Another", "One" }
            }
        });

        Metrics.UseMetricsForTests(metricsMock);

        var sut = new MetricsnBuilderHandler(metricsMock);

        // Act
        sut.Handler(new TestLambdaContext
        {
            FunctionName = "My_Function_Name"
        });

        metricsMock.Received(1).PushSingleMetric("ColdStart", 1, MetricUnit.Count, "dotnet-powertools-test",
            service: "testService", Arg.Any<Dictionary<string, string>>());
        metricsMock.Received(1).AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }
// --8<-- [end:unit_tests]

// --8<-- [start:inject_metric_namespace]
Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE","AWSLambdaPowertools");
// --8<-- [end:inject_metric_namespace]
