using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests
{
    public class ThreadingTests : IDisposable
    {
        [Fact]
        public async Task AddMetric_ConcurrentSameKey_VerifiesThreadSafety()
        {
            var exceptionThrown = false;
            var exception = (Exception)null;
            var operationCount = 0;
            var lockObject = new object();
            
            try
            {
                Metrics.ResetForTest();
                Metrics.SetNamespace("Test");
                
                // Use many threads to stress test the system
                var tasks = new Task[50];
                
                for (int i = 0; i < 50; i++)
                {
                    int threadId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            try
                            {
                                // Mix of same and different keys to trigger both code paths
                                if (j % 3 == 0)
                                {
                                    // This now uses GetExistingMetric instead of FirstOrDefault
                                    Metrics.AddMetric("SharedKey", 1.0, MetricUnit.Count);
                                }
                                else if (j % 7 == 0)
                                {
                                    // Different key to force collection modifications
                                    Metrics.AddMetric($"Key{threadId}_{j}", 1.0, MetricUnit.Count);
                                }
                                else
                                {
                                    // Back to shared key
                                    Metrics.AddMetric("SharedKey", 2.0, MetricUnit.Count);
                                }
                                
                                Interlocked.Increment(ref operationCount);
                                
                                // Occasionally flush to trigger more collection modifications
                                if (j % 25 == 0)
                                {
                                    Metrics.Flush();
                                }
                            }
                            catch (Exception ex)
                            {
                                // Capture any threading-related exceptions that shouldn't occur now
                                if (ex is NullReferenceException || 
                                    ex is InvalidOperationException ||
                                    ex.Message.Contains("Collection was modified") || 
                                    ex.Message.Contains("enumeration"))
                                {
                                    lock (lockObject)
                                    {
                                        if (!exceptionThrown)
                                        {
                                            exceptionThrown = true;
                                            exception = ex;
                                        }
                                    }
                                    return; // Exit this thread
                                }
                                throw; // Re-throw other exceptions
                            }
                        }
                    });
                }
                
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Capture any threading-related exceptions at the task level
                if (ex is NullReferenceException || 
                    ex is InvalidOperationException ||
                    ex.Message.Contains("modified") || 
                    ex.Message.Contains("enumeration"))
                {
                    exceptionThrown = true;
                    exception = ex;
                }
                else
                {
                    throw; // Re-throw unexpected exceptions
                }
            }
            
            // Assert that the threading bug has been fixed - no exceptions should occur
            Assert.False(exceptionThrown, 
                $"Threading exception occurred, indicating the bug may not be fully fixed: {exception?.GetType().Name}: {exception?.Message}");
            
            // Verify that we successfully performed many concurrent operations
            Assert.True(operationCount > 1000, 
                $"Expected many successful operations, but only got {operationCount}");
        }
        
        public void Dispose()
        {
            Metrics.ResetForTest();
        }
    }
}