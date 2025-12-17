using System.Collections.Concurrent;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing;

/// <summary>
/// Tests for validating thread-safety in AWS Lambda Powertools Batch Processing utility
/// under concurrent execution scenarios.
/// 
/// These tests verify that when multiple Lambda invocations run concurrently (multi-instance mode),
/// the batch processing operations are thread-safe and don't cause exceptions or data corruption.
/// </summary>
[Collection("BatchProcessing Concurrency Tests")]
public class BatchProcessingThreadSafetyTests
{
    #region Helper Classes

    private class ThreadSafetyResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public int OperationsAttempted { get; set; }
        public int OperationsCompleted { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionType { get; set; }
        public ProcessingResult<SQSEvent.SQSMessage>? Result { get; set; }
    }

    private class DataIntegrityResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public int ExpectedRecordCount { get; set; }
        public int ActualRecordCount { get; set; }
        public int ExpectedSuccessCount { get; set; }
        public int ActualSuccessCount { get; set; }
        public int ExpectedFailureCount { get; set; }
        public int ActualFailureCount { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public HashSet<string> ExpectedRecordIds { get; set; } = new();
        public HashSet<string> ActualRecordIds { get; set; } = new();
        public bool HasCorrectRecords => ActualRecordIds.SetEquals(ExpectedRecordIds);
    }

    private class ParallelProcessingResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public int ExpectedRecordCount { get; set; }
        public int ExpectedSuccessCount { get; set; }
        public int ExpectedFailureCount { get; set; }
        public ProcessingResult<SQSEvent.SQSMessage>? Result { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    #endregion

    #region Basic Thread-Safety Tests (Task 3.1)

    /// <summary>
    /// Verifies that concurrent access to the batch processor does not throw
    /// thread-safety related exceptions.
    /// Requirements: 2.1
    /// </summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(5, 20)]
    [InlineData(10, 10)]
    public async Task ConcurrentAccess_ShouldNotThrowThreadSafetyExceptions(
        int concurrencyLevel, int iterationsPerThread)
    {
        // Arrange
        var results = new ConcurrentBag<ThreadSafetyResult>();
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            var result = new ThreadSafetyResult
            {
                InvocationIndex = threadIndex,
                OperationsAttempted = iterationsPerThread
            };

            try
            {
                barrier.SignalAndWait();

                for (int iteration = 0; iteration < iterationsPerThread; iteration++)
                {
                    var invocationId = $"thread-{threadIndex}-iter-{iteration}-{Guid.NewGuid():N}";
                    result.InvocationId = invocationId;
                    
                    var sqsEvent = TestEventFactory.CreateSqsEvent(3, invocationId);
                    var handler = new TestSqsRecordHandler();

                    var processor = new SqsBatchProcessor();
                    var processingResult = await processor.ProcessAsync(sqsEvent, handler);

                    // Verify basic correctness
                    Assert.Equal(3, processingResult.BatchRecords.Count);
                    result.OperationsCompleted++;
                }
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                result.ExceptionType = ex.GetType().Name;
            }

            results.Add(result);
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.All(results, r => Assert.False(r.ExceptionThrown,
            $"Thread {r.InvocationIndex} threw {r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    /// <summary>
    /// Verifies that data integrity is maintained when multiple invocations
    /// simultaneously clear and populate ProcessingResult.
    /// Requirements: 2.2
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConcurrentProcessingResultClearing_ShouldMaintainDataIntegrity(int concurrencyLevel)
    {
        // Arrange
        var results = new DataIntegrityResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var recordCounts = Enumerable.Range(0, concurrencyLevel)
            .Select(i => 3 + i) // Different record counts: 3, 4, 5, etc.
            .ToArray();

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"integrity-{invocationIndex}-{Guid.NewGuid():N}";
                var recordCount = recordCounts[invocationIndex];
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);
                var failureCount = invocationIndex % recordCount; // Varying failure counts

                var handler = new TestSqsRecordHandler
                {
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < failureCount;
                    }
                };

                var result = new DataIntegrityResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    ExpectedRecordCount = recordCount,
                    ExpectedSuccessCount = recordCount - failureCount,
                    ExpectedFailureCount = failureCount,
                    ExpectedRecordIds = expectedRecordIds
                };

                try
                {
                    // Synchronize to maximize race condition potential
                    barrier.SignalAndWait();

                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                    var processingResult = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                    result.ActualRecordCount = processingResult.BatchRecords.Count;
                    result.ActualSuccessCount = processingResult.SuccessRecords.Count;
                    result.ActualFailureCount = processingResult.FailureRecords.Count;
                    result.ActualRecordIds = processingResult.BatchRecords
                        .Select(r => r.MessageId)
                        .ToHashSet();
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                results[invocationIndex] = result;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.False(result.ExceptionThrown, result.ExceptionMessage);
            Assert.Equal(result.ExpectedRecordCount, result.ActualRecordCount);
            Assert.Equal(result.ExpectedSuccessCount, result.ActualSuccessCount);
            Assert.Equal(result.ExpectedFailureCount, result.ActualFailureCount);
            Assert.True(result.HasCorrectRecords,
                $"Invocation {result.InvocationId}: Expected records {string.Join(",", result.ExpectedRecordIds)} " +
                $"but got {string.Join(",", result.ActualRecordIds)}");
        }
    }

    /// <summary>
    /// Verifies that parallel batch processing within an invocation works correctly
    /// while other invocations are also processing.
    /// Requirements: 2.3
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(3, 8)]
    [InlineData(5, 6)]
    public async Task ParallelProcessingWithConcurrentInvocations_ShouldTrackRecordsCorrectly(
        int concurrencyLevel, int recordsPerInvocation)
    {
        // Arrange
        var results = new ParallelProcessingResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"parallel-{invocationIndex}-{Guid.NewGuid():N}";
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);
                var failureCount = invocationIndex % recordsPerInvocation;

