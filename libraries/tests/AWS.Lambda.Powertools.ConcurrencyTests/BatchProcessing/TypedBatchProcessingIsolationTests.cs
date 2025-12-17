using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing;

/// <summary>
/// Tests for validating typed batch processor isolation under concurrent execution scenarios.
/// These tests verify that when multiple Lambda invocations run concurrently (multi-instance mode),
/// each invocation's typed deserialization and record handling remains isolated from other invocations.
/// </summary>
[Collection("BatchProcessing Concurrency Tests")]
public class TypedBatchProcessingIsolationTests
{
    /// <summary>
    /// Verifies that concurrent invocations using TypedSqsBatchProcessor
    /// each receive their own ProcessingResult with correctly deserialized records.
    /// Requirements: 3.1, 3.2
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConcurrentTypedInvocations_ShouldMaintainDeserializationIsolation(int concurrencyLevel)
    {
        // Arrange
        var results = new TypedInvocationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var recordCountsPerInvocation = Enumerable.Range(0, concurrencyLevel)
            .Select(i => 3 + (i * 2)) // Different record counts: 3, 5, 7, 9, etc.
            .ToArray();

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"typed-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var recordCount = recordCountsPerInvocation[invocationIndex];
                var sqsEvent = TestEventFactory.CreateTypedSqsEvent<TestMessage>(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = new TestTypedRecordHandler<TestMessage>();

                // Synchronize all invocations to start at the same time
                barrier.SignalAndWait();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Create a new processor instance for each invocation to ensure isolation
                var processor = new TypedSqsBatchProcessor();
                var result = await processor.ProcessAsync<TestMessage>(sqsEvent, handler);
                
                stopwatch.Stop();

                results[invocationIndex] = new TypedInvocationResult
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordCount,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result,
                    ProcessedMessages = handler.ProcessedMessages.ToList(),
                    Duration = stopwatch.Elapsed
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result.ActualResult);
            Assert.Equal(result.ExpectedRecordCount, result.ActualResult.BatchRecords.Count);
            
            // Verify all records in the result belong to this invocation
            var actualRecordIds = result.ActualResult.BatchRecords
                .Select(r => r.MessageId)
                .ToHashSet();
            Assert.True(actualRecordIds.SetEquals(result.ExpectedRecordIds),
                $"Invocation {result.InvocationId}: Expected records {string.Join(",", result.ExpectedRecordIds)} " +
                $"but got {string.Join(",", actualRecordIds)}");
            
            // Verify deserialized messages belong to this invocation
            Assert.Equal(result.ExpectedRecordCount, result.ProcessedMessages.Count);
            foreach (var msg in result.ProcessedMessages)
            {
                Assert.Equal(result.InvocationId, msg.InvocationId);
            }
        }
    }

    /// <summary>
    /// Verifies that concurrent invocations with custom record handlers
    /// maintain handler isolation without cross-invocation state sharing.
    /// Requirements: 3.2
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConcurrentInvocations_ShouldMaintainRecordHandlerIsolation(int concurrencyLevel)
    {
        // Arrange
        var results = new TypedInvocationResult[concurrencyLevel];
        var handlers = new TestTypedRecordHandler<TestMessage>[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var recordCount = 5;

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            handlers[invocationIndex] = new TestTypedRecordHandler<TestMessage>();
            
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"handler-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var sqsEvent = TestEventFactory.CreateTypedSqsEvent<TestMessage>(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = handlers[invocationIndex];

                barrier.SignalAndWait();

                var processor = new TypedSqsBatchProcessor();
                var result = await processor.ProcessAsync<TestMessage>(sqsEvent, handler);

                results[invocationIndex] = new TypedInvocationResult
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordCount,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result,
                    ProcessedMessages = handler.ProcessedMessages.ToList()
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Each handler should only have processed its own invocation's messages
        for (int i = 0; i < concurrencyLevel; i++)
        {
            var result = results[i];
            var handler = handlers[i];
            
            Assert.NotNull(result.ActualResult);
            Assert.Equal(recordCount, handler.ProcessedCount);
            
            // Verify handler only processed messages from its own invocation
            foreach (var msg in handler.ProcessedMessages)
            {
                Assert.Equal(result.InvocationId, msg.InvocationId);
            }
            
            // Verify no messages from other invocations
            var otherInvocationIds = results
                .Where(r => r.InvocationId != result.InvocationId)
                .Select(r => r.InvocationId)
                .ToHashSet();
            
            var foreignMessages = handler.ProcessedMessages
                .Where(m => otherInvocationIds.Contains(m.InvocationId))
                .ToList();
            
            Assert.Empty(foreignMessages);
        }
    }

    /// <summary>
    /// Verifies that deserialization errors in one invocation do not affect
    /// other concurrent invocations.
    /// Requirements: 3.3
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public async Task ConcurrentInvocations_DeserializationErrorsShouldBeIsolated(int concurrencyLevel)
    {
        // Arrange
        var results = new TypedInvocationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var recordCount = 5;

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"error-inv-{invocationIndex}-{Guid.NewGuid():N}";
                SQSEvent sqsEvent;
                int expectedFailures;
                
                // Every other invocation will have invalid JSON in some records
                if (invocationIndex % 2 == 0)
                {
                    sqsEvent = TestEventFactory.CreateTypedSqsEvent<TestMessage>(recordCount, invocationId);
                    expectedFailures = 0;
                }
                else
                {
                    sqsEvent = TestEventFactory.CreateMixedValidInvalidSqsEvent(recordCount, invocationIndex, invocationId);
                    expectedFailures = invocationIndex; // Number of invalid records
                }
                
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = new TestTypedRecordHandler<TestMessage>();

                barrier.SignalAndWait();

                var processor = new TypedSqsBatchProcessor();
                var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                var result = await processor.ProcessAsync<TestMessage>(sqsEvent, handler, null, processingOptions);

                results[invocationIndex] = new TypedInvocationResult
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordCount,
                    ExpectedFailureCount = expectedFailures,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result,
                    ProcessedMessages = handler.ProcessedMessages.ToList()
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Each invocation should have its own error handling isolated
        foreach (var result in results)
        {
            Assert.NotNull(result.ActualResult);
            Assert.Equal(result.ExpectedRecordCount, result.ActualResult.BatchRecords.Count);
            Assert.Equal(result.ExpectedFailureCount, result.ActualResult.FailureRecords.Count);
            
            // Verify all failure IDs belong to this invocation
            foreach (var failure in result.ActualResult.BatchItemFailuresResponse.BatchItemFailures)
            {
                Assert.True(failure.ItemIdentifier.StartsWith(result.InvocationId),
                    $"Failure ID {failure.ItemIdentifier} should start with invocation ID {result.InvocationId}");
            }
        }
    }


    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 7: Typed Deserialization Isolation**
    /// 
    /// Property: For any set of concurrent typed batch processing invocations, each invocation's
    /// deserialization SHALL operate independently without affecting other invocations.
    /// 
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(3, 2, 10)]
    [InlineData(5, 4, 8)]
    [InlineData(10, 3, 6)]
    public async Task Property7_TypedDeserializationIsolation_EachInvocationDeserializesIndependently(
        int concurrencyLevel, int minRecords, int maxRecords)
    {
        // Arrange - Generate random record counts for each invocation
        var random = new Random();
        var recordCounts = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => random.Next(minRecords, maxRecords + 1))
            .ToArray();

        var results = new TypedInvocationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop7-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var recordCount = recordCounts[invocationIndex];
                var sqsEvent = TestEventFactory.CreateTypedSqsEvent<TestMessage>(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = new TestTypedRecordHandler<TestMessage>();

                barrier.SignalAndWait();

                var processor = new TypedSqsBatchProcessor();
                var result = await processor.ProcessAsync<TestMessage>(sqsEvent, handler);

                results[invocationIndex] = new TypedInvocationResult
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordCount,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result,
                    ProcessedMessages = handler.ProcessedMessages.ToList()
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Property: Each invocation's deserialization is independent
        foreach (var result in results)
        {
            Assert.NotNull(result.ActualResult);
            
            // Property check 1: Record count matches
            Assert.Equal(result.ExpectedRecordCount, result.ActualResult.BatchRecords.Count);
            
            // Property check 2: All deserialized messages belong to this invocation
            Assert.Equal(result.ExpectedRecordCount, result.ProcessedMessages.Count);
            foreach (var msg in result.ProcessedMessages)
            {
                Assert.Equal(result.InvocationId, msg.InvocationId);
            }
            
            // Property check 3: No foreign messages (messages from other invocations)
            var otherInvocationIds = results
                .Where(r => r.InvocationId != result.InvocationId)
                .Select(r => r.InvocationId)
                .ToHashSet();
            
            var foreignMessages = result.ProcessedMessages
                .Where(m => otherInvocationIds.Contains(m.InvocationId))
                .ToList();
            
            Assert.Empty(foreignMessages);
        }
    }

    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 8: Record Handler Isolation**
    /// 
    /// Property: For any set of concurrent invocations using custom record handlers, each handler
    /// instance SHALL process only records from its own invocation without cross-invocation state sharing.
    /// 
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(3, 8)]
    [InlineData(5, 6)]
    [InlineData(10, 4)]
    public async Task Property8_RecordHandlerIsolation_EachHandlerProcessesOnlyOwnRecords(
        int concurrencyLevel, int recordsPerInvocation)
    {
        // Arrange
        var results = new TypedInvocationResult[concurrencyLevel];
        var handlers = new TestTypedRecordHandler<TestMessage>[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            handlers[invocationIndex] = new TestTypedRecordHandler<TestMessage>();
            
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop8-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var sqsEvent = TestEventFactory.CreateTypedSqsEvent<TestMessage>(recordsPerInvocation, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = handlers[invocationIndex];

                barrier.SignalAndWait();

                var processor = new TypedSqsBatchProcessor();
                var result = await processor.ProcessAsync<TestMessage>(sqsEvent, handler);

                results[invocationIndex] = new TypedInvocationResult
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordsPerInvocation,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result,
                    ProcessedMessages = handler.ProcessedMessages.ToList()
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Property: Each handler processes ONLY its own invocation's records
        for (int i = 0; i < concurrencyLevel; i++)
        {
            var result = results[i];
            var handler = handlers[i];
            
            Assert.NotNull(result.ActualResult);
            
            // Property check 1: Handler processed correct number of records
            Assert.Equal(recordsPerInvocation, handler.ProcessedCount);
            
            // Property check 2: All processed messages belong to this invocation
            foreach (var msg in handler.ProcessedMessages)
            {
                Assert.Equal(result.InvocationId, msg.InvocationId);
            }
            
            // Property check 3: No messages from other invocations
            var otherInvocationIds = results
                .Where(r => r.InvocationId != result.InvocationId)
                .Select(r => r.InvocationId)
                .ToHashSet();
            
            var foreignMessages = handler.ProcessedMessages
                .Where(m => otherInvocationIds.Contains(m.InvocationId))
                .ToList();
            
            Assert.Empty(foreignMessages);
        }
    }

    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 9: Error Handling Isolation**
    /// 
    /// Property: For any invocation experiencing deserialization errors, other concurrent invocations
    /// SHALL continue processing normally without being affected by the error.
    /// 
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(3, 8)]
    [InlineData(5, 6)]
    public async Task Property9_ErrorHandlingIsolation_ErrorsInOneInvocationDoNotAffectOthers(
        int concurrencyLevel, int recordsPerInvocation)
    {
        // Arrange - Half invocations will have errors, half will be clean
        var results = new TypedInvocationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop9-inv-{invocationIndex}-{Guid.NewGuid():N}";
                SQSEvent sqsEvent;
                int expectedFailures;
                bool hasErrors = invocationIndex % 2 == 1; // Odd invocations have errors
                
                if (hasErrors)
                {
                    // Create event with some invalid JSON records
                    var invalidCount = Math.Min(invocationIndex, recordsPerInvocation - 1);
                    sqsEvent = TestEventFactory.CreateMixedValidInvalidSqsEvent(recordsPerInvocation, invalidCount, invocationId);
                    expectedFailures = invalidCount;
                }
                else
                {
                    sqsEvent = TestEventFactory.CreateTypedSqsEvent<TestMessage>(recordsPerInvocation, invocationId);
                    expectedFailures = 0;
                }
                
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = new TestTypedRecordHandler<TestMessage>();

                barrier.SignalAndWait();

                var processor = new TypedSqsBatchProcessor();
                var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                var result = await processor.ProcessAsync<TestMessage>(sqsEvent, handler, null, processingOptions);

                results[invocationIndex] = new TypedInvocationResult
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordsPerInvocation,
                    ExpectedFailureCount = expectedFailures,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result,
                    ProcessedMessages = handler.ProcessedMessages.ToList(),
                    HasErrors = hasErrors
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Property: Errors in one invocation do not affect others
        foreach (var result in results)
        {
            Assert.NotNull(result.ActualResult);
            
            // Property check 1: Each invocation has correct total record count
            Assert.Equal(result.ExpectedRecordCount, result.ActualResult.BatchRecords.Count);
            
            // Property check 2: Each invocation has correct failure count
            Assert.Equal(result.ExpectedFailureCount, result.ActualResult.FailureRecords.Count);
            
            // Property check 3: Success count is correct
            var expectedSuccessCount = result.ExpectedRecordCount - result.ExpectedFailureCount;
            Assert.Equal(expectedSuccessCount, result.ActualResult.SuccessRecords.Count);
            
            // Property check 4: All failure IDs belong to this invocation
            foreach (var failure in result.ActualResult.BatchItemFailuresResponse.BatchItemFailures)
            {
                Assert.True(failure.ItemIdentifier.StartsWith(result.InvocationId),
                    $"Property violation: Failure ID {failure.ItemIdentifier} does not belong to invocation {result.InvocationId}");
            }
            
            // Property check 5: Clean invocations should have no failures
            if (!result.HasErrors)
            {
                Assert.Empty(result.ActualResult.FailureRecords);
            }
        }
    }
}

