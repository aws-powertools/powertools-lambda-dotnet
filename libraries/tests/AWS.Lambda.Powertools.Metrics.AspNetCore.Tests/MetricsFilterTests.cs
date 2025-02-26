using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Tests;

public class MetricsFilterTests : IDisposable
{
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
    public async Task InvokeAsync_Second_Call_DoesNotRecord_ColdStart_Metric()
    {
        // Arrange
        var options = new MetricsOptions { CaptureColdStart = false };
        _metrics.Options.Returns(options);

        var filter = new MetricsFilter(_metrics);
        var next = new EndpointFilterDelegate(_ => ValueTask.FromResult<object?>("result"));

        // Act
        _ = await filter.InvokeAsync(_context, next);
        var result = await filter.InvokeAsync(_context, next);

        // Assert
        _metrics.Received(1).CaptureColdStartMetric(Arg.Any<ILambdaContext>() );
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

    public void Dispose()
    {
        MetricsHelper.ResetColdStart();
    }
}