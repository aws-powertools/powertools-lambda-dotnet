using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Tests;

[Collection("Sequential")]
public class MetricsFilterTests : IDisposable
{
    public void Dispose()
    {
        MetricsHelper.ResetColdStart();
        MetricsAspect.ResetForTest();
    }
    
    private readonly IMetrics _metrics;
    private readonly EndpointFilterInvocationContext _context;
    private readonly ILambdaContext _lambdaContext;

    public MetricsFilterTests()
    {
        MetricsHelper.ResetColdStart(); // Reset before each test
        _metrics = Substitute.For<IMetrics>();
        _context = Substitute.For<EndpointFilterInvocationContext>();
        _lambdaContext = Substitute.For<ILambdaContext>();

        var httpContext = new DefaultHttpContext();
        httpContext.Items["LambdaContext"] = _lambdaContext;
        _context.HttpContext.Returns(httpContext);
    }

    [Fact]
    public async Task InvokeAsync_WhenColdStartEnabled_RecordsColdStartMetric()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace",
            Service = "TestService",
            DefaultDimensions = new Dictionary<string, string>()
        };

        _metrics.Options.Returns(options);
        _lambdaContext.FunctionName.Returns("TestFunction");

        var filter = new MetricsFilter(_metrics);
        var next = new EndpointFilterDelegate(_ => ValueTask.FromResult<object?>("result"));

        // Act
        var result = await filter.InvokeAsync(_context, next);

        // Assert
        _metrics.Received(1).PushSingleMetric(
            "ColdStart",
            1.0,
            MetricUnit.Count,
            "TestNamespace",
            "TestService",
            Arg.Any<Dictionary<string, string>>()
        );
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task InvokeAsync_WhenColdStartDisabled_DoesNotRecordMetric()
    {
        // Arrange
        var options = new MetricsOptions { CaptureColdStart = false };
        _metrics.Options.Returns(options);

        var filter = new MetricsFilter(_metrics);
        var next = new EndpointFilterDelegate(_ => ValueTask.FromResult<object?>("result"));

        // Act
        var result = await filter.InvokeAsync(_context, next);

        // Assert
        _metrics.DidNotReceive().PushSingleMetric(
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<MetricUnit>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>()
        );
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextAndContinue()
    {
        // Arrange
        var metrics = Substitute.For<IMetrics>();
        metrics.Options.Returns(new MetricsOptions { CaptureColdStart = true });
        
        var httpContext = new DefaultHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext);
        var filter = new MetricsFilter(metrics);
        
        var called = false;
        EndpointFilterDelegate next = _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>("result");
        };

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.True(called);
        Assert.Equal("result", result);
    }
}