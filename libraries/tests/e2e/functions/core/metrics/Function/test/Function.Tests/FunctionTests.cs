using System.Text.Json;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.Model;
using TestUtils;
using Xunit.Abstractions;

namespace Function.Tests;

[Trait("Category", "E2E")]
public class FunctionTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AmazonLambdaClient _lambdaClient;
    private string _functionName;

    public FunctionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _lambdaClient = new AmazonLambdaClient();
    }

    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_X64_AOT_NET8_metrics")]
    // [InlineData("E2ETestLambda_ARM_AOT_NET8_metrics")]
    public async Task AotFunctionTest(string functionName)
    {
        await TestFunction(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET6_metrics")]
    [InlineData("E2ETestLambda_ARM_NET6_metrics")]
    [InlineData("E2ETestLambda_X64_NET8_metrics")]
    [InlineData("E2ETestLambda_ARM_NET8_metrics")]
    public async Task FunctionTest(string functionName)
    {
        await TestFunction(functionName);
    }

    internal async Task TestFunction(string functionName)
    {
        _functionName = functionName;
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = await File.ReadAllTextAsync("../../../../../../../payload.json"),
            LogType = LogType.Tail
        };

        // run twice for cold and warm start
        for (int i = 0; i < 2; i++)
        {
            var response = await _lambdaClient.InvokeAsync(request);

            if (string.IsNullOrEmpty(response.LogResult))
            {
                Assert.Fail("No LogResult field returned in the response of Lambda invocation.");
            }

            var payload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
            var parsedPayload = JsonSerializer.Deserialize<APIGatewayProxyResponse>(payload);

            if (parsedPayload == null)
            {
                Assert.Fail("Failed to parse payload.");
            }

            Assert.Equal(200, parsedPayload.StatusCode);
            Assert.Equal("HELLO WORLD", parsedPayload.Body);

            // Assert Output log from Lambda execution
            AssertOutputLog(response);
        }
        
        // Assert cloudwatch
        await AssertCloudWatch();
    }
    
    private async Task AssertCloudWatch()
    {
        using var cloudWatchClient = new AmazonCloudWatchClient();
        var request = new ListMetricsRequest
        {
            Namespace = "Test",
            Dimensions =
            [
                new DimensionFilter
                {
                    Name = "Service",
                    Value = "Test"
                },

                new DimensionFilter
                {
                    Name = "FunctionName",
                    Value = _functionName
                }
            ]
        };
        
        var response = await cloudWatchClient.ListMetricsAsync(request);

        foreach (var metric in response.Metrics)
        {
            _testOutputHelper.WriteLine($"Namespace: {metric.Namespace}, MetricName: {metric.MetricName}");
            foreach (var dimension in metric.Dimensions)
            {
                _testOutputHelper.WriteLine($"  Dimension: {dimension.Name} = {dimension.Value}");
            }
        }
    }

    private void AssertOutputLog(InvokeResponse response)
    {
        // Extract and parse log
        var logResult = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
        _testOutputHelper.WriteLine(logResult);
        var output = OutputLogParser.ParseLogSegments(logResult, out var report);
        var isColdStart = report.initDuration != "N/A";
        var index = 0;
        if (isColdStart)
        {
            AssertColdStart(output[index]);
            index += 1;
        }

        AssertSingleMetric(output[index]);
        AssertMetricsDimensionsMetadata(output[index + 1]);
    }

    private void AssertMetricsDimensionsMetadata(string output)
    {
        using JsonDocument doc = JsonDocument.Parse(output);
        JsonElement root = doc.RootElement;

        Assert.True(root.TryGetProperty("_aws", out JsonElement awsElement));

        Assert.True(awsElement.TryGetProperty("CloudWatchMetrics", out JsonElement cloudWatchMetricsElement));
        Assert.True(cloudWatchMetricsElement[0].TryGetProperty("Namespace", out JsonElement namespaceElement));
        Assert.Equal("Test", namespaceElement.GetString());

        Assert.True(cloudWatchMetricsElement[0].TryGetProperty("Metrics", out JsonElement metricsElement));

        Assert.True(metricsElement[0].TryGetProperty("Name", out JsonElement nameElement));
        Assert.Equal("Invocation", nameElement.GetString());
        Assert.True(metricsElement[0].TryGetProperty("Unit", out JsonElement unitElement));
        Assert.Equal("Count", unitElement.GetString());

        Assert.True(metricsElement[1].TryGetProperty("Name", out JsonElement nameElement2));
        Assert.Equal("Memory with Environment dimension", nameElement2.GetString());
        Assert.True(metricsElement[1].TryGetProperty("Unit", out JsonElement unitElement2));
        Assert.Equal("Megabytes", unitElement2.GetString());

        Assert.True(metricsElement[2].TryGetProperty("Name", out JsonElement nameElement3));
        Assert.Equal("Standard resolution", nameElement3.GetString());
        Assert.True(metricsElement[2].TryGetProperty("Unit", out JsonElement unitElement3));
        Assert.Equal("Count", unitElement3.GetString());
        Assert.True(metricsElement[2].TryGetProperty("StorageResolution", out JsonElement storageResolutionElement));
        Assert.Equal(60, storageResolutionElement.GetInt32());

        Assert.True(metricsElement[3].TryGetProperty("Name", out JsonElement nameElement4));
        Assert.Equal("High resolution", nameElement4.GetString());
        Assert.True(metricsElement[3].TryGetProperty("Unit", out JsonElement unitElement4));
        Assert.Equal("Count", unitElement4.GetString());
        Assert.True(metricsElement[3].TryGetProperty("StorageResolution", out JsonElement storageResolutionElement2));
        Assert.Equal(1, storageResolutionElement2.GetInt32());

        Assert.True(metricsElement[4].TryGetProperty("Name", out JsonElement nameElement5));
        Assert.Equal("Default resolution", nameElement5.GetString());
        Assert.True(metricsElement[4].TryGetProperty("Unit", out JsonElement unitElement5));
        Assert.Equal("Count", unitElement5.GetString());

        Assert.True(cloudWatchMetricsElement[0].TryGetProperty("Dimensions", out JsonElement dimensionsElement));
        Assert.Equal("Service", dimensionsElement[0][0].GetString());
        Assert.Equal("Environment", dimensionsElement[0][1].GetString());
        Assert.Equal("Another", dimensionsElement[0][2].GetString());
        Assert.Equal("Memory", dimensionsElement[0][3].GetString());

        Assert.True(root.TryGetProperty("Service", out JsonElement serviceElement));
        Assert.Equal("Test", serviceElement.GetString());

        Assert.True(root.TryGetProperty("Environment", out JsonElement environmentElement));
        Assert.Equal("Prod", environmentElement.GetString());

        Assert.True(root.TryGetProperty("Another", out JsonElement anotherElement));
        Assert.Equal("One", anotherElement.GetString());

        Assert.True(root.TryGetProperty("Memory", out JsonElement memoryElement));
        Assert.Equal("MemoryLimitInMB", memoryElement.GetString());

        Assert.True(root.TryGetProperty("Invocation", out JsonElement invocationElement));
        Assert.Equal(1, invocationElement.GetInt32());

        Assert.True(root.TryGetProperty("Memory with Environment dimension",
            out JsonElement memoryWithEnvironmentElement));
        Assert.Equal(128, memoryWithEnvironmentElement.GetInt32());

        Assert.True(root.TryGetProperty("Standard resolution", out JsonElement standardResolutionElement));
        Assert.Equal(1, standardResolutionElement.GetInt32());

        Assert.True(root.TryGetProperty("High resolution", out JsonElement highResolutionElement));
        Assert.Equal(1, highResolutionElement.GetInt32());

        Assert.True(root.TryGetProperty("Default resolution", out JsonElement defaultResolutionElement));
        Assert.Equal(1, defaultResolutionElement.GetInt32());

        Assert.True(root.TryGetProperty("RequestId", out JsonElement requestIdElement));
        Assert.Equal("c6af9ac6-7b61-11e6-9a41-93e8deadbeef", requestIdElement.GetString());
    }

    private void AssertSingleMetric(string output)
    {
        using JsonDocument doc = JsonDocument.Parse(output);
        JsonElement root = doc.RootElement;

        Assert.True(root.TryGetProperty("_aws", out JsonElement awsElement));

        Assert.True(awsElement.TryGetProperty("CloudWatchMetrics", out JsonElement cloudWatchMetricsElement));
        Assert.True(cloudWatchMetricsElement[0].TryGetProperty("Namespace", out JsonElement namespaceElement));
        Assert.Equal("Test", namespaceElement.GetString());

        Assert.True(cloudWatchMetricsElement[0].TryGetProperty("Metrics", out JsonElement metricsElement));
        Assert.True(metricsElement[0].TryGetProperty("Name", out JsonElement nameElement));
        Assert.Equal("SingleMetric", nameElement.GetString());
        Assert.True(metricsElement[0].TryGetProperty("Unit", out JsonElement unitElement));
        Assert.Equal("Count", unitElement.GetString());

        Assert.True(cloudWatchMetricsElement[0].TryGetProperty("Dimensions", out JsonElement dimensionsElement));
        Assert.Equal("FunctionName", dimensionsElement[0][0].GetString());
        Assert.Equal("Service", dimensionsElement[0][1].GetString());

        Assert.True(root.TryGetProperty("FunctionName", out JsonElement functionNameElement));
        Assert.Equal(_functionName, functionNameElement.GetString());

        Assert.True(root.TryGetProperty("Service", out JsonElement serviceElement));
        Assert.Equal("Test", serviceElement.GetString());

        Assert.True(root.TryGetProperty("SingleMetric", out JsonElement singleMetricElement));
        Assert.Equal(1, singleMetricElement.GetInt32());
    }

    private void AssertColdStart(string output)
    {
        using JsonDocument doc = JsonDocument.Parse(output);
        JsonElement root = doc.RootElement;

        Assert.True(root.TryGetProperty("_aws", out JsonElement awsElement));
        Assert.True(awsElement.TryGetProperty("CloudWatchMetrics", out JsonElement cloudWatchMetricsElement));

        Assert.True(cloudWatchMetricsElement[0].TryGetProperty("Metrics", out JsonElement metricsElement));
        Assert.True(metricsElement[0].TryGetProperty("Name", out JsonElement nameElement));
        Assert.Equal("ColdStart", nameElement.GetString());
        Assert.True(metricsElement[0].TryGetProperty("Unit", out JsonElement unitElement));
        Assert.Equal("Count", unitElement.GetString());

        Assert.True(root.TryGetProperty("ColdStart", out JsonElement coldStartElement));
        Assert.Equal(1, coldStartElement.GetInt32());
    }
}