/// <summary>
/// Test message type for typed batch processing tests.
/// </summary>
public class TestMessage
{
    [System.Text.Json.Serialization.JsonPropertyName("invocationId")]
    public string InvocationId { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("index")]
    public int Index { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Result container for typed batch processing invocation tests.
/// </summary>
public class TypedInvocationResult
{
    public string InvocationId { get; set; } = string.Empty;
    public int ExpectedRecordCount { get; set; }
    public int ExpectedFailureCount { get; set; }
    public HashSet<string> ExpectedRecordIds { get; set; } = new();
    public ProcessingResult<SQSEvent.SQSMessage>? ActualResult { get; set; }
    public List<TestMessage> ProcessedMessages { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool HasErrors { get; set; }
}

/// <summary>
/// Typed record handler for testing that tracks processed messages.
/// </summary>
/// <typeparam name="T">The type of message to handle.</typeparam>
public class TestTypedRecordHandler<T> : ITypedRecordHandler<T> where T : class
{
    private int _processedCount;
    private readonly List<T> _processedMessages = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the number of records processed.
    /// </summary>
    public int ProcessedCount => _processedCount;

    /// <summary>
    /// Gets the list of processed messages.
    /// </summary>
    public IReadOnlyList<T> ProcessedMessages
    {
        get
        {
            lock (_lock)
            {
                return _processedMessages.ToList();
            }
        }
    }

    /// <summary>
    /// Gets or sets a function that determines if a record should fail.
    /// </summary>
    public Func<T, bool>? ShouldFail { get; set; }

    /// <summary>
    /// Gets or sets the processing delay to simulate work.
    /// </summary>
    public TimeSpan ProcessingDelay { get; set; } = TimeSpan.Zero;

    /// <inheritdoc />
    public async Task<RecordHandlerResult> HandleAsync(T data, CancellationToken cancellationToken)
    {
        if (ProcessingDelay > TimeSpan.Zero)
        {
            await Task.Delay(ProcessingDelay, cancellationToken);
        }

        if (ShouldFail?.Invoke(data) == true)
        {
            throw new InvalidOperationException($"Simulated failure for record");
        }

        lock (_lock)
        {
            _processedMessages.Add(data);
        }
        Interlocked.Increment(ref _processedCount);
        
        return await Task.FromResult(RecordHandlerResult.None);
    }
}
