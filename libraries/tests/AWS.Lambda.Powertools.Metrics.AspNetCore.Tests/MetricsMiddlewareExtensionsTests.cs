using System.Runtime.CompilerServices;
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
public class MetricsMiddlewareExtensionsTests : IDisposable
{
    [Fact]
    public async Task When_UseMetrics_Should_Add_ColdStart()
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
        app.UseMetrics();
        app.MapGet("/test", () => Results.Ok());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("CloudWatchMetrics\":[{\"Namespace\":\"TestNamespace\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[]]}]},\"ColdStart\":1}"))
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task When_UseMetrics_Should_Add_ColdStart_With_LambdaContext()
    {
        // Arrange
        var options = new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "TestNamespace",
        };
        
        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, consoleWrapper:consoleWrapper, options: options);

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
        app.UseMetrics();
        app.MapGet("/test", () => Results.Ok());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("CloudWatchMetrics\":[{\"Namespace\":\"TestNamespace\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"FunctionName\"]]}]},\"FunctionName\":\"TestFunction\",\"ColdStart\":1}"))
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task When_UseMetrics_And_Multiple_Requests_Should_Add_ColdStart_Once()
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
        app.UseMetrics();
        app.MapGet("/test", () => Results.Ok());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var statusCodes = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/test");
            statusCodes.Add((int)response.StatusCode);
        }

        // Assert
        Assert.All(statusCodes, code => Assert.Equal(200, code));
        consoleWrapper.Received(1).WriteLine(Arg.Is<string>(s => s.Contains("\"ColdStart\":1")));

        await app.StopAsync();
    }

    [Fact]
    public async Task When_UseMetrics_With_DefaultDimensions_And_Multiple_Requests_Should_Add_ColdStart_Once_And_Not_Throw()
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
        app.UseMetrics();
        app.MapGet("/test", () => Results.Ok());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var statusCodes = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/test");
            statusCodes.Add((int)response.StatusCode);
        }

        // Assert
        Assert.All(statusCodes, code => Assert.Equal(200, code));
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s =>
                s.Contains("\"Name\":\"ColdStart\",\"Unit\":\"Count\"") &&
                s.Contains("\"Environment\":\"Prod\"") &&
                s.Contains("\"FunctionName\":\"TestFunction\"") &&
                s.Contains("\"ColdStart\":1"))
        );
        Assert.Equal(new Dictionary<string, string> { { "Environment", "Prod" } }, options.DefaultDimensions);

        await app.StopAsync();
    }

    [Theory]
    [InlineData("provisioned-concurrency", 0)]
    [InlineData("on-demand", 1)]
    [InlineData(null, 1)]
    public async Task When_UseMetrics_With_InitializationType_Should_Add_ColdStart_Only_When_Not_Provisioned(
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
            app.UseMetrics();
        app.MapGet("/test", () => Results.Ok());

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

    [Fact]
    public async Task When_UseMetrics_And_Endpoint_Resumes_On_Another_Thread_Should_Flush_Metrics_Added_Before_The_Await()
    {
        // Arrange
        Metrics.ResetForTest();
        var conf = Substitute.For<IPowertoolsConfigurations>();
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var metrics = new Metrics(conf, nameSpace: "TestNamespace", consoleWrapper: consoleWrapper);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IMetrics>(metrics);
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseMetrics();
        app.MapGet("/test", async (IMetrics m) =>
        {
            m.AddMetric("BeforeAwait", 1, MetricUnit.Count);
            await new ResumeOnNewThread();
            m.AddMetric("AfterAwait", 1, MetricUnit.Count);
            m.Flush();
            return Results.Ok();
        });

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);
        consoleWrapper.Received(1).WriteLine(
            Arg.Is<string>(s => s.Contains("\"BeforeAwait\":1") && s.Contains("\"AfterAwait\":1")));

        await app.StopAsync();
        Metrics.ResetForTest();
    }

    /// <summary>
    /// Never completes synchronously and resumes on a brand-new thread, so the code after the
    /// await always runs on a different thread than the code before it.
    /// </summary>
    private readonly struct ResumeOnNewThread : ICriticalNotifyCompletion
    {
        public ResumeOnNewThread GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) =>
            new Thread(() => continuation()) { IsBackground = true }.Start();
    }

    public void Dispose()
    {
        ColdStartTracker.ResetColdStart();
    }
}