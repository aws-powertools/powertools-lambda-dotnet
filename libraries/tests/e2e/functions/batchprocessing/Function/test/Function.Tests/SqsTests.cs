using Amazon;
using Amazon.Lambda;
using Amazon.SQS;
using Amazon.SQS.Model;
using Xunit;
using Xunit.Abstractions;

namespace Function.Tests;

public class SqsTests : TestBase
{
    private readonly AmazonSQSClient _sqsClient;
    private readonly AmazonLambdaClient _lambdaClient;
    private readonly string _standardQueueUrl;
    private readonly string _fifoQueueUrl;

    public SqsTests(ITestOutputHelper output) : base(output)
    {
        _sqsClient = new AmazonSQSClient();
        _lambdaClient = new AmazonLambdaClient();
        _standardQueueUrl = "https://sqs.eu-west-1.amazonaws.com/746792595426/BatchProcessingStandardQueue";
        _fifoQueueUrl = "https://sqs.eu-west-1.amazonaws.com/746792595426/BatchProcessingFifoQueue.fifo";
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_ARM_NET8_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_X64_NET6_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_ARM_NET6_batchprocessing_SQS")]
    public async Task TestStandardQueueSuccessfulProcessing(string functionName)
    {
        await TestStandardQueueSuccess(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_ARM_NET8_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_X64_NET6_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_ARM_NET6_batchprocessing_SQS")]
    public async Task TestStandardQueueFailedItems(string functionName)
    {
        await TestStandardQueueFailures(functionName);
    }

    [Theory]
    [InlineData("E2ETestLambda_X64_NET8_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_ARM_NET8_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_X64_NET6_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_ARM_NET6_batchprocessing_SQS")]
    public async Task TestFifoQueueStopOnFirstFailure(string functionName)
    {
        await TestFifoQueueFailureHandling(functionName);
    }

    [Trait("Category", "AOT")]
    [Theory]
    [InlineData("E2ETestLambda_ARM_AOT_NET8_batchprocessing_SQS")]
    [InlineData("E2ETestLambda_X64_AOT_NET8_batchprocessing_SQS")]
    public async Task TestAotStandardQueueProcessing(string functionName)
    {
        await TestStandardQueueSuccess(functionName);
    }

    private async Task TestStandardQueueSuccess(string functionName)
    {
        // Arrange - Send 3 messages to standard queue
        for (int i = 1; i <= 3; i++)
        {
            var message = new { Id = i, Name = $"Test Item {i}", Action = "process" };
            var request = new SendMessageRequest
            {
                QueueUrl = _standardQueueUrl,
                MessageBody = Serialize(message)
            };

            await _sqsClient.SendMessageAsync(request);
        }

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Assert - Check logs for processing
        var success = await WaitForSuccessInLogs(
            functionName,
            "Processing message:",
            TimeSpan.FromMinutes(2));

        Assert.True(success, $"Failed to find successful processing in logs for {functionName}");
    }

    private async Task TestStandardQueueFailures(string functionName)
    {
        // Arrange - Send a message that should fail
        var failMessage = new { Id = 999, Name = "Failure Test", Action = "fail" };
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _standardQueueUrl,
            MessageBody = Serialize(failMessage)
        });

        // Send another message that should succeed
        var successMessage = new { Id = 1000, Name = "Success After Failure", Action = "process" };
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _standardQueueUrl,
            MessageBody = Serialize(successMessage)
        });

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Assert - Check for failure handling
        var failureLogged = await WaitForSuccessInLogs(
            functionName,
            "Failed to process message with ID:",
            TimeSpan.FromMinutes(2));

        Assert.True(failureLogged, $"Failed to find error handling in logs for {functionName}");
    }

    private async Task TestFifoQueueFailureHandling(string functionName)
    {
        // Arrange - Send messages to FIFO queue with a group ID
        string messageGroupId = Guid.NewGuid().ToString();

        // First message should succeed
        var message1 = new { Id = 1, Name = "FIFO First Message", Action = "process" };
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _fifoQueueUrl,
            MessageBody = Serialize(message1),
            MessageGroupId = messageGroupId,
            MessageDeduplicationId = Guid.NewGuid().ToString()
        });

        // Second message should fail
        var message2 = new { Id = 2, Name = "FIFO Second Message", Action = "fail" };
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _fifoQueueUrl,
            MessageBody = Serialize(message2),
            MessageGroupId = messageGroupId,
            MessageDeduplicationId = Guid.NewGuid().ToString()
        });

        // Third message should be returned as unprocessed since second failed
        var message3 = new { Id = 3, Name = "FIFO Third Message", Action = "process" };
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _fifoQueueUrl,
            MessageBody = Serialize(message3),
            MessageGroupId = messageGroupId,
            MessageDeduplicationId = Guid.NewGuid().ToString()
        });

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(15));

        // Look for error and also check that the third message was properly returned
        var batchFailure = await WaitForSuccessInLogs(
            functionName,
            "BatchItemFailures",
            TimeSpan.FromMinutes(2));

        Assert.True(batchFailure, $"Failed to find batch failure handling for FIFO queue in {functionName}");
    }
}