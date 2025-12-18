using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing;

/// <summary>
/// Tests for validating DynamoDB Stream batch processor isolation under concurrent execution scenarios.
/// These tests verify that when multiple Lambda invocations run concurrently (multi-instance mode),
/// each invocation's ProcessingResult remains isolated from other invocations.
/// </summary>
[Collection("BatchProcessing Concurrency Tests")]
public class DynamoDbProcessorIsolationTests
{
    /// <summary>
    /// Verifies that concurrent invocations using the DynamoDbStreamBatchProcessor
    /// each receive their own ProcessingResult with the correct record count.
    /// Requirements: 1.1, 1.3, 2.1
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConcurrentInvocations_ShouldMaintainProcessingResultIsolation(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentInvocationResult<DynamoDBEvent.DynamodbStreamRecord>[concurrencyLevel];
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
                var invocationId = $"dynamodb-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var recordCount = recordCountsPerInvocation[invocationIndex];
                var dynamoDbEvent = TestEventFactory.CreateDynamoDbEvent(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetDynamoDbSequenceNumbers(dynamoDbEvent);
                var handler = new TestDynamoDbRecordHandler();

                // Synchronize all invocations to start at the same time
                barrier.SignalAndWait();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Create a new processor instance for each invocation to ensure isolation
                var processor = new TestDynamoDbStreamBatchProcessor();
                var result = await processor.ProcessAsync(dynamoDbEvent, handler);
                
                stopwatch.Stop();

                results[invocationIndex] = new ConcurrentInvocationResult<DynamoDBEvent.DynamodbStreamRecord>
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordCount,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result,
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
                .Select(r => r.Dynamodb.SequenceNumber)
                .ToHashSet();
            Assert.True(actualRecordIds.SetEquals(result.ExpectedRecordIds),
                $"Invocation {result.InvocationId}: Expected records {string.Join(",", result.ExpectedRecordIds)} " +
                $"but got {string.Join(",", actualRecordIds)}");
        }
    }

