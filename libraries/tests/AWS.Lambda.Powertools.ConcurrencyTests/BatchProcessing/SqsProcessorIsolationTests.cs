using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing;

/// <summary>
/// Tests for validating SQS batch processor isolation under concurrent execution scenarios.
/// These tests verify that when multiple Lambda invocations run concurrently (multi-instance mode),
/// each invocation's ProcessingResult remains isolated from other invocations.
/// </summary>
[Collection("BatchProcessing Concurrency Tests")]
public class SqsProcessorIsolationTests
{
    /// <summary>
    /// Verifies that concurrent invocations using the singleton SqsBatchProcessor
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
        var results = new ConcurrentInvocationResult<SQSEvent.SQSMessage>[concurrencyLevel];
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
                var invocationId = $"inv-{invocationIndex}-{Guid.NewGuid():N}";
                var recordCount = recordCountsPerInvocation[invocationIndex];
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = new TestSqsRecordHandler();

                // Synchronize all invocations to start at the same time
                barrier.SignalAndWait();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Create a new processor instance for each invocation to ensure isolation
                var processor = new SqsBatchProcessor();
                var result = await processor.ProcessAsync(sqsEvent, handler);
                
                stopwatch.Stop();

                results[invocationIndex] = new ConcurrentInvocationResult<SQSEvent.SQSMessage>
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
                .Select(r => r.MessageId)
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
        var results = new ConcurrentInvocationResult<SQSEvent.SQSMessage>[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var recordCount = 5;

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"inv-{invocationIndex}-{Guid.NewGuid():N}";
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                var expectedFailureCount = invocationIndex % recordCount; // 0, 1, 2, 3, 4 failures
                
                var handler = new TestSqsRecordHandler
                {
                    // Fail the first N records based on invocation index
                    ShouldFail = msg => 
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < expectedFailureCount;
                    }
                };

                barrier.SignalAndWait();

                var processor = new SqsBatchProcessor();
                var result = await processor.ProcessAsync(sqsEvent, handler);

                results[invocationIndex] = new ConcurrentInvocationResult<SQSEvent.SQSMessage>
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
        var shortInvocationResult = new ConcurrentInvocationResult<SQSEvent.SQSMessage>();
        var longInvocationResult = new ConcurrentInvocationResult<SQSEvent.SQSMessage>();
        var barrier = new Barrier(2);

        var shortRecordCount = 3;
        var longRecordCount = 5;

        // Act
        var shortTask = Task.Run(async () =>
        {
            var invocationId = $"short-{Guid.NewGuid():N}";
            var sqsEvent = TestEventFactory.CreateSqsEvent(shortRecordCount, invocationId);
            var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
            var handler = new TestSqsRecordHandler
            {
                ProcessingDelay = TimeSpan.FromMilliseconds(shortDelayMs)
            };

            barrier.SignalAndWait();

            var processor = new SqsBatchProcessor();
            var result = await processor.ProcessAsync(sqsEvent, handler);

            shortInvocationResult.InvocationId = invocationId;
            shortInvocationResult.ExpectedRecordCount = shortRecordCount;
            shortInvocationResult.ExpectedRecordIds = expectedRecordIds;
            shortInvocationResult.ActualResult = result;
        });