                var handler = new TestSqsRecordHandler
                {
                    ProcessingDelay = TimeSpan.FromMilliseconds(10), // Add delay to increase overlap
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < failureCount;
                    }
                };

                var result = new ParallelProcessingResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    ExpectedRecordCount = recordsPerInvocation,
                    ExpectedSuccessCount = recordsPerInvocation - failureCount,
                    ExpectedFailureCount = failureCount
                };

                try
                {
                    barrier.SignalAndWait();

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions
                    {
                        BatchParallelProcessingEnabled = true,
                        MaxDegreeOfParallelism = 4,
                        ThrowOnFullBatchFailure = false
                    };
                    
                    result.Result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);
                    stopwatch.Stop();
                    result.Duration = stopwatch.Elapsed;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                results[invocationIndex] = result;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.False(result.ExceptionThrown, result.ExceptionMessage);
            Assert.NotNull(result.Result);
            Assert.Equal(result.ExpectedRecordCount, result.Result.BatchRecords.Count);
            Assert.Equal(result.ExpectedSuccessCount, result.Result.SuccessRecords.Count);
            Assert.Equal(result.ExpectedFailureCount, result.Result.FailureRecords.Count);

            // Verify all records belong to this invocation
            foreach (var record in result.Result.BatchRecords)
            {
                Assert.True(record.MessageId.StartsWith(result.InvocationId),
                    $"Record {record.MessageId} does not belong to invocation {result.InvocationId}");
            }
        }
    }

    #endregion


    #region Property 4: Exception-Free Concurrent Access

    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 4: Exception-Free Concurrent Access**
    /// 
    /// Property: For any number of concurrent threads accessing the singleton batch processor,
    /// all operations SHALL complete without throwing thread-safety related exceptions
    /// (InvalidOperationException, ConcurrentModificationException, etc.).
    /// 
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Theory]
    [InlineData(2, 5, 3)]
    [InlineData(3, 10, 5)]
    [InlineData(5, 8, 4)]
    [InlineData(10, 5, 3)]
    public async Task Property4_ExceptionFreeConcurrentAccess_AllOperationsShouldComplete(
        int concurrencyLevel, int iterationsPerThread, int recordsPerIteration)
    {
        // Arrange
        var results = new ConcurrentBag<ThreadSafetyResult>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            var result = new ThreadSafetyResult
            {
                InvocationIndex = threadIndex,
                OperationsAttempted = iterationsPerThread
            };

            try
            {
                barrier.SignalAndWait();

                for (int iteration = 0; iteration < iterationsPerThread; iteration++)
                {
                    var invocationId = $"prop4-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
                    result.InvocationId = invocationId;

                    var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerIteration, invocationId);
                    var handler = new TestSqsRecordHandler();

                    var processor = new SqsBatchProcessor();
                    var processingResult = await processor.ProcessAsync(sqsEvent, handler);

                    // Property check: Result should have correct record count
                    Assert.Equal(recordsPerIteration, processingResult.BatchRecords.Count);
                    
                    // Property check: All records should be successes (no failures configured)
                    Assert.Equal(recordsPerIteration, processingResult.SuccessRecords.Count);
                    Assert.Empty(processingResult.FailureRecords);

                    result.OperationsCompleted++;
                }
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                result.ExceptionType = ex.GetType().Name;
                exceptions.Add(ex);
            }

            results.Add(result);
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert - Property: No thread-safety exceptions should be thrown
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.Empty(exceptions);
        Assert.All(results, r => Assert.False(r.ExceptionThrown,
            $"Property violation: Thread {r.InvocationIndex} threw {r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    /// <summary>
    /// Additional test for Property 4 with mixed operations (success and failure).
    /// </summary>
    [Theory]
    [InlineData(3, 10)]
    [InlineData(5, 15)]
    [InlineData(8, 8)]
    public async Task Property4_ExceptionFreeConcurrentAccess_MixedOperations_ShouldComplete(
        int concurrencyLevel, int iterationsPerThread)
    {
        // Arrange
        var results = new ConcurrentBag<ThreadSafetyResult>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(concurrencyLevel);
        var random = new Random();

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            var result = new ThreadSafetyResult
            {
                InvocationIndex = threadIndex,
                OperationsAttempted = iterationsPerThread
            };

            try
            {
                barrier.SignalAndWait();

                for (int iteration = 0; iteration < iterationsPerThread; iteration++)
                {
                    var invocationId = $"prop4mix-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
                    var recordCount = random.Next(2, 8);
                    var failureCount = random.Next(0, recordCount);

                    var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                    var handler = new TestSqsRecordHandler
                    {
                        ShouldFail = msg =>
                        {
                            var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                            return msgIndex < failureCount;
                        }
                    };

                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                    var processingResult = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                    // Property check: Total records should match
                    Assert.Equal(recordCount, processingResult.BatchRecords.Count);
                    
                    // Property check: Success + Failure should equal total
                    Assert.Equal(recordCount, 
                        processingResult.SuccessRecords.Count + processingResult.FailureRecords.Count);

                    result.OperationsCompleted++;
                }
            }
            catch (Exception ex)
            {
                result.ExceptionThrown = true;
                result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                result.ExceptionType = ex.GetType().Name;
                exceptions.Add(ex);
            }

            results.Add(result);
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.Empty(exceptions);
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.Equal(r.OperationsAttempted, r.OperationsCompleted));
    }

    #endregion

    #region Property 5: Data Integrity Under Concurrent Clear

    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 5: Data Integrity Under Concurrent Clear**
    /// 
    /// Property: For any set of invocations that simultaneously begin ProcessAsync
    /// (triggering ProcessingResult clearing), each invocation SHALL maintain data integrity
    /// in its final result.
    /// 
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Theory]
    [InlineData(2, 3, 7)]
    [InlineData(3, 4, 8)]
    [InlineData(5, 3, 6)]
    [InlineData(10, 2, 5)]
    public async Task Property5_DataIntegrityUnderConcurrentClear_EachInvocationMaintainsIntegrity(
        int concurrencyLevel, int minRecords, int maxRecords)
    {
        // Arrange
        var random = new Random();
        var recordCounts = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => random.Next(minRecords, maxRecords + 1))
            .ToArray();
        var failureCounts = recordCounts
            .Select(rc => random.Next(0, rc))
            .ToArray();

        var results = new DataIntegrityResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop5-{invocationIndex}-{Guid.NewGuid():N}";
                var recordCount = recordCounts[invocationIndex];
                var failureCount = failureCounts[invocationIndex];
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);

                var handler = new TestSqsRecordHandler
                {
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < failureCount;
                    }
                };

                var result = new DataIntegrityResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    ExpectedRecordCount = recordCount,
                    ExpectedSuccessCount = recordCount - failureCount,
                    ExpectedFailureCount = failureCount,
                    ExpectedRecordIds = expectedRecordIds
                };

                try
                {
                    // Synchronize to maximize concurrent clearing
                    barrier.SignalAndWait();

                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                    var processingResult = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                    result.ActualRecordCount = processingResult.BatchRecords.Count;
                    result.ActualSuccessCount = processingResult.SuccessRecords.Count;
                    result.ActualFailureCount = processingResult.FailureRecords.Count;
                    result.ActualRecordIds = processingResult.BatchRecords
                        .Select(r => r.MessageId)
                        .ToHashSet();
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                results[invocationIndex] = result;
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Property: Each invocation maintains data integrity
        foreach (var result in results)
        {
            // Property check 1: No exceptions
            Assert.False(result.ExceptionThrown,
                $"Property violation: Invocation {result.InvocationId} threw: {result.ExceptionMessage}");

            // Property check 2: Record count integrity
            Assert.Equal(result.ExpectedRecordCount, result.ActualRecordCount);

            // Property check 3: Success count integrity
            Assert.Equal(result.ExpectedSuccessCount, result.ActualSuccessCount);

            // Property check 4: Failure count integrity
            Assert.Equal(result.ExpectedFailureCount, result.ActualFailureCount);

            // Property check 5: Record identity integrity
            Assert.True(result.HasCorrectRecords,
                $"Property violation: Invocation {result.InvocationId} has incorrect records. " +
                $"Expected: {string.Join(",", result.ExpectedRecordIds)}, " +
                $"Actual: {string.Join(",", result.ActualRecordIds)}");

            // Property check 6: No foreign records
            var foreignRecords = result.ActualRecordIds.Except(result.ExpectedRecordIds).ToList();
            Assert.Empty(foreignRecords);
        }
    }

    /// <summary>
    /// Additional test for Property 5 with rapid successive invocations.
    /// </summary>
    [Theory]
    [InlineData(5, 20)]
    [InlineData(10, 15)]
    public async Task Property5_DataIntegrityUnderConcurrentClear_RapidSuccessiveInvocations(
        int concurrencyLevel, int iterationsPerThread)
    {
        // Arrange
        var allResults = new ConcurrentBag<DataIntegrityResult>();
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            barrier.SignalAndWait();

            for (int iteration = 0; iteration < iterationsPerThread; iteration++)
            {
                var invocationId = $"prop5rapid-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
                var recordCount = 3 + (iteration % 5);
                var failureCount = iteration % recordCount;
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                var expectedRecordIds = TestEventFactory.GetSqsMessageIds(sqsEvent);

                var handler = new TestSqsRecordHandler
                {
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < failureCount;
                    }
                };

                var result = new DataIntegrityResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = threadIndex,
                    ExpectedRecordCount = recordCount,
                    ExpectedSuccessCount = recordCount - failureCount,
                    ExpectedFailureCount = failureCount,
                    ExpectedRecordIds = expectedRecordIds
                };

                try
                {
                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                    var processingResult = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                    result.ActualRecordCount = processingResult.BatchRecords.Count;
                    result.ActualSuccessCount = processingResult.SuccessRecords.Count;
                    result.ActualFailureCount = processingResult.FailureRecords.Count;
                    result.ActualRecordIds = processingResult.BatchRecords
                        .Select(r => r.MessageId)
                        .ToHashSet();
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                allResults.Add(result);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel * iterationsPerThread, allResults.Count);
        Assert.All(allResults, r =>
        {
            Assert.False(r.ExceptionThrown, r.ExceptionMessage);
            Assert.Equal(r.ExpectedRecordCount, r.ActualRecordCount);
            Assert.Equal(r.ExpectedSuccessCount, r.ActualSuccessCount);
            Assert.Equal(r.ExpectedFailureCount, r.ActualFailureCount);
            Assert.True(r.HasCorrectRecords);
        });
    }

    #endregion

    #region Property 6: Parallel Processing with Concurrent Invocations

    /// <summary>
    /// **Feature: batch-processing-multi-instance-validation, Property 6: Parallel Processing with Concurrent Invocations**
    /// 
    /// Property: For any invocation using BatchParallelProcessingEnabled=true while other invocations
    /// are also processing, the parallel invocation SHALL correctly track all success and failure records.
    /// 
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Theory]
    [InlineData(2, 5, 2)]
    [InlineData(3, 8, 3)]
    [InlineData(5, 6, 4)]
    [InlineData(8, 4, 2)]
    public async Task Property6_ParallelProcessingWithConcurrentInvocations_CorrectlyTracksRecords(
        int concurrencyLevel, int recordsPerInvocation, int maxDegreeOfParallelism)
    {
        // Arrange
        var random = new Random();
        var failureCounts = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => random.Next(0, recordsPerInvocation))
            .ToArray();

        var results = new ParallelProcessingResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop6-{invocationIndex}-{Guid.NewGuid():N}";
                var failureCount = failureCounts[invocationIndex];
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);

                var handler = new TestSqsRecordHandler
                {
                    ProcessingDelay = TimeSpan.FromMilliseconds(5), // Add delay to increase parallel overlap
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < failureCount;
                    }
                };

                var result = new ParallelProcessingResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    ExpectedRecordCount = recordsPerInvocation,
                    ExpectedSuccessCount = recordsPerInvocation - failureCount,
                    ExpectedFailureCount = failureCount
                };

                try
                {
                    barrier.SignalAndWait();

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions
                    {
                        BatchParallelProcessingEnabled = true,
                        MaxDegreeOfParallelism = maxDegreeOfParallelism,
                        ThrowOnFullBatchFailure = false
                    };

                    result.Result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);
                    stopwatch.Stop();
                    result.Duration = stopwatch.Elapsed;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                results[invocationIndex] = result;
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Property: Each parallel invocation correctly tracks records
        foreach (var result in results)
        {
            // Property check 1: No exceptions
            Assert.False(result.ExceptionThrown,
                $"Property violation: Invocation {result.InvocationId} threw: {result.ExceptionMessage}");
            Assert.NotNull(result.Result);

            // Property check 2: Total record count is correct
            Assert.Equal(result.ExpectedRecordCount, result.Result.BatchRecords.Count);

            // Property check 3: Success count is correct
            Assert.Equal(result.ExpectedSuccessCount, result.Result.SuccessRecords.Count);

            // Property check 4: Failure count is correct
            Assert.Equal(result.ExpectedFailureCount, result.Result.FailureRecords.Count);

            // Property check 5: BatchItemFailures count matches failure count
            Assert.Equal(result.ExpectedFailureCount, 
                result.Result.BatchItemFailuresResponse.BatchItemFailures.Count);

            // Property check 6: All records belong to this invocation
            foreach (var record in result.Result.BatchRecords)
            {
                Assert.True(record.MessageId.StartsWith(result.InvocationId),
                    $"Property violation: Record {record.MessageId} does not belong to invocation {result.InvocationId}");
            }

            // Property check 7: All failure IDs belong to this invocation
            foreach (var failure in result.Result.BatchItemFailuresResponse.BatchItemFailures)
            {
                Assert.True(failure.ItemIdentifier.StartsWith(result.InvocationId),
                    $"Property violation: Failure {failure.ItemIdentifier} does not belong to invocation {result.InvocationId}");
            }
        }
    }

    /// <summary>
    /// Additional test for Property 6 with mixed parallel and sequential processing.
    /// </summary>
    [Theory]
    [InlineData(4, 6)]
    [InlineData(6, 8)]
    public async Task Property6_MixedParallelAndSequentialProcessing_AllInvocationsCorrect(
        int concurrencyLevel, int recordsPerInvocation)
    {
        // Arrange
        var results = new ParallelProcessingResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            bool useParallel = invocationIndex % 2 == 0; // Alternate between parallel and sequential
            
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = $"prop6mix-{invocationIndex}-{Guid.NewGuid():N}";
                var failureCount = invocationIndex % recordsPerInvocation;
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);

                var handler = new TestSqsRecordHandler
                {
                    ProcessingDelay = TimeSpan.FromMilliseconds(3),
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < failureCount;
                    }
                };

                var result = new ParallelProcessingResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    ExpectedRecordCount = recordsPerInvocation,
                    ExpectedSuccessCount = recordsPerInvocation - failureCount,
                    ExpectedFailureCount = failureCount
                };

                try
                {
                    barrier.SignalAndWait();

                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions
                    {
                        BatchParallelProcessingEnabled = useParallel,
                        MaxDegreeOfParallelism = useParallel ? 4 : 1,
                        ThrowOnFullBatchFailure = false
                    };

                    result.Result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                results[invocationIndex] = result;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.False(result.ExceptionThrown, result.ExceptionMessage);
            Assert.NotNull(result.Result);
            Assert.Equal(result.ExpectedRecordCount, result.Result.BatchRecords.Count);
            Assert.Equal(result.ExpectedSuccessCount, result.Result.SuccessRecords.Count);
            Assert.Equal(result.ExpectedFailureCount, result.Result.FailureRecords.Count);

            // Verify record ownership
            foreach (var record in result.Result.BatchRecords)
            {
                Assert.True(record.MessageId.StartsWith(result.InvocationId));
            }
        }
    }

    #endregion
}
