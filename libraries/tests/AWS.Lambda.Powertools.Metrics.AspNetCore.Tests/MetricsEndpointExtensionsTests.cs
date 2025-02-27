using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Tests;

[Collection("Metrics")]
public class MetricsEndpointExtensionsTests : IDisposable
{
    [Fact]
    public async Task When_WithMetrics_Should_Add_ColdStart()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace",
            Service = "TestService"
        };
        
        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IMetrics>(metrics);
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.MapGet("/test", () => Results.Ok(new { success = true })).WithMetrics();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);

        // Assert metrics calls
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("CloudWatchMetrics\":[{\"Namespace\":\"TestNamespace\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[]]}]},\"ColdStart\":1}"))
        );


        await app.StopAsync();
    }
    
    [Fact]
    public async Task When_WithMetrics_Should_Add_ColdStart_Dimensions()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace",
            Service = "TestService"
        };
        
        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IMetrics>(metrics);
        builder.WebHost.UseTestServer();

        
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            var lambdaContext = new TestLambdaContext
            {
                FunctionName = "TestFunction"
            };
            context.Items["LambdaContext"] = lambdaContext;
            await next();
        });

        app.MapGet("/test", () => Results.Ok(new { success = true })).WithMetrics();

        await app.StartAsync();
        var client = app.GetTestClient();
        
        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);

        // Assert metrics calls
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("CloudWatchMetrics\":[{\"Namespace\":\"TestNamespace\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"FunctionName\"]]}]},\"FunctionName\":\"TestFunction\",\"ColdStart\":1}"))
        );

        await app.StopAsync();
    }
    
    [Fact]
    public async Task When_WithMetrics_Should_Add_ColdStart_Default_Dimensions()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace",
            Service = "TestService",
            DefaultDimensions = new Dictionary<string, string>
            {
                { "Environment", "Prod" }
            }
        };
        
        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IMetrics>(metrics);
        builder.WebHost.UseTestServer();

        
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            var lambdaContext = new TestLambdaContext
            {
                FunctionName = "TestFunction"
            };
            context.Items["LambdaContext"] = lambdaContext;
            await next();
        });

        app.MapGet("/test", () => Results.Ok(new { success = true })).WithMetrics();

        await app.StartAsync();
        var client = app.GetTestClient();
        
        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);

        // Assert metrics calls
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("CloudWatchMetrics\":[{\"Namespace\":\"TestNamespace\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Environment\",\"FunctionName\"]]}]},\"Environment\":\"Prod\",\"FunctionName\":\"TestFunction\",\"ColdStart\":1}"))
        );

        await app.StopAsync();
    }

    public void Dispose()
    {
        ColdStartTracker.ResetColdStart();
    }
}