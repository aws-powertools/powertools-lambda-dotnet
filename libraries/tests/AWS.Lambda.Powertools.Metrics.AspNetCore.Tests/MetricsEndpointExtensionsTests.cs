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
            Namespace = "TestNamespace"
        };
        
        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IMetrics>(metrics);
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.MapGet("/test", () => Results.Text("ok")).WithMetrics();

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

        app.MapGet("/test", () => Results.Text("ok")).WithMetrics();

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

        app.MapGet("/test", () => Results.Text("ok")).WithMetrics();

        await app.StartAsync();
        var client = app.GetTestClient();
        
        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);

        // Assert metrics calls - check key properties without caring about dimension order
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => 
                s.Contains("\"Namespace\":\"TestNamespace\"") &&
                s.Contains("\"Name\":\"ColdStart\",\"Unit\":\"Count\"") &&
                s.Contains("\"Environment\":\"Prod\"") &&
                s.Contains("\"FunctionName\":\"TestFunction\"") &&
                s.Contains("\"ColdStart\":1"))
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task When_WithMetrics_With_DefaultDimensions_And_Multiple_Requests_Should_Add_ColdStart_Once()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace",
            FunctionName = "TestFunction",
            DefaultDimensions = new Dictionary<string, string> { { "Environment", "Prod" } }
        };

        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IMetrics>(metrics);
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapGet("/test", () => Results.Text("ok")).WithMetrics();
        app.MapGet("/other", () => Results.Text("ok")).WithMetrics();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var statusCodes = new List<int>();
        foreach (var path in new[] { "/test", "/other", "/test" })
        {
            var response = await client.GetAsync(path);
            statusCodes.Add((int)response.StatusCode);
        }

        // Assert
        Assert.All(statusCodes, code => Assert.Equal(200, code));
        consoleWrapper.Received(1).WriteLine(Arg.Is<string>(s => s.Contains("\"ColdStart\":1")));
        Assert.Equal(new Dictionary<string, string> { { "Environment", "Prod" } }, options.DefaultDimensions);

        await app.StopAsync();
    }

    [Fact]
    public async Task When_MetricsFilter_Is_Disposed_Should_Not_Add_ColdStart_Again()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace"
        };

        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IMetrics>(metrics);
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapGet("/test", () => Results.Text("ok")).WithMetrics();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        await client.GetAsync("/test");
        // A filter instance being disposed (e.g. by a DI container) must not reset cold start tracking
        new MetricsFilter(metrics).Dispose();
        await client.GetAsync("/test");
        await client.GetAsync("/test");

        // Assert
        consoleWrapper.Received(1).WriteLine(Arg.Is<string>(s => s.Contains("\"ColdStart\":1")));

        await app.StopAsync();
    }

    [Theory]
    [InlineData("provisioned-concurrency", 0)]
    [InlineData("on-demand", 1)]
    [InlineData(null, 1)]
    public async Task When_WithMetrics_With_InitializationType_Should_Add_ColdStart_Only_When_Not_Provisioned(
        string? initializationType, int expectedColdStarts)
    {
        // Arrange
        var previous = Environment.GetEnvironmentVariable("AWS_LAMBDA_INITIALIZATION_TYPE");
        Environment.SetEnvironmentVariable("AWS_LAMBDA_INITIALIZATION_TYPE", initializationType);
        try
        {
            var options = new MetricsOptions
            {
                CaptureColdStart = true,
                Namespace = "TestNamespace",
            };

            var conf = Substitute.For<IPowertoolsConfigurations>();
            var consoleWrapper = Substitute.For<IConsoleWrapper>();
            var metrics = new Metrics(conf, consoleWrapper: consoleWrapper, options: options);

            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSingleton<IMetrics>(metrics);
            builder.WebHost.UseTestServer();

            var app = builder.Build();
            app.MapGet("/test", () => Results.Ok()).WithMetrics();

            await app.StartAsync();
            var client = app.GetTestClient();

            // Act
            var statusCodes = new List<int>();
            for (var i = 0; i < 2; i++)
            {
                var response = await client.GetAsync("/test");
                statusCodes.Add((int)response.StatusCode);
            }

            // Assert
            Assert.All(statusCodes, code => Assert.Equal(200, code));
            consoleWrapper.Received(expectedColdStarts).WriteLine(Arg.Is<string>(s => s.Contains("\"ColdStart\":1")));

            await app.StopAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable("AWS_LAMBDA_INITIALIZATION_TYPE", previous);
        }
    }

    public void Dispose()
    {
        ColdStartTracker.ResetColdStart();
    }
}