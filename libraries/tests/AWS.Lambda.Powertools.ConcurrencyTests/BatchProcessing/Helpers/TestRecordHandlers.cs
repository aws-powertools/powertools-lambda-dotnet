using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;

/// <summary>
/// Configurable test record handler for SQS messages.
/// </summary>
public class TestSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
{
    private int _processedCount;
    private int _failedCount;

    /// <summary>
    /// Gets the number of records processed.
    /// </summary>
    public int ProcessedCount => _processedCount;

    /// <summary>
    /// Gets the number of records that failed.
    /// </summary>
    public int FailedCount => _failedCount;

    /// <summary>
    /// Gets or sets a function that determines if a record should fail.
    /// </summary>
    public Func<SQSEvent.SQSMessage, bool>? ShouldFail { get; set; }

    /// <summary>
    /// Gets or sets the processing delay to simulate work.
    /// </summary>
    public TimeSpan ProcessingDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the exception to throw when a record fails.
    /// </summary>
    public Func<SQSEvent.SQSMessage, Exception>? ExceptionFactory { get; set; }

    /// <inheritdoc />
    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        if (ProcessingDelay > TimeSpan.Zero)
        {
            await Task.Delay(ProcessingDelay, cancellationToken);
        }

        if (ShouldFail?.Invoke(record) == true)
        {
            Interlocked.Increment(ref _failedCount);
            throw ExceptionFactory?.Invoke(record) ?? new InvalidOperationException($"Simulated failure for {record.MessageId}");
        }

        Interlocked.Increment(ref _processedCount);
        return await Task.FromResult(RecordHandlerResult.None);
    }

    /// <summary>
    /// Resets the handler state.
    /// </summary>
    public void Reset()
    {
        _processedCount = 0;
        _failedCount = 0;
    }
}


/// <summary>
/// Configurable test record handler for Kinesis records.
/// </summary>
public class TestKinesisRecordHandler : IRecordHandler<KinesisEvent.KinesisEventRecord>
{
    private int _processedCount;
    private int _failedCount;

    /// <summary>
    /// Gets the number of records processed.
    /// </summary>
    public int ProcessedCount => _processedCount;

    /// <summary>
    /// Gets the number of records that failed.
    /// </summary>
    public int FailedCount => _failedCount;

    /// <summary>
    /// Gets or sets a function that determines if a record should fail.
    /// </summary>
    public Func<KinesisEvent.KinesisEventRecord, bool>? ShouldFail { get; set; }

    /// <summary>
    /// Gets or sets the processing delay to simulate work.
    /// </summary>
    public TimeSpan ProcessingDelay { get; set; } = TimeSpan.Zero;

    /// <inheritdoc />
    public async Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record, CancellationToken cancellationToken)
    {
        if (ProcessingDelay > TimeSpan.Zero)
        {
            await Task.Delay(ProcessingDelay, cancellationToken);
        }

        if (ShouldFail?.Invoke(record) == true)
        {
            Interlocked.Increment(ref _failedCount);
            throw new InvalidOperationException($"Simulated failure for {record.Kinesis.SequenceNumber}");
        }

        Interlocked.Increment(ref _processedCount);
        return await Task.FromResult(RecordHandlerResult.None);
    }

    /// <summary>
    /// Resets the handler state.
    /// </summary>
    public void Reset()
    {
        _processedCount = 0;
        _failedCount = 0;
    }
}

/// <summary>
/// Configurable test record handler for DynamoDB Stream records.
/// </summary>
public class TestDynamoDbRecordHandler : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>
{
    private int _processedCount;
    private int _failedCount;

    /// <summary>
    /// Gets the number of records processed.
    /// </summary>
    public int ProcessedCount => _processedCount;

    /// <summary>
    /// Gets the number of records that failed.
    /// </summary>
    public int FailedCount => _failedCount;

    /// <summary>
    /// Gets or sets a function that determines if a record should fail.
    /// </summary>
    public Func<DynamoDBEvent.DynamodbStreamRecord, bool>? ShouldFail { get; set; }

    /// <summary>
    /// Gets or sets the processing delay to simulate work.
    /// </summary>
    public TimeSpan ProcessingDelay { get; set; } = TimeSpan.Zero;

    /// <inheritdoc />
    public async Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record, CancellationToken cancellationToken)
    {
        if (ProcessingDelay > TimeSpan.Zero)
        {
            await Task.Delay(ProcessingDelay, cancellationToken);
        }

        if (ShouldFail?.Invoke(record) == true)
        {
            Interlocked.Increment(ref _failedCount);
            throw new InvalidOperationException($"Simulated failure for {record.Dynamodb.SequenceNumber}");
        }

        Interlocked.Increment(ref _processedCount);
        return await Task.FromResult(RecordHandlerResult.None);
    }

    /// <summary>
    /// Resets the handler state.
    /// </summary>
    public void Reset()
    {
        _processedCount = 0;
        _failedCount = 0;
    }
}


/// <summary>
/// Test-specific Kinesis batch processor that exposes the protected constructor.
/// </summary>
public class TestKinesisEventBatchProcessor : KinesisEventBatchProcessor
{
    /// <summary>
    /// Creates a new instance of the test Kinesis batch processor.
    /// </summary>
    public TestKinesisEventBatchProcessor() : base()
    {
    }
}

/// <summary>
/// Test-specific DynamoDB Stream batch processor that exposes the protected constructor.
/// </summary>
public class TestDynamoDbStreamBatchProcessor : DynamoDbStreamBatchProcessor
{
    /// <summary>
    /// Creates a new instance of the test DynamoDB Stream batch processor.
    /// </summary>
    public TestDynamoDbStreamBatchProcessor() : base()
    {
    }
}
