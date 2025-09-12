

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Integration tests to verify ITypedBatchProcessor interface accessibility and usage.
/// </summary>
public class ITypedBatchProcessorIntegrationTest
{
    [Fact]
    public void ITypedBatchProcessor_ShouldBeAccessibleFromNamespace()
    {
        // Arrange & Act
        var interfaceType = typeof(ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage>);

        // Assert
        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsInterface);
        Assert.Equal("AWS.Lambda.Powertools.BatchProcessing", interfaceType.Namespace);
    }

    [Fact]
    public void ITypedBatchProcessor_ShouldHaveCorrectGenericParameters()
    {
        // Arrange & Act
        var interfaceType = typeof(ITypedBatchProcessor<,>);
        var genericParameters = interfaceType.GetGenericArguments();

        // Assert
        Assert.Equal(2, genericParameters.Length);
        Assert.Equal("TEvent", genericParameters[0].Name);
        Assert.Equal("TRecord", genericParameters[1].Name);
    }

    [Fact]
    public void ITypedBatchProcessor_ShouldBeImplementableByConcreteClass()
    {
        // Arrange & Act
        var concreteType = typeof(TestTypedBatchProcessor);
        var interfaceType = typeof(ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage>);

        // Assert
        Assert.True(interfaceType.IsAssignableFrom(concreteType));
    }

    [Fact]
    public async Task ITypedBatchProcessor_ShouldSupportTypedRecordHandlers()
    {
        // Arrange
        var processor = new TestTypedBatchProcessor();
        var handler = new TestTypedRecordHandler();
        var sqsEvent = new SQSEvent();

        // Act
        var result = await processor.ProcessAsync(sqsEvent, handler);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ITypedBatchProcessor_ShouldSupportTypedRecordHandlersWithContext()
    {
        // Arrange
        var processor = new TestTypedBatchProcessor();
        var handler = new TestTypedRecordHandlerWithContext();
        var sqsEvent = new SQSEvent();
        var context = new TestLambdaContext();

        // Act
        var result = await processor.ProcessAsync(sqsEvent, handler, context);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    /// <summary>
    /// Test implementation of ITypedBatchProcessor for integration testing.
    /// </summary>
    public class TestTypedBatchProcessor : ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage>
    {
        public ProcessingResult<SQSEvent.SQSMessage> ProcessingResult { get; } = new ProcessingResult<SQSEvent.SQSMessage>();

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions, ProcessingOptions processingOptions)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions, ProcessingOptions processingOptions)
        {
            return Task.FromResult(ProcessingResult);
        }
    }

    /// <summary>
    /// Test typed record handler.
    /// </summary>
    public class TestTypedRecordHandler : ITypedRecordHandler<TestData>
    {
        public Task<RecordHandlerResult> HandleAsync(TestData data, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.FromData("Success"));
        }
    }

    /// <summary>
    /// Test typed record handler with context.
    /// </summary>
    public class TestTypedRecordHandlerWithContext : ITypedRecordHandlerWithContext<TestData>
    {
        public Task<RecordHandlerResult> HandleAsync(TestData data, ILambdaContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.FromData("Success with context"));
        }
    }

    /// <summary>
    /// Test data class.
    /// </summary>
    public class TestData
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Test Lambda context.
    /// </summary>
    public class TestLambdaContext : ILambdaContext
    {
        public string AwsRequestId { get; set; } = "test-request-id";
        public IClientContext ClientContext { get; set; }
        public string FunctionName { get; set; } = "test-function";
        public string FunctionVersion { get; set; } = "1.0";
        public ICognitoIdentity Identity { get; set; }
        public string InvokedFunctionArn { get; set; } = "arn:aws:lambda:us-east-1:123456789012:function:test-function";
        public ILambdaLogger Logger { get; set; }
        public string LogGroupName { get; set; } = "/aws/lambda/test-function";
        public string LogStreamName { get; set; } = "2023/01/01/[$LATEST]abcdef123456";
        public int MemoryLimitInMB { get; set; } = 128;
        public TimeSpan RemainingTime { get; set; } = TimeSpan.FromMinutes(5);
    }
}