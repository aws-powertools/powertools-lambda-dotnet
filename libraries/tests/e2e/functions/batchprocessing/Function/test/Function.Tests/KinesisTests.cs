using System.Text;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Lambda;
using Xunit;
using Xunit.Abstractions;

namespace Function.Tests;

public class KinesisTests : TestBase
{
    private readonly AmazonKinesisClient _kinesisClient;
    private readonly AmazonLambdaClient _lambdaClient;
    private readonly string _streamName = "BatchProcessingKinesisStream";

    public KinesisTests(ITestOutputHelper output) : base(output)
    {
        _kinesisClient = new AmazonKinesisClient();
        _lambdaClient = new AmazonLambdaClient();
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_batchprocessing_Kinesis")]
    [InlineData("E2ETestLambda_ARM_NET8_batchprocessing_Kinesis")]
    [InlineData("E2ETestLambda_X64_NET6_batchprocessing_Kinesis")]
    [InlineData("E2ETestLambda_ARM_NET6_batchprocessing_Kinesis")]
    public async Task TestSuccessfulProcessing(string functionName)
    {
        await TestSuccessfulKinesisProcessing(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_batchprocessing_Kinesis")]
    [InlineData("E2ETestLambda_ARM_NET8_batchprocessing_Kinesis")]
    [InlineData("E2ETestLambda_X64_NET6_batchprocessing_Kinesis")]
    [InlineData("E2ETestLambda_ARM_NET6_batchprocessing_Kinesis")]
    public async Task TestFailedItems(string functionName)
    {
        await TestFailedKinesisItems(functionName);
    }

    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_batchprocessing_Kinesis")]
    [InlineData("E2ETestLambda_X64_AOT_NET8_batchprocessing_Kinesis")]
    public async Task TestAotFunctionProcessing(string functionName)
    {
        await TestAotKinesisProcessing(functionName);
    }

    private async Task TestSuccessfulKinesisProcessing(string functionName)
    {
        // Arrange - Put records into the Kinesis stream
        var putRecordsRequest = new PutRecordsRequest
        {
            StreamName = _streamName,
            Records = new List<PutRecordsRequestEntry>()
        };

        for (int i = 1; i <= 3; i++)
        {
            var record = new { Id = i, Name = $"Kinesis Record {i}", Status = "success", Timestamp = DateTime.UtcNow };
            var data = Encoding.UTF8.GetBytes(Serialize(record));

            putRecordsRequest.Records.Add(new PutRecordsRequestEntry
            {
                Data = new MemoryStream(data),
                PartitionKey = $"partition-{i}"
            });
        }

        await _kinesisClient.PutRecordsAsync(putRecordsRequest);

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Assert - Check logs for successful processing
        var success = await WaitForSuccessInLogs(
            functionName,
            "Processing record:",
            TimeSpan.FromMinutes(2));

        Assert.True(success, $"Failed to find successful processing in logs for {functionName}");
    }

    private async Task TestFailedKinesisItems(string functionName)
    {
        // Create one record that should fail
        var errorRecord = new { Id = 999, Name = "Error Record", Status = "error", Timestamp = DateTime.UtcNow };
        var errorData = Encoding.UTF8.GetBytes(Serialize(errorRecord));

        await _kinesisClient.PutRecordAsync(new PutRecordRequest
        {
            StreamName = _streamName,
            Data = new MemoryStream(errorData),
            PartitionKey = "error-partition"
        });

        // Create a success record after
        var successRecord = new { Id = 1000, Name = "Success After Error", Status = "success", Timestamp = DateTime.UtcNow };
        var successData = Encoding.UTF8.GetBytes(Serialize(successRecord));

        await _kinesisClient.PutRecordAsync(new PutRecordRequest
        {
            StreamName = _streamName,
            Data = new MemoryStream(successData),
            PartitionKey = "success-partition"
        });

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Check for error handling
        var failureLogged = await WaitForSuccessInLogs(
            functionName,
            "Failed to process record with status error",
            TimeSpan.FromMinutes(2));

        Assert.True(failureLogged, $"Failed to find error handling in logs for {functionName}");
    }

    private async Task TestAotKinesisProcessing(string functionName)
    {
        // Send a record to test AOT function
        var aotRecord = new { Id = 500, Name = "AOT Test Record", Status = "success", Timestamp = DateTime.UtcNow };
        var aotData = Encoding.UTF8.GetBytes(Serialize(aotRecord));

        await _kinesisClient.PutRecordAsync(new PutRecordRequest
        {
            StreamName = _streamName,
            Data = new MemoryStream(aotData),
            PartitionKey = "aot-test-partition"
        });

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Check AOT function logs
        var aotSuccess = await WaitForSuccessInLogs(
            functionName,
            "Processing record:",
            TimeSpan.FromMinutes(2));

        Assert.True(aotSuccess, $"Failed to find AOT function processing in logs for {functionName}");
    }
}