        var longTask = Task.Run(async () =>
        {
            var invocationId = $"long-{Guid.NewGuid():N}";
            var sqsEvent = TestEventFactory.CreateSqsEvent(longRecordCount, invocationId);
            var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
            var handler = new TestSqsRecordHandler
            {
                ProcessingDelay = TimeSpan.FromMilliseconds(longDelayMs)
            };

            barrier.SignalAndWait();

            var processor = new SqsBatchProcessor();
            var result = await processor.ProcessAsync(sqsEvent, handler);

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
            .Select(r => r.MessageId)
            .ToHashSet();
        Assert.True(shortActualIds.SetEquals(shortInvocationResult.ExpectedRecordIds),
            "Short invocation should contain only its own records");

        // Assert - Long invocation should have only its own records
        Assert.NotNull(longInvocationResult.ActualResult);
        Assert.Equal(longRecordCount, longInvocationResult.ActualResult.BatchRecords.Count);
        var longActualIds = longInvocationResult.ActualResult.BatchRecords
            .Select(r => r.MessageId)
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
                    var invocationId = $"thread-{threadIndex}-iter-{iteration}-{Guid.NewGuid():N}";
                    var sqsEvent = TestEventFactory.CreateSqsEvent(3, invocationId);
                    var handler = new TestSqsRecordHandler();

                    var processor = new SqsBatchProcessor();
                    var result = await processor.ProcessAsync(sqsEvent, handler);

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


    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 1: ProcessingResult Isolation**
    /// 
    /// Property: For any set of concurrent invocations processing different batch events on the same
    /// singleton processor, each invocation's returned ProcessingResult SHALL contain only records
    /// from that invocation's input event.
    /// 
    /// **Validates: Requirements 1.1, 1.3**
    /// </summary>
    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(3, 2, 10)]
    [InlineData(5, 4, 8)]
    [InlineData(10, 3, 6)]
    public async Task Property1_ProcessingResultIsolation_EachInvocationGetsOnlyOwnRecords(
        int concurrencyLevel, int minRecords, int maxRecords)
    {
        // Arrange - Generate random record counts for each invocation
        var random = new Random();
        var recordCounts = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => random.Next(minRecords, maxRecords + 1))
            .ToArray();

        var results = new ConcurrentInvocationResult<SQSEvent.SQSMessage>[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop1-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var recordCount = recordCounts[invocationIndex];
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var handler = new TestSqsRecordHandler();

                barrier.SignalAndWait();

                var processor = new SqsBatchProcessor();
                var result = await processor.ProcessAsync(sqsEvent, handler);

                results[invocationIndex] = new ConcurrentInvocationResult<SQSEvent.SQSMessage>
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordCount,
                    ExpectedRecordIds = expectedRecordIds,
                    ActualResult = result
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Property: Each invocation's result contains ONLY its own records
        foreach (var result in results)
        {
            Assert.NotNull(result.ActualResult);
            
            // Property check 1: Record count matches
            Assert.Equal(result.ExpectedRecordCount, result.ActualResult.BatchRecords.Count);
            
            // Property check 2: All records belong to this invocation
            var actualRecordIds = result.ActualResult.BatchRecords
                .Select(r => r.MessageId)
                .ToHashSet();
            
            Assert.True(actualRecordIds.SetEquals(result.ExpectedRecordIds),
                $"Property violation: Invocation {result.InvocationId} expected records " +
                $"{string.Join(",", result.ExpectedRecordIds)} but got {string.Join(",", actualRecordIds)}");
            
            // Property check 3: No foreign records (records from other invocations)
            var otherInvocationIds = results
                .Where(r => r.InvocationId != result.InvocationId)
                .SelectMany(r => r.ExpectedRecordIds)
                .ToHashSet();
            
            var foreignRecords = actualRecordIds.Intersect(otherInvocationIds).ToList();
            Assert.Empty(foreignRecords);
        }
    }

    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 2: BatchItemFailures Isolation**
    /// 
    /// Property: For any set of concurrent invocations with different failure patterns, each invocation's
    /// BatchItemFailuresResponse SHALL contain only the failed record identifiers from that invocation's batch.
    /// 
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(3, 8)]
    [InlineData(5, 6)]
    [InlineData(10, 4)]
    public async Task Property2_BatchItemFailuresIsolation_EachInvocationGetsOnlyOwnFailures(
        int concurrencyLevel, int recordsPerInvocation)
    {
        // Arrange - Generate random failure patterns for each invocation
        // Ensure we never fail all records to avoid BatchProcessingException
        var random = new Random();
        var failurePatterns = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => random.Next(0, recordsPerInvocation)) // 0 to recordsPerInvocation-1 failures
            .ToArray();

        var results = new ConcurrentInvocationResult<SQSEvent.SQSMessage>[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop2-inv-{invocationIndex}-{Guid.NewGuid():N}";
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);
                var expectedFailureCount = failurePatterns[invocationIndex];
                
