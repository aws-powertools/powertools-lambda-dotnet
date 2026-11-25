using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.Common.Utils;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
///     [BatchProcessor] handlers that return value types or a plain Task, with Native AOT simulated through
///     <see cref="RuntimeFeatureWrapper" />.
/// </summary>
[Collection("BatchProcessorTests")]
public class BatchProcessorValueTypeReturnTests : IDisposable
{
    private readonly Functions _functions = new();

    public BatchProcessorValueTypeReturnTests()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        CountingRecordHandler.Processed = 0;
    }

    public void Dispose()
    {
        RuntimeFeatureWrapper.Reset();
    }

    [Fact]
    public void Handle_WhenDynamicCodeIsNotSupported_AndHandlerReturnsInt_ProcessesBatchAndReturnsValue()
    {
        var result = _functions.Int(CreateEvent());

        Assert.Equal(42, result);
        Assert.Equal(1, CountingRecordHandler.Processed);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndHandlerReturnsTaskOfBool_ProcessesBatchAndReturnsValue()
    {
        var result = await _functions.TaskBool(CreateEvent());

        Assert.True(result);
        Assert.Equal(1, CountingRecordHandler.Processed);
    }

    [Fact]
    public async Task Handle_WhenDynamicCodeIsNotSupported_AndHandlerReturnsTask_ProcessesBatch()
    {
        await _functions.Task(CreateEvent());

        Assert.Equal(1, CountingRecordHandler.Processed);
        Assert.True(_functions.TaskCompleted);
    }

    [Fact]
    public void Handle_WhenDynamicCodeIsNotSupported_AndHandlerReturnsUserStruct_ThrowsNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(() => _functions.Struct(CreateEvent()));

        Assert.Contains($"'{nameof(Functions)}.{nameof(Functions.Struct)}'", ex.Message);
        Assert.Contains("'UserStruct'", ex.Message);
        Assert.Contains(nameof(BatchProcessorAttribute), ex.Message);
        Assert.Equal(0, CountingRecordHandler.Processed);
    }

    private static SQSEvent CreateEvent() => new()
    {
        Records = new List<SQSEvent.SQSMessage>
        {
            new()
            {
                MessageId = "1", Body = "{}", EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
            }
        }
    };

    public readonly record struct UserStruct(int Value);

    public class CountingRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
    {
        public static int Processed;

        public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Processed);
            return System.Threading.Tasks.Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class Functions
    {
        public bool TaskCompleted { get; private set; }

        [BatchProcessor(RecordHandler = typeof(CountingRecordHandler))]
        public int Int(SQSEvent _) => 42;

        [BatchProcessor(RecordHandler = typeof(CountingRecordHandler))]
        public async Task<bool> TaskBool(SQSEvent _)
        {
            await System.Threading.Tasks.Task.Yield();
            return true;
        }

        [BatchProcessor(RecordHandler = typeof(CountingRecordHandler))]
        public async Task Task(SQSEvent _)
        {
            await System.Threading.Tasks.Task.Yield();
            TaskCompleted = true;
        }

        [BatchProcessor(RecordHandler = typeof(CountingRecordHandler))]
        public UserStruct Struct(SQSEvent _) => new(1);
    }
}
