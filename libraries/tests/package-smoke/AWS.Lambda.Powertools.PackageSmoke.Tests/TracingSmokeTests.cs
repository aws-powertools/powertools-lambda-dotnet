using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Tracing;
using Xunit;

namespace PackageSmoke.Tests;

public class TracingFunction
{
    [Tracing(SegmentName = "SmokeHandler")]
    public async Task<string> FunctionHandler(string input, ILambdaContext context)
    {
        Tracing.AddAnnotation("input", input);
        await BusinessLogic();
        return input.ToUpperInvariant();
    }

    [Tracing]
    private static Task BusinessLogic() => Task.Delay(1);
}

public sealed class TracingSmokeTests : IDisposable
{
    public TracingSmokeTests()
    {
        // Tracing only records inside Lambda, which it detects through LAMBDA_TASK_ROOT.
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", "/var/task");
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "smoke-tracing");
    }

    [Fact]
    public async Task Handler_Records_Subsegments_And_Annotations()
    {
        // Lambda creates the root segment; stand in for it.
        var root = new Segment("PackageSmoke-Tracing");
        root.SetStartTimeToNow();
        AWSXRayRecorder.Instance.TraceContext.SetEntity(root);

        var result = await new TracingFunction().FunctionHandler("hello", new TestLambdaContext());

        Assert.Equal("HELLO", result);

        var handler = Assert.Single(root.Subsegments);
        Assert.Equal("SmokeHandler", handler.Name);
        Assert.Equal("hello", handler.Annotations["input"]);
        Assert.Equal("smoke-tracing", handler.Annotations["Service"]);
        Assert.IsType<bool>(handler.Annotations["ColdStart"]);

        var businessLogic = Assert.Single(handler.Subsegments);
        Assert.Equal("## BusinessLogic", businessLogic.Name);
    }

    public void Dispose()
    {
        AWSXRayRecorder.Instance.TraceContext.ClearEntity();
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
    }
}