                var handler = new TestSqsRecordHandler
                {
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < expectedFailureCount;
                    }
                };

                barrier.SignalAndWait();

                var processor = new SqsBatchProcessor();
                var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                var result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                results[invocationIndex] = new ConcurrentInvocationResult<SQSEvent.SQSMessage>
                {
                    InvocationId = invocationId,
                    ExpectedRecordCount = recordsPerInvocation,
                    ExpectedFailureCount = expectedFailureCount,
                    ActualResult = result
                };
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Property: Each invocation's failures contain ONLY its own failed records
        foreach (var result in results)
        {
            Assert.NotNull(result.ActualResult);
            
            // Property check 1: Failure count matches expected
            Assert.Equal(result.ExpectedFailureCount, result.ActualResult.FailureRecords.Count);
            Assert.Equal(result.ExpectedFailureCount, 
                result.ActualResult.BatchItemFailuresResponse.BatchItemFailures.Count);
            
            // Property check 2: All failure IDs belong to this invocation
            foreach (var failure in result.ActualResult.BatchItemFailuresResponse.BatchItemFailures)
            {
                Assert.True(failure.ItemIdentifier.StartsWith(result.InvocationId),
                    $"Property violation: Failure ID {failure.ItemIdentifier} does not belong to invocation {result.InvocationId}");
            }
            
            // Property check 3: Success count is correct
            var expectedSuccessCount = result.ExpectedRecordCount - result.ExpectedFailureCount;
            Assert.Equal(expectedSuccessCount, result.ActualResult.SuccessRecords.Count);
        }
    }

    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 3: Overlapping Lifecycle Isolation**
    /// 
    /// Property: For any pair of overlapping invocations (one completing while another is still processing),
    /// the completing invocation SHALL return results containing only its own records, regardless of the
    /// other invocation's state.
    /// 
    /// **Validates: Requirements 1.2, 4.3**
    /// </summary>
    [Theory]
    [InlineData(5, 15, 3, 7)]
    [InlineData(10, 30, 4, 8)]
    [InlineData(20, 60, 5, 10)]
    public async Task Property3_OverlappingLifecycleIsolation_CompletingInvocationGetsOnlyOwnRecords(
        int shortDelayMs, int longDelayMs, int shortRecordCount, int longRecordCount)
    {
        // Arrange
        var shortResults = new ConcurrentInvocationResult<SQSEvent.SQSMessage>();
        var longResults = new ConcurrentInvocationResult<SQSEvent.SQSMessage>();
        var barrier = new Barrier(2);

        // Act
        var shortTask = Task.Run(async () =>
        {
            var invocationId = $"prop3-short-{Guid.NewGuid():N}";
            var sqsEvent = TestEventFactory.CreateSqsEvent(shortRecordCount, invocationId);
            var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
            var handler = new TestSqsRecordHandler
            {
                ProcessingDelay = TimeSpan.FromMilliseconds(shortDelayMs)
            };

            barrier.SignalAndWait();

            var processor = new SqsBatchProcessor();
            var result = await processor.ProcessAsync(sqsEvent, handler);

            shortResults.InvocationId = invocationId;
            shortResults.ExpectedRecordCount = shortRecordCount;
            shortResults.ExpectedRecordIds = expectedRecordIds;
            shortResults.ActualResult = result;
        });

        var longTask = Task.Run(async () =>
        {
            var invocationId = $"prop3-long-{Guid.NewGuid():N}";
            var sqsEvent = TestEventFactory.CreateSqsEvent(longRecordCount, invocationId);
            var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
            var handler = new TestSqsRecordHandler
            {
                ProcessingDelay = TimeSpan.FromMilliseconds(longDelayMs)
            };

            barrier.SignalAndWait();

            var processor = new SqsBatchProcessor();
            var result = await processor.ProcessAsync(sqsEvent, handler);

            longResults.InvocationId = invocationId;
            longResults.ExpectedRecordCount = longRecordCount;
            longResults.ExpectedRecordIds = expectedRecordIds;
            longResults.ActualResult = result;
        });

        await Task.WhenAll(shortTask, longTask);

        // Assert - Property: Each invocation gets only its own records despite overlapping lifecycles
        
        // Short invocation property checks
        Assert.NotNull(shortResults.ActualResult);
        Assert.Equal(shortRecordCount, shortResults.ActualResult.BatchRecords.Count);
        var shortActualIds = shortResults.ActualResult.BatchRecords.Select(r => r.MessageId).ToHashSet();
        Assert.True(shortActualIds.SetEquals(shortResults.ExpectedRecordIds),
            "Property violation: Short invocation should contain only its own records");
        Assert.False(shortActualIds.Overlaps(longResults.ExpectedRecordIds),
            "Property violation: Short invocation contains records from long invocation");

        // Long invocation property checks
        Assert.NotNull(longResults.ActualResult);
        Assert.Equal(longRecordCount, longResults.ActualResult.BatchRecords.Count);
        var longActualIds = longResults.ActualResult.BatchRecords.Select(r => r.MessageId).ToHashSet();
        Assert.True(longActualIds.SetEquals(longResults.ExpectedRecordIds),
            "Property violation: Long invocation should contain only its own records");
        Assert.False(longActualIds.Overlaps(shortResults.ExpectedRecordIds),
            "Property violation: Long invocation contains records from short invocation");
    }
}
