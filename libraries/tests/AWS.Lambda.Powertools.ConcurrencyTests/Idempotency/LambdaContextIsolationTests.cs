/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Xunit;
using IdempotencyLib = AWS.Lambda.Powertools.Idempotency;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Idempotency;

/// <summary>
/// Tests for validating LambdaContext isolation in Powertools Idempotency
/// under concurrent execution scenarios.
/// 
/// These tests verify that when multiple Lambda invocations run concurrently,
/// each invocation's LambdaContext remains isolated from other invocations.
/// 
/// The Idempotency implementation uses AsyncLocal storage to ensure
/// isolation between concurrent Lambda invocations.
/// </summary>
[Collection("Idempotency Tests")]
public class LambdaContextIsolationTests : IDisposable
{
    public LambdaContextIsolationTests()
    {
        // Configure Idempotency with a thread-safe in-memory store for testing
        IdempotencyLib.Idempotency.Configure(builder => builder
            .WithPersistenceStore(new ThreadSafeInMemoryPersistenceStore()));
    }

    public void Dispose()
    {
        // Clean up after tests
    }

    #region Helper Result Classes

    private class ContextIsolationResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public int InvocationIndex { get; set; }
        public string ExpectedFunctionName { get; set; } = string.Empty;
        public string ActualFunctionName { get; set; } = string.Empty;
        public bool ContextMatched { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class AsyncContextResult
    {
        public string InvocationId { get; set; } = string.Empty;
        public string ExpectedFunctionName { get; set; } = string.Empty;
        public string FunctionNameBeforeAwait { get; set; } = string.Empty;
        public string FunctionNameAfterAwait { get; set; } = string.Empty;
        public bool ContextPreservedAcrossAwait { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    #endregion

    #region Property 2: LambdaContext Isolation

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 2: LambdaContext Isolation**
    /// *For any* set of concurrent invocations registering different LambdaContext instances,
    /// each invocation should be able to retrieve its own context without interference from other invocations.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void LambdaContextIsolation_ConcurrentInvocations_ShouldMaintainSeparateContexts(
        int concurrencyLevel)
    {
        var results = new ContextIsolationResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;

            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"Function_{invocationIndex}_{invocationId}";
                
                var result = new ContextIsolationResult
                {
                    InvocationId = invocationId,
                    InvocationIndex = invocationIndex,
                    ExpectedFunctionName = expectedFunctionName
                };

                try
                {
                    // Create a unique context for this invocation
                    var context = new TestLambdaContext
                    {
                        FunctionName = expectedFunctionName,
                        AwsRequestId = invocationId,
                        RemainingTime = TimeSpan.FromMinutes(5)
                    };

                    // Wait for all threads to be ready
                    barrier.SignalAndWait();

                    // Register the context
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    // Small delay to allow other threads to potentially interfere
                    Thread.Sleep(Random.Shared.Next(1, 10));

                    // Retrieve the context and verify it's the one we registered
                    var retrievedContext = IdempotencyLib.Idempotency.Instance.LambdaContext;
                    result.ActualFunctionName = retrievedContext?.FunctionName ?? "null";
                    result.ContextMatched = retrievedContext?.FunctionName == expectedFunctionName;
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}";
                }

                results[invocationIndex] = result;
            });
        }

        Task.WaitAll(tasks);

        // Verify no exceptions were thrown
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        
        // Verify each invocation retrieved its own context
        Assert.All(results, r => Assert.True(r.ContextMatched, 
            $"Invocation {r.InvocationIndex} expected '{r.ExpectedFunctionName}' but got '{r.ActualFunctionName}'"));
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 2b: LambdaContext Isolation with Multiple Reads**
    /// *For any* set of concurrent invocations, multiple reads of the LambdaContext within the same
    /// invocation should consistently return the same context.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Theory]
    [InlineData(2, 5)]
    [InlineData(5, 10)]
    [InlineData(10, 3)]
    public void LambdaContextIsolation_MultipleReads_ShouldReturnConsistentContext(
        int concurrencyLevel, int readsPerInvocation)
    {
        var results = new List<ContextIsolationResult>[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            results[invocationIndex] = new List<ContextIsolationResult>();

            tasks[i] = Task.Run(() =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"Function_{invocationIndex}_{invocationId}";

                try
                {
                    var context = new TestLambdaContext
                    {
                        FunctionName = expectedFunctionName,
                        AwsRequestId = invocationId,
                        RemainingTime = TimeSpan.FromMinutes(5)
                    };

                    barrier.SignalAndWait();

                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    // Perform multiple reads with small delays
                    for (int r = 0; r < readsPerInvocation; r++)
                    {
                        Thread.Sleep(Random.Shared.Next(1, 5));
                        
                        var retrievedContext = IdempotencyLib.Idempotency.Instance.LambdaContext;
                        var result = new ContextIsolationResult
                        {
                            InvocationId = invocationId,
                            InvocationIndex = invocationIndex,
                            ExpectedFunctionName = expectedFunctionName,
                            ActualFunctionName = retrievedContext?.FunctionName ?? "null",
                            ContextMatched = retrievedContext?.FunctionName == expectedFunctionName
                        };
                        
                        lock (results[invocationIndex])
                        {
                            results[invocationIndex].Add(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (results[invocationIndex])
                    {
                        results[invocationIndex].Add(new ContextIsolationResult
                        {
                            InvocationId = invocationId,
                            InvocationIndex = invocationIndex,
                            ExceptionThrown = true,
                            ExceptionMessage = $"{ex.GetType().Name}: {ex.Message}"
                        });
                    }
                }
            });
        }

        Task.WaitAll(tasks);

        // Verify all reads returned the correct context
        foreach (var invocationResults in results)
        {
            Assert.All(invocationResults, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
            Assert.All(invocationResults, r => Assert.True(r.ContextMatched,
                $"Invocation {r.InvocationIndex} expected '{r.ExpectedFunctionName}' but got '{r.ActualFunctionName}'"));
        }
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 2c: LambdaContext Isolation Across Async Boundaries**
    /// *For any* invocation with async operations, the LambdaContext should be preserved
    /// across await points.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task LambdaContextIsolation_AsyncOperations_ShouldPreserveContextAcrossAwait(
        int concurrencyLevel)
    {
        var results = new AsyncContextResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;

            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"AsyncFunction_{invocationIndex}_{invocationId}";
                
                var result = new AsyncContextResult
                {
                    InvocationId = invocationId,
                    ExpectedFunctionName = expectedFunctionName
                };

                try
                {
                    var context = new TestLambdaContext
                    {
                        FunctionName = expectedFunctionName,
                        AwsRequestId = invocationId,
                        RemainingTime = TimeSpan.FromMinutes(5)
                    };

                    barrier.SignalAndWait();

                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    // Read before await
                    var contextBeforeAwait = IdempotencyLib.Idempotency.Instance.LambdaContext;
                    result.FunctionNameBeforeAwait = contextBeforeAwait?.FunctionName ?? "null";

                    // Simulate async operation
                    await Task.Delay(Random.Shared.Next(10, 50));

                    // Read after await
                    var contextAfterAwait = IdempotencyLib.Idempotency.Instance.LambdaContext;
                    result.FunctionNameAfterAwait = contextAfterAwait?.FunctionName ?? "null";

                    result.ContextPreservedAcrossAwait = 
                        result.FunctionNameBeforeAwait == expectedFunctionName &&
                        result.FunctionNameAfterAwait == expectedFunctionName;
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

        // Verify no exceptions were thrown
        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        
        // Verify context was preserved across await
        Assert.All(results, r => Assert.True(r.ContextPreservedAcrossAwait,
            $"Context not preserved: expected '{r.ExpectedFunctionName}', " +
            $"before await: '{r.FunctionNameBeforeAwait}', after await: '{r.FunctionNameAfterAwait}'"));
    }

    #endregion
}
