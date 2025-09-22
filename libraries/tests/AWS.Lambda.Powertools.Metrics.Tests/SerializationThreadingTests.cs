using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests
{
    public class SerializationThreadingTests : IDisposable
    {
        [Fact]
        public async Task Serialize_ConcurrentWithAddMetric_ShouldNotThrowArgumentOutOfRangeException()
        {
            var exceptions = new List<Exception>();
            var exceptionsLock = new object();
            var operationCount = 0;
            var serializationCount = 0;
            var cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                Metrics.ResetForTest();
                Metrics.SetNamespace("SerializationTest");
                
                // Create tasks that continuously add metrics
                var metricTasks = new List<Task>();
                for (int i = 0; i < 10; i++)
                {
                    int threadId = i;
                    metricTasks.Add(Task.Run(async () =>
                    {
                        while (!cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            try
                            {
                                // Add various types of metrics to create complex scenarios
                                Metrics.AddMetric("SharedMetric", threadId * 1.5, MetricUnit.Count);
                                Metrics.AddMetric($"ThreadMetric_{threadId}", threadId, MetricUnit.Milliseconds);
                                Metrics.AddMetric("AnotherSharedMetric", threadId * 2.0, MetricUnit.Bytes);
                                
                                // Add dimensions occasionally to make serialization more complex
                                if (threadId % 3 == 0)
                                {
                                    Metrics.AddDimension("ThreadId", threadId.ToString());
                                    Metrics.AddDimension("Operation", "Test");
                                }
                                
                                Interlocked.Increment(ref operationCount);
                                
                                // Small delay to allow other threads to interleave
                                await Task.Delay(1, cancellationTokenSource.Token);
                            }
                            catch (Exception ex) when (!(ex is OperationCanceledException))
                            {
                                lock (exceptionsLock)
                                {
                                    exceptions.Add(ex);
                                }
                                return;
                            }
                        }
                    }, cancellationTokenSource.Token));
                }
                
                // Create tasks that continuously serialize metrics
                var serializationTasks = new List<Task>();
                for (int i = 0; i < 5; i++)
                {
                    serializationTasks.Add(Task.Run(async () =>
                    {
                        while (!cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            try
                            {
                                // This is where the bug occurred - during serialization
                                // while other threads were modifying the collections
                                // Flush() internally calls Serialize() which is where the race condition happened
                                Metrics.Flush();
                                
                                Interlocked.Increment(ref serializationCount);
                                
                                // Small delay to allow metric additions to interleave
                                await Task.Delay(2, cancellationTokenSource.Token);
                            }
                            catch (Exception ex) when (!(ex is OperationCanceledException))
                            {
                                lock (exceptionsLock)
                                {
                                    exceptions.Add(ex);
                                }
                                return;
                            }
                        }
                    }, cancellationTokenSource.Token));
                }
                
                // Let the test run for a reasonable amount of time to stress test the system
                await Task.Delay(2000); // 2 seconds of concurrent operations
                
                // Stop all tasks
                cancellationTokenSource.Cancel();
                
                // Wait for all tasks to complete
                try
                {
                    await Task.WhenAll(metricTasks.Concat(serializationTasks));
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
            
            // Assert that no threading exceptions occurred
            lock (exceptionsLock)
            {
                if (exceptions.Count > 0)
                {
                    var firstException = exceptions[0];
                    
                    // Check specifically for the types of exceptions that indicate threading issues
                    if (firstException is ArgumentOutOfRangeException ||
                        firstException is InvalidOperationException ||
                        firstException is NullReferenceException ||
                        firstException.Message.Contains("Collection was modified") ||
                        firstException.Message.Contains("Index was out of range") ||
                        firstException.Message.Contains("enumeration"))
                    {
                        Assert.Fail(
                            $"Threading-related exception occurred during concurrent serialization: " +
                            $"{firstException.GetType().Name}: {firstException.Message}\n" +
                            $"Stack trace: {firstException.StackTrace}");
                    }
                    else
                    {
                        // Re-throw unexpected exceptions
                        throw firstException;
                    }
                }
            }
            
            // Verify that we actually performed concurrent operations
            Assert.True(operationCount > 100, 
                $"Expected many metric operations, but only got {operationCount}");
            Assert.True(serializationCount > 10, 
                $"Expected many serialization operations, but only got {serializationCount}");
        }
        
        [Fact]
        public async Task Flush_ConcurrentWithAddMetric_ShouldNotThrowArgumentOutOfRangeException()
        {
            var exceptions = new List<Exception>();
            var exceptionsLock = new object();
            var cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                Metrics.ResetForTest();
                Metrics.SetNamespace("FlushTest");
                
                var tasks = new List<Task>();
                
                // Tasks that add metrics
                for (int i = 0; i < 8; i++)
                {
                    int threadId = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        while (!cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            try
                            {
                                Metrics.AddMetric($"Metric_{threadId}", threadId, MetricUnit.Count);
                                await Task.Delay(5, cancellationTokenSource.Token);
                            }
                            catch (Exception ex) when (!(ex is OperationCanceledException))
                            {
                                lock (exceptionsLock)
                                {
                                    exceptions.Add(ex);
                                }
                                return;
                            }
                        }
                    }, cancellationTokenSource.Token));
                }
                
                // Tasks that flush metrics (which triggers serialization)
                for (int i = 0; i < 3; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        while (!cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            try
                            {
                                Metrics.Flush();
                                await Task.Delay(10, cancellationTokenSource.Token);
                            }
                            catch (Exception ex) when (!(ex is OperationCanceledException))
                            {
                                lock (exceptionsLock)
                                {
                                    exceptions.Add(ex);
                                }
                                return;
                            }
                        }
                    }, cancellationTokenSource.Token));
                }
                
                // Run for 1 second
                await Task.Delay(1000);
                cancellationTokenSource.Cancel();
                
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
            
            // Check for threading exceptions
            lock (exceptionsLock)
            {
                if (exceptions.Count > 0)
                {
                    var firstException = exceptions[0];
                    if (firstException is ArgumentOutOfRangeException ||
                        firstException is InvalidOperationException ||
                        firstException.Message.Contains("Index was out of range"))
                    {
                        Assert.Fail(
                            $"Threading exception in Flush: {firstException.GetType().Name}: {firstException.Message}");
                    }
                    else
                    {
                        throw firstException;
                    }
                }
            }
        }
        
        public void Dispose()
        {
            Metrics.ResetForTest();
        }
    }
}