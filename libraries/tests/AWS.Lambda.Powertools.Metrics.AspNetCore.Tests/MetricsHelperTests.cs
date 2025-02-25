using System.Reflection;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Tests;

[Collection("Sequential")]
public class MetricsHelperTests : IDisposable
{
    public void Dispose()
    {
        MetricsHelper.ResetColdStart();
        MetricsAspect.ResetForTest();
    }
    
    [Fact]
    public async Task CaptureColdStartMetrics_WhenEnabled_ShouldPushMetric()
    {
        // Arrange
        var metrics = Substitute.For<IMetrics>();
        metrics.Options.Returns(new MetricsOptions 
        { 
            CaptureColdStart = true,
            Namespace = "TestNamespace",
            Service = "TestService"
        });
        
        var context = new DefaultHttpContext();
        var helper = new MetricsHelper(metrics);

        // Act
        await helper.CaptureColdStartMetrics(context);

        // Assert
        metrics.Received(1).PushSingleMetric(
            Arg.Is<string>(s => s == "ColdStart"),
            Arg.Is<double>(d => d == 1.0),
            Arg.Is<MetricUnit>(u => u == MetricUnit.Count),
            Arg.Is<string>(s => s == "TestNamespace"),
            Arg.Is<string>(s => s == "TestService"),
            Arg.Any<Dictionary<string, string>>()
        );
    }

    [Fact]
    public async Task CaptureColdStartMetrics_WhenDisabled_ShouldNotPushMetric()
    {
        // Arrange
        var metrics = Substitute.For<IMetrics>();
        metrics.Options.Returns(new MetricsOptions { CaptureColdStart = false });
        
        var context = new DefaultHttpContext();
        var helper = new MetricsHelper(metrics);

        // Act
        await helper.CaptureColdStartMetrics(context);

        // Assert
        metrics.DidNotReceive().PushSingleMetric(
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<MetricUnit>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>()
        );
    }
}

public static class EndpointFilterInvocationContextHelper
{
    public static EndpointFilterInvocationContext Create(HttpContext httpContext, object[] arguments)
    {
        var endpoint = new RouteEndpoint(
            c => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            0,
            EndpointMetadataCollection.Empty,
            "test");

        var constructor = typeof(EndpointFilterInvocationContext)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First();

        return (EndpointFilterInvocationContext)constructor.Invoke(new object[] { httpContext, endpoint, arguments });
    }
}