using System.Collections.Concurrent;
using System.Diagnostics;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing;

/// <summary>
/// Stress tests for validating AWS Lambda Powertools Batch Processing utility
/// under high-concurrency and sustained load conditions.
/// 
/// These tests verify that the batch processing utility maintains correctness
/// when subjected to:
/// - High concurrency levels (many simultaneous invocations)
/// - High iteration counts per thread (sustained load)
/// - Mixed processing patterns (parallel and sequential)
/// 
/// Requirements: 4.1, 4.2
/// </summary>
[Collection("BatchProcessing Concurrency Tests")]
public class BatchProcessingStressTests
{
    #region Helper Classes

    private class StressTestResult
    {
        public string ThreadId { get; set; } = string.Empty;
        public int ThreadIndex { get; set; }
        public int TotalIterations { get; set; }
        public int SuccessfulIterations { get; set; }
        public int FailedIterations { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }
        public double AverageIterationMs => TotalIterations > 0 
            ? TotalDuration.TotalMilliseconds / TotalIterations 
            : 0;
    }

    private class IterationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int ExpectedRecordCount { get; set; }
        public int ActualRecordCount { get; set; }
        public int ExpectedSuccessCount { get; set; }
        public int ActualSuccessCount { get; set; }
        public int ExpectedFailureCount { get; set; }
        public int ActualFailureCount { get; set; }
        public bool IsCorrect => 
            ExpectedRecordCount == ActualRecordCount &&
            ExpectedSuccessCount == ActualSuccessCount &&
            ExpectedFailureCount == ActualFailureCount;
        public string? Error { get; set; }
    }

    #endregion

    #region High Concurrency Tests (Task 7.3)

    /// <summary>
    /// Stress test with high concurrency levels to verify correctness under load.
    /// Requirements: 4.1
    /// </summary>
    [Theory]
    [InlineData(20, 10)]
    [InlineData(50, 5)]
    [InlineData(100, 3)]
    public async Task HighConcurrency_ShouldMaintainCorrectness(int concurrencyLevel, int recordsPerInvocation)
    {
        // Arrange
        var results = new ConcurrentBag<IterationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            var iterationResult = new IterationResult();
            
            try
            {
                var invocationId = $"highconc-{threadIndex}-{Guid.NewGuid():N}";
                var failureCount = threadIndex % recordsPerInvocation;
                var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);
                
                var handler = new TestSqsRecordHandler
                {
                    ShouldFail = msg =>
                    {
                        var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                        return msgIndex < failureCount;
                    }
                };

                iterationResult.InvocationId = invocationId;
                iterationResult.ExpectedRecordCount = recordsPerInvocation;
                iterationResult.ExpectedSuccessCount = recordsPerInvocation - failureCount;
                iterationResult.ExpectedFailureCount = failureCount;

                barrier.SignalAndWait();

                var processor = new SqsBatchProcessor();
                var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                var result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                iterationResult.ActualRecordCount = result.BatchRecords.Count;
                iterationResult.ActualSuccessCount = result.SuccessRecords.Count;
                iterationResult.ActualFailureCount = result.FailureRecords.Count;

                // Verify record ownership
                foreach (var record in result.BatchRecords)
                {
                    if (!record.MessageId.StartsWith(invocationId))
                    {
                        iterationResult.Error = $"Foreign record detected: {record.MessageId}";
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                iterationResult.Error = $"{ex.GetType().Name}: {ex.Message}";
                exceptions.Add(ex);
            }

            results.Add(iterationResult);
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrencyLevel, results.Count);
        Assert.Empty(exceptions);
        
        var incorrectResults = results.Where(r => !r.IsCorrect || r.Error != null).ToList();
        Assert.Empty(incorrectResults);
    }

    /// <summary>
    /// Stress test with high iteration counts per thread to verify sustained correctness.
    /// Requirements: 4.2
    /// </summary>
    [Theory]
    [InlineData(5, 50)]
    [InlineData(10, 30)]
    [InlineData(20, 20)]
    public async Task HighIterationCount_ShouldMaintainCorrectness(int concurrencyLevel, int iterationsPerThread)
    {
        // Arrange
        var threadResults = new StressTestResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new StressTestResult
                {
                    ThreadId = $"thread-{threadIndex}",
                    ThreadIndex = threadIndex,
                    TotalIterations = iterationsPerThread
                };

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    barrier.SignalAndWait();

                    for (int iteration = 0; iteration < iterationsPerThread; iteration++)
                    {
                        var invocationId = $"highiter-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
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

                        var processor = new SqsBatchProcessor();
                        var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                        var processingResult = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                        // Verify correctness
                        var actualRecordIds = processingResult.BatchRecords
                            .Select(r => r.MessageId)
                            .ToHashSet();

                        if (processingResult.BatchRecords.Count != recordCount ||
                            processingResult.SuccessRecords.Count != recordCount - failureCount ||
                            processingResult.FailureRecords.Count != failureCount ||
                            !actualRecordIds.SetEquals(expectedRecordIds))
                        {
                            result.FailedIterations++;
                            result.Errors.Add($"Iteration {iteration}: Mismatch in results");
                        }
                        else
                        {
                            result.SuccessfulIterations++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Exception: {ex.GetType().Name}: {ex.Message}");
                }

                stopwatch.Stop();
                result.TotalDuration = stopwatch.Elapsed;
                threadResults[threadIndex] = result;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in threadResults)
        {
            Assert.NotNull(result);
            Assert.Equal(iterationsPerThread, result.SuccessfulIterations + result.FailedIterations);
            Assert.Equal(0, result.FailedIterations);
            Assert.Empty(result.Errors);
        }
    }

    /// <summary>
    /// Stress test combining high concurrency with high iterations.
    /// Requirements: 4.1, 4.2
    /// </summary>
    [Theory]
    [InlineData(10, 20, 5)]
    [InlineData(20, 10, 4)]
    public async Task CombinedHighConcurrencyAndIterations_ShouldMaintainCorrectness(
        int concurrencyLevel, int iterationsPerThread, int recordsPerInvocation)
    {
        // Arrange
        var allIterationResults = new ConcurrentBag<IterationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var totalExpectedIterations = concurrencyLevel * iterationsPerThread;

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            barrier.SignalAndWait();

            for (int iteration = 0; iteration < iterationsPerThread; iteration++)
            {
                var iterationResult = new IterationResult();
                
                try
                {
                    var invocationId = $"combined-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
                    var failureCount = (threadIndex + iteration) % recordsPerInvocation;
                    var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);

                    var handler = new TestSqsRecordHandler
                    {
                        ShouldFail = msg =>
                        {
                            var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                            return msgIndex < failureCount;
                        }
                    };

                    iterationResult.InvocationId = invocationId;
                    iterationResult.ExpectedRecordCount = recordsPerInvocation;
                    iterationResult.ExpectedSuccessCount = recordsPerInvocation - failureCount;
                    iterationResult.ExpectedFailureCount = failureCount;

                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions { ThrowOnFullBatchFailure = false };
                    var result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                    iterationResult.ActualRecordCount = result.BatchRecords.Count;
                    iterationResult.ActualSuccessCount = result.SuccessRecords.Count;
                    iterationResult.ActualFailureCount = result.FailureRecords.Count;

                    // Verify record ownership
                    foreach (var record in result.BatchRecords)
                    {
                        if (!record.MessageId.StartsWith(invocationId))
                        {
                            iterationResult.Error = $"Foreign record: {record.MessageId}";
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    iterationResult.Error = $"{ex.GetType().Name}: {ex.Message}";
                }

                allIterationResults.Add(iterationResult);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(totalExpectedIterations, allIterationResults.Count);
        
        var incorrectResults = allIterationResults.Where(r => !r.IsCorrect || r.Error != null).ToList();
        Assert.Empty(incorrectResults);
    }

    #endregion

    #region Parallel Processing Stress Tests

    /// <summary>
    /// Stress test with parallel processing enabled under high concurrency.
    /// Requirements: 4.1, 4.2
    /// </summary>
    [Theory]
    [InlineData(10, 10, 8, 4)]
    [InlineData(20, 5, 6, 2)]
    public async Task ParallelProcessingUnderStress_ShouldMaintainCorrectness(
        int concurrencyLevel, int iterationsPerThread, int recordsPerInvocation, int maxDegreeOfParallelism)
    {
        // Arrange
        var allIterationResults = new ConcurrentBag<IterationResult>();
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            barrier.SignalAndWait();

            for (int iteration = 0; iteration < iterationsPerThread; iteration++)
            {
                var iterationResult = new IterationResult();
                
                try
                {
                    var invocationId = $"parallelstress-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
                    var failureCount = (threadIndex + iteration) % recordsPerInvocation;
                    var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);

                    var handler = new TestSqsRecordHandler
                    {
                        ProcessingDelay = TimeSpan.FromMilliseconds(2),
                        ShouldFail = msg =>
                        {
                            var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                            return msgIndex < failureCount;
                        }
                    };

                    iterationResult.InvocationId = invocationId;
                    iterationResult.ExpectedRecordCount = recordsPerInvocation;
                    iterationResult.ExpectedSuccessCount = recordsPerInvocation - failureCount;
                    iterationResult.ExpectedFailureCount = failureCount;

                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions
                    {
                        BatchParallelProcessingEnabled = true,
                        MaxDegreeOfParallelism = maxDegreeOfParallelism,
                        ThrowOnFullBatchFailure = false
                    };
                    var result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                    iterationResult.ActualRecordCount = result.BatchRecords.Count;
                    iterationResult.ActualSuccessCount = result.SuccessRecords.Count;
                    iterationResult.ActualFailureCount = result.FailureRecords.Count;

                    // Verify record ownership
                    foreach (var record in result.BatchRecords)
                    {
                        if (!record.MessageId.StartsWith(invocationId))
                        {
                            iterationResult.Error = $"Foreign record: {record.MessageId}";
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    iterationResult.Error = $"{ex.GetType().Name}: {ex.Message}";
                }

                allIterationResults.Add(iterationResult);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        var totalExpectedIterations = concurrencyLevel * iterationsPerThread;
        Assert.Equal(totalExpectedIterations, allIterationResults.Count);
        
        var incorrectResults = allIterationResults.Where(r => !r.IsCorrect || r.Error != null).ToList();
        Assert.Empty(incorrectResults);
    }

    #endregion

    #region Mixed Pattern Stress Tests

    /// <summary>
    /// Stress test with mixed sequential and parallel processing patterns.
    /// Requirements: 4.1, 4.2
    /// </summary>
    [Theory]
    [InlineData(10, 15, 6)]
    [InlineData(20, 10, 5)]
    public async Task MixedProcessingPatterns_ShouldMaintainCorrectness(
        int concurrencyLevel, int iterationsPerThread, int recordsPerInvocation)
    {
        // Arrange
        var allIterationResults = new ConcurrentBag<IterationResult>();
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel).Select(threadIndex => Task.Run(async () =>
        {
            barrier.SignalAndWait();

            for (int iteration = 0; iteration < iterationsPerThread; iteration++)
            {
                var iterationResult = new IterationResult();
                var useParallel = (threadIndex + iteration) % 2 == 0;
                
                try
                {
                    var invocationId = $"mixed-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
                    var failureCount = (threadIndex + iteration) % recordsPerInvocation;
                    var sqsEvent = TestEventFactory.CreateSqsEvent(recordsPerInvocation, invocationId);

                    var handler = new TestSqsRecordHandler
                    {
                        ProcessingDelay = useParallel ? TimeSpan.FromMilliseconds(1) : TimeSpan.Zero,
                        ShouldFail = msg =>
                        {
                            var msgIndex = int.Parse(msg.MessageId.Split("-msg-")[1]);
                            return msgIndex < failureCount;
                        }
                    };

                    iterationResult.InvocationId = invocationId;
                    iterationResult.ExpectedRecordCount = recordsPerInvocation;
                    iterationResult.ExpectedSuccessCount = recordsPerInvocation - failureCount;
                    iterationResult.ExpectedFailureCount = failureCount;

                    var processor = new SqsBatchProcessor();
                    var processingOptions = new ProcessingOptions
                    {
                        BatchParallelProcessingEnabled = useParallel,
                        MaxDegreeOfParallelism = useParallel ? 4 : 1,
                        ThrowOnFullBatchFailure = false
                    };
                    var result = await processor.ProcessAsync(sqsEvent, handler, processingOptions);

                    iterationResult.ActualRecordCount = result.BatchRecords.Count;
                    iterationResult.ActualSuccessCount = result.SuccessRecords.Count;
                    iterationResult.ActualFailureCount = result.FailureRecords.Count;

                    // Verify record ownership
                    foreach (var record in result.BatchRecords)
                    {
                        if (!record.MessageId.StartsWith(invocationId))
                        {
                            iterationResult.Error = $"Foreign record: {record.MessageId}";
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    iterationResult.Error = $"{ex.GetType().Name}: {ex.Message}";
                }

                allIterationResults.Add(iterationResult);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        var totalExpectedIterations = concurrencyLevel * iterationsPerThread;
        Assert.Equal(totalExpectedIterations, allIterationResults.Count);
        
        var incorrectResults = allIterationResults.Where(r => !r.IsCorrect || r.Error != null).ToList();
        Assert.Empty(incorrectResults);
    }

    #endregion

    #region Sustained Load Tests

    /// <summary>
    /// Sustained load test to verify no degradation over time.
    /// Requirements: 4.2
    /// </summary>
    [Theory]
    [InlineData(5, 100)]
    [InlineData(10, 50)]
    public async Task SustainedLoad_ShouldNotDegrade(int concurrencyLevel, int iterationsPerThread)
    {
        // Arrange
        var threadResults = new StressTestResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);

        // Act
        var tasks = new Task[concurrencyLevel];
        for (int i = 0; i < concurrencyLevel; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = new StressTestResult
                {
                    ThreadId = $"sustained-{threadIndex}",
                    ThreadIndex = threadIndex,
                    TotalIterations = iterationsPerThread
                };

                var iterationTimes = new List<double>();
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    barrier.SignalAndWait();

                    for (int iteration = 0; iteration < iterationsPerThread; iteration++)
                    {
                        var iterationStopwatch = Stopwatch.StartNew();
                        
                        var invocationId = $"sustained-t{threadIndex}-i{iteration}-{Guid.NewGuid():N}";
                        var recordCount = 4;
                        var sqsEvent = TestEventFactory.CreateSqsEvent(recordCount, invocationId);
                        var handler = new TestSqsRecordHandler();

                        var processor = new SqsBatchProcessor();
                        var processingResult = await processor.ProcessAsync(sqsEvent, handler);

                        iterationStopwatch.Stop();
                        iterationTimes.Add(iterationStopwatch.Elapsed.TotalMilliseconds);

                        // Verify correctness
                        if (processingResult.BatchRecords.Count == recordCount &&
                            processingResult.SuccessRecords.Count == recordCount)
                        {
                            result.SuccessfulIterations++;
                        }
                        else
                        {
                            result.FailedIterations++;
                            result.Errors.Add($"Iteration {iteration}: Incorrect result");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Exception: {ex.GetType().Name}: {ex.Message}");
                }

                stopwatch.Stop();
                result.TotalDuration = stopwatch.Elapsed;
                threadResults[threadIndex] = result;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in threadResults)
        {
            Assert.NotNull(result);
            Assert.Equal(iterationsPerThread, result.SuccessfulIterations);
            Assert.Equal(0, result.FailedIterations);
            Assert.Empty(result.Errors);
        }

        // Verify no significant degradation (all threads completed)
        var totalIterations = threadResults.Sum(r => r.SuccessfulIterations);
        Assert.Equal(concurrencyLevel * iterationsPerThread, totalIterations);
    }

    #endregion
}
