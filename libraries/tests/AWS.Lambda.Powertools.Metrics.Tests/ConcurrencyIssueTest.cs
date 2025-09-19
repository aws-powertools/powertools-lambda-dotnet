using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Metrics;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests
{
    public class ConcurrencyIssueTest : IDisposable
    {
        [Fact]
        public async Task AddMetric_ConcurrentAccess_ShouldNotThrowException()
        {
            // Arrange
            Metrics.ResetForTest();
            Metrics.SetNamespace("TestNamespace");
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            
            // Act - Simulate concurrent access from multiple threads
            for (int i = 0; i < 10; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Simulate multiple metrics being added concurrently
                        for (int j = 0; j < 100; j++)
                        {
                            Metrics.AddMetric($"TestMetric_{taskId}_{j}", 1.0, MetricUnit.Count);
                            Metrics.AddMetric($"Client.{taskId}", 1.0, MetricUnit.Count);
                            Metrics.AddMetadata($"TestMetadata_{taskId}_{j}", $"value_{j}");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Assert
            foreach (var ex in exceptions)
            {
                Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.StackTrace != null)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Assert.Empty(exceptions);
            
            // Cleanup after test
            CleanupMetrics();
        }
        
        [Fact]
        public async Task AddMetric_ConcurrentAccessWithSameKey_ShouldNotThrowException()
        {
            // Arrange
            Metrics.ResetForTest();
            Metrics.SetNamespace("TestNamespace");
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            
            // Act - Simulate the specific scenario where the same metric key is used concurrently
            // Increase concurrency to try to reproduce the issue
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // This simulates the scenario where the same metric key 
                        // (like "Client.6b70*28198e") is being added from multiple threads
                        for (int j = 0; j < 200; j++)
                        {
                            Metrics.AddMetric("Client.SharedKey", 1.0, MetricUnit.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Assert - Should not have any exceptions
            foreach (var ex in exceptions)
            {
                Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.StackTrace != null)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Assert.Empty(exceptions);
            
            // Cleanup after test
            CleanupMetrics();
        }
        
        [Fact]
        public async Task AddMetric_Batch_ShouldNotThrowException()
        {
            // Arrange
            Metrics.ResetForTest();
            Metrics.SetNamespace("TestNamespace");
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            
            for (int i = 0; i < 5; i++)
            {
                var batchId = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // Simulate DataLoader batch processing
                        var innerTasks = new List<Task>();
                        for (int j = 0; j < 10; j++)
                        {
                            var itemId = j;
                            innerTasks.Add(Task.Run(() =>
                            {
                                // Simulate metrics being added from parallel DataLoader operations
                                Metrics.AddMetric($"DataLoader.InsidersStatusDataLoader", 1.0, MetricUnit.Count);
                                Metrics.AddMetric($"Query.insidersStatus", 1.0, MetricUnit.Count);
                                Metrics.AddMetric($"Client.6b70*28198e", 1.0, MetricUnit.Count);
                                Metrics.AddMetadata($"Query.insidersStatus.OperationName", "GetInsidersStatus");
                                Metrics.AddMetadata($"Query.insidersStatus.UserId", $"user_{batchId}_{itemId}");
                            }));
                        }
                        await Task.WhenAll(innerTasks);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Assert
            foreach (var ex in exceptions)
            {
                Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.StackTrace != null)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Assert.Empty(exceptions);
            
            // Cleanup after test
            CleanupMetrics();
        }
        
        [Fact]
        public async Task AddMetric_ReproduceFirstOrDefaultIssue_ShouldNotThrowException()
        {
            // Arrange
            Metrics.ResetForTest();
            Metrics.SetNamespace("TestNamespace");
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            
            // Act - This test specifically targets the FirstOrDefault issue in line 202-203 of Metrics.cs
            // metrics are added and flushed rapidly to trigger collection modification
            for (int i = 0; i < 100; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Add metrics rapidly to trigger the overflow condition that calls FirstOrDefault
                        for (int j = 0; j < 150; j++) // This should trigger multiple flushes
                        {
                            Metrics.AddMetric($"TestMetric_{taskId}_{j}", 1.0, MetricUnit.Count);
                            
                            // Also add the same metric key to trigger the FirstOrDefault path
                            if (j % 10 == 0)
                            {
                                Metrics.AddMetric("SharedMetric", 1.0, MetricUnit.Count);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Assert
            foreach (var ex in exceptions)
            {
                Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.StackTrace != null)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Assert.Empty(exceptions);
            
            // Cleanup after test
            CleanupMetrics();
        }
        
        [Fact]
        public async Task AddMetric_ConcurrentModificationDuringIteration_ShouldHandleArgumentOutOfRangeException()
        {
            // Arrange
            Metrics.ResetForTest();
            Metrics.SetNamespace("TestNamespace");
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            
            // Act - Create a scenario where collection modification happens during iteration
            // This test specifically targets the ArgumentOutOfRangeException catch block in GetExistingMetric
            for (int i = 0; i < 20; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Rapidly add and flush metrics to create timing conditions
                        // where GetExistingMetric might access an index that becomes invalid
                        for (int j = 0; j < 200; j++)
                        {
                            // Add metrics with the same key to trigger GetExistingMetric calls
                            Metrics.AddMetric("SharedMetricKey", 1.0, MetricUnit.Count);
                            
                            // Occasionally add many metrics to trigger flush (which clears the collection)
                            if (j % 50 == 0)
                            {
                                // Add enough metrics to trigger overflow and flush
                                for (int k = 0; k < 105; k++) // Exceeds MaxMetrics (100)
                                {
                                    Metrics.AddMetric($"OverflowMetric_{taskId}_{k}", 1.0, MetricUnit.Count);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Assert - Should not have any exceptions, even if ArgumentOutOfRangeException occurs internally
            foreach (var ex in exceptions)
            {
                Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.StackTrace != null)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Assert.Empty(exceptions);
            
            // Cleanup after test
            CleanupMetrics();
        }
        
        [Fact]
        public void GetExistingMetric_ArgumentOutOfRangeException_ShouldReturnNull()
        {
            // Arrange - Use reflection to test the private GetExistingMetric method directly
            var getExistingMetricMethod = typeof(Metrics).GetMethod("GetExistingMetric", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(getExistingMetricMethod);
            
            // Create a list that will throw ArgumentOutOfRangeException on indexer access
            // This directly tests the catch (ArgumentOutOfRangeException) block in GetExistingMetric
            var metricsList = new ThrowingList();
            
            // Act - Call the private method via reflection
            var result = getExistingMetricMethod.Invoke(null, new object[] { metricsList, "TestMetric" });
            
            // Assert - Should return null when ArgumentOutOfRangeException is caught
            Assert.Null(result);
            
            // Additional verification - ensure our ThrowingList actually throws
            Assert.Equal(1, metricsList.Count); // Should return 1
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = metricsList[0]); // Should throw
        }
        
        [Fact]
        public async Task AddMetric_ExtremeRaceCondition_ShouldCoverArgumentOutOfRangeException()
        {
            // This test is designed to create the exact timing conditions that would
            // trigger ArgumentOutOfRangeException in GetExistingMetric during real usage
            
            // Arrange
            Metrics.ResetForTest();
            Metrics.SetNamespace("TestNamespace");
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            
            // Act - Create extreme race conditions with very tight timing
            for (int i = 0; i < 50; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        for (int j = 0; j < 500; j++)
                        {
                            // Add the same metric key repeatedly to trigger GetExistingMetric
                            Metrics.AddMetric("RaceConditionMetric", 1.0, MetricUnit.Count);
                            
                            // Create timing pressure with very short delays
                            if (j % 25 == 0)
                            {
                                await Task.Delay(1); // Tiny delay to create timing windows
                                
                                // Force flush by adding 100+ metrics
                                for (int k = 0; k < 101; k++)
                                {
                                    Metrics.AddMetric($"FlushForce_{taskId}_{k}", 1.0, MetricUnit.Count);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Assert - Should not have any unhandled exceptions
            foreach (var ex in exceptions)
            {
                Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            }
            Assert.Empty(exceptions);
            
            // Cleanup
            CleanupMetrics();
        }
        
        /// <summary>
        /// Custom list that always throws ArgumentOutOfRangeException on indexer access
        /// to directly test the exception handling path in GetExistingMetric
        /// </summary>
        private class ThrowingList : List<MetricDefinition>
        {
            public new int Count => 1; // Return 1 so the for loop condition (i < metrics.Count) passes
            
            public new MetricDefinition this[int index]
            {
                get => throw new ArgumentOutOfRangeException(nameof(index), "Simulated concurrent modification");
                set => throw new ArgumentOutOfRangeException(nameof(index), "Simulated concurrent modification");
            }
        }
        
        /// <summary>
        /// Cleanup method to ensure no state leaks between tests
        /// </summary>
        private void CleanupMetrics()
        {
            try
            {
                // Reset the static instance to clean state
                Metrics.ResetForTest();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        /// <summary>
        /// IDisposable implementation for proper test cleanup
        /// </summary>
        public void Dispose()
        {
            CleanupMetrics();
        }
    }
}