    /// <summary>
    /// Verifies that concurrent invocations with different success/failure ratios
    /// each receive the correct BatchItemFailures for their invocation.
    /// Requirements: 1.4
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConcurrentInvocations_ShouldMaintainBatchItemFailuresIsolation(int concurrencyLevel)
    {
        // Arrange
        var results = new ConcurrentInvocationResult<DynamoDBEvent.DynamodbStreamRecord>[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var recordCount = 5;

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"dynamodb-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var dynamoDbEvent = TestEventFactory.CreateDynamoDbEvent(recordCount, invocationId);
                var expectedFailureCount = invocationIndex % recordCount; // 0, 1, 2, 3, 4 failures
                
                var handler = new TestDynamoDbRecordHandler
                {
                    // Fail the first N records based on invocation index
                    ShouldFail = record => 
                    {
                        var seqParts = record.Dynamodb.SequenceNumber.Split("-seq-");
                        var recordIndex = int.Parse(seqParts[1]);
                        return recordIndex < expectedFailureCount;
                    }
                };

                barrier.SignalAndWait();

                var processor = new TestDynamoDbStreamBatchProcessor();
                var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                var result = await processor.ProcessAsync(dynamoDbEvent, handler, processingOptions);

                results[invocationIndex] = new ConcurrentInvocationResult<DynamoDBEvent.DynamodbStreamRecord>
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordCount,
                    ExpectedFailureCount = expectedFailureCount,
                    ActualResult = result
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result.ActualResult);
            Assert.Equal(result.ExpectedFailureCount, result.ActualResult.FailureRecords.Count);
            Assert.Equal(result.ExpectedFailureCount, 
                result.ActualResult.BatchItemFailuresResponse.BatchItemFailures.Count);
            
            // Verify all failure IDs belong to this invocation
            foreach (var failure in result.ActualResult.BatchItemFailuresResponse.BatchItemFailures)
            {
                Assert.True(failure.ItemIdentifier.StartsWith(result.InvocationId),
                    $"Failure ID {failure.ItemIdentifier} should start with invocation ID {result.InvocationId}");
            }
        }
    }

    /// <summary>
    /// Verifies that when one invocation completes while another is still processing,
    /// the completing invocation returns only its own results.
    /// Requirements: 1.2, 4.3
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task OverlappingInvocations_CompletingInvocationShouldReturnOnlyOwnResults(int shortDelayMs)
    {
        // Arrange
        var longDelayMs = shortDelayMs * 3;
        var shortInvocationResult = new ConcurrentInvocationResult<DynamoDBEvent.DynamodbStreamRecord>();
        var longInvocationResult = new ConcurrentInvocationResult<DynamoDBEvent.DynamodbStreamRecord>();
        var barrier = new Barrier(2);

        var shortRecordCount = 3;
        var longRecordCount = 5;

        // Act
        var shortTask = Task.Run(async () =>
        {
            var invocationId = $"dynamodb-short-{Guid.NewGuid():N}";
            var dynamoDbEvent = TestEventFactory.CreateDynamoDbEvent(shortRecordCount, invocationId);
            var expectedRecordIds = TestEventFactory.GetDynamoDbSequenceNumbers(dynamoDbEvent);
            var handler = new TestDynamoDbRecordHandler
            {
                ProcessingDelay = TimeSpan.FromMilliseconds(shortDelayMs)
            };

            barrier.SignalAndWait();

            var processor = new TestDynamoDbStreamBatchProcessor();
            var result = await processor.ProcessAsync(dynamoDbEvent, handler);

            shortInvocationResult.InvocationId = invocationId;
            shortInvocationResult.ExpectedRecordCount = shortRecordCount;
            shortInvocationResult.ExpectedRecordIds = expectedRecordIds;
            shortInvocationResult.ActualResult = result;
        });

        var longTask = Task.Run(async () =>
        {
            var invocationId = $"dynamodb-long-{Guid.NewGuid():N}";
            var dynamoDbEvent = TestEventFactory.CreateDynamoDbEvent(longRecordCount, invocationId);
            var expectedRecordIds = TestEventFactory.GetDynamoDbSequenceNumbers(dynamoDbEvent);
            var handler = new TestDynamoDbRecordHandler
            {
                ProcessingDelay = TimeSpan.FromMilliseconds(longDelayMs)
            };

            barrier.SignalAndWait();

            var processor = new TestDynamoDbStreamBatchProcessor();
            var result = await processor.ProcessAsync(dynamoDbEvent, handler);

            longInvocationResult.InvocationId = invocationId;
            longInvocationResult.ExpectedRecordCount = longRecordCount;
            longInvocationResult.ExpectedRecordIds = expectedRecordIds;
            longInvocationResult.ActualResult = result;
        });

        await Task.WhenAll(shortTask, longTask);

        // Assert - Short invocation should have only its own records
        Assert.NotNull(shortInvocationResult.ActualResult);
        Assert.Equal(shortRecordCount, shortInvocationResult.ActualResult.BatchRecords.Count);
        var shortActualIds = shortInvocationResult.ActualResult.BatchRecords
            .Select(r => r.Dynamodb.SequenceNumber)
            .ToHashSet();
        Assert.True(shortActualIds.SetEquals(shortInvocationResult.ExpectedRecordIds),
            "Short invocation should contain only its own records");

        // Assert - Long invocation should have only its own records
        Assert.NotNull(longInvocationResult.ActualResult);
        Assert.Equal(longRecordCount, longInvocationResult.ActualResult.BatchRecords.Count);
        var longActualIds = longInvocationResult.ActualResult.BatchRecords
            .Select(r => r.Dynamodb.SequenceNumber)
            .ToHashSet();
        Assert.True(longActualIds.SetEquals(longInvocationResult.ExpectedRecordIds),
            "Long invocation should contain only its own records");

        // Verify no cross-contamination
        Assert.False(shortActualIds.Overlaps(longInvocationResult.ExpectedRecordIds),
            "Short invocation should not contain long invocation's records");
        Assert.False(longActualIds.Overlaps(shortInvocationResult.ExpectedRecordIds),
            "Long invocation should not contain short invocation's records");
    }

    /// <summary>
    /// Verifies that concurrent access to the batch processor does not throw
    /// thread-safety related exceptions.
    /// Requirements: 2.1
    /// </summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(5, 20)]
    [InlineData(10, 10)]
    public async Task ConcurrentAccess_ShouldNotThrowThreadSafetyExceptions(int concurrencyLevel, int iterationsPerThread)
    {
        // Arrange
        var exceptions = new List<Exception>();
        var exceptionLock = new object();
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            try
            {
                barrier.SignalAndWait();

                for (int iteration = 0; iteration < iterationsPerThread; iteration++)
                {
                    var invocationId = $"dynamodb-thread-{threadIndex}-iter-{iteration}-{Guid.NewGuid():N}";
                    var dynamoDbEvent = TestEventFactory.CreateDynamoDbEvent(3, invocationId);
                    var handler = new TestDynamoDbRecordHandler();

                    var processor = new TestDynamoDbStreamBatchProcessor();
                    var result = await processor.ProcessAsync(dynamoDbEvent, handler);

                    // Verify basic correctness
                    Assert.Equal(3, result.BatchRecords.Count);
                }
            }
            catch (Exception ex)
            {
                lock (exceptionLock)
                {
                    exceptions.Add(new Exception($"Thread {threadIndex}: {ex.Message}", ex));
                }
            }
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
    }
}
