using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Tests;

[Collection("Sequential")]
public class MetricsMiddlewareExtensionsTests : IDisposable
{
    public MetricsMiddlewareExtensionsTests()
    {
        MetricsHelper.ResetColdStart();
        MetricsAspect.ResetForTest();
    }

    public void Dispose()
    {
        MetricsHelper.ResetColdStart();
        MetricsAspect.ResetForTest();
    }

    [Fact]
    public async Task UseMetrics_ShouldCaptureColdStart_WhenEnabled()
    {
        // Arrange
        var metrics = Substitute.For<IMetrics>();
        metrics.Options.Returns(new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace",
            Service = "TestService"
        });

        var services = new ServiceCollection();
        services.AddSingleton(metrics);
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var appBuilder = new ApplicationBuilder(serviceProvider);
        appBuilder.UseMetrics();
        var app = appBuilder.Build();

        // Act
        await app.Invoke(context);

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
    public async Task UseMetrics_ShouldNotCaptureColdStart_WhenDisabled()
    {
        // Arrange
        var metrics = Substitute.For<IMetrics>();
        metrics.Options.Returns(new MetricsOptions
        {
            CaptureColdStart = false,
            Namespace = "TestNamespace",
            Service = "TestService"
        });

        var services = new ServiceCollection();
        services.AddSingleton(metrics);
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var appBuilder = new ApplicationBuilder(serviceProvider);
        appBuilder.UseMetrics();
        var app = appBuilder.Build();

        // Act
        await app.Invoke(context);

        // Assert
        metrics.DidNotReceive().PushSingleMetric(
            Arg.Is<string>(s => s == "ColdStart"),
            Arg.Any<double>(),
            Arg.Any<MetricUnit>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>()
        );
    }
}