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

using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Idempotency.Internal;
using Xunit;
using IdempotencyLib = AWS.Lambda.Powertools.Idempotency;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Idempotency;

/// <summary>
/// Tests for validating async context preservation in Powertools Idempotency.
/// **Feature: idempotency-thread-safety, Property 7: Async Context Preservation**
/// **Validates: Requirements 6.1, 6.2, 6.3**
/// </summary>
[Collection("Idempotency Async Context Tests")]
public class IdempotencyAsyncContextTests : IDisposable
{
    private readonly ThreadSafeInMemoryPersistenceStore _store;

    public IdempotencyAsyncContextTests()
    {
        _store = new ThreadSafeInMemoryPersistenceStore();
        IdempotencyLib.Idempotency.Configure(builder => builder
            .WithPersistenceStore(_store)
            .WithOptions(opt => opt
                .WithEventKeyJmesPath("id")
                .WithExpiration(TimeSpan.FromMinutes(5))));
    }

    public void Dispose()
    {
        _store.Clear();
        _store.ResetCounters();
    }

    private class AsyncContextResult
    {
        public int InvocationIndex { get; set; }
        public string InvocationId { get; set; } = string.Empty;
        public string ExpectedFunctionName { get; set; } = string.Empty;
        public List<string> FunctionNamesAtAwaitPoints { get; set; } = new();
        public bool ContextPreservedAcrossAllAwaits { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class HandlerAsyncResult
    {
        public int InvocationIndex { get; set; }
        public string InvocationId { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string? ActualResult { get; set; }
        public bool ResultMatched { get; set; }
        public List<string> ContextsAtAwaitPoints { get; set; } = new();
        public bool ContextPreserved { get; set; }
        public bool ExceptionThrown { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    private class TestRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    private static JsonDocument CreatePayload(string id, string data)
    {
        var request = new TestRequest { Id = id, Data = data };
        return JsonDocument.Parse(JsonSerializer.Serialize(request));
    }

    private static TestLambdaContext CreateTestContext(string functionName, string requestId)
    {
        return new TestLambdaContext
        {
            FunctionName = functionName,
            AwsRequestId = requestId,
            RemainingTime = TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// **Feature: idempotency-thread-safety, Property 7: Async Context Preservation**
    /// **Validates: Requirements 6.1, 6.2, 6.3**
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void AsyncContextPreservation_AcrossAwaitPoints_ShouldPreserveContext(int awaitCount)
    {
        var invocationId = Guid.NewGuid().ToString("N");
        var expectedFunctionName = $"AsyncFunction_{invocationId}";
        var functionsAtAwaitPoints = new List<string>();
        
        var context = CreateTestContext(expectedFunctionName, invocationId);
        IdempotencyLib.Idempotency.RegisterLambdaContext(context);

        var contextBefore = IdempotencyLib.Idempotency.Instance.LambdaContext;
        Assert.Equal(expectedFunctionName, contextBefore?.FunctionName);

        var task = Task.Run(async () =>
        {
            for (int i = 0; i < awaitCount; i++)
            {
                await Task.Delay(Random.Shared.Next(1, 10));
                var currentContext = IdempotencyLib.Idempotency.Instance.LambdaContext;
                lock (functionsAtAwaitPoints)
                {
                    functionsAtAwaitPoints.Add(currentContext?.FunctionName ?? "null");
                }
            }
        });

        task.Wait();

        Assert.All(functionsAtAwaitPoints, fn => Assert.Equal(expectedFunctionName, fn));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void AsyncContextPreservation_ConcurrentAsyncInvocations_ShouldMaintainIsolation(int concurrencyLevel)
    {
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"ConcurrentAsync_{invocationIndex}_{invocationId}";
                
                var result = new AsyncContextResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedFunctionName = expectedFunctionName
                };

                try
                {
                    var context = CreateTestContext(expectedFunctionName, invocationId);
                    
                    barrier.SignalAndWait();
                    
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    for (int awaitPoint = 0; awaitPoint < 3; awaitPoint++)
                    {
                        await Task.Delay(Random.Shared.Next(5, 20));
                        var currentContext = IdempotencyLib.Idempotency.Instance.LambdaContext;
                        result.FunctionNamesAtAwaitPoints.Add(currentContext?.FunctionName ?? "null");
                    }

                    result.ContextPreservedAcrossAllAwaits = 
                        result.FunctionNamesAtAwaitPoints.All(fn => fn == expectedFunctionName);
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results.Add(result);
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ContextPreservedAcrossAllAwaits,
            $"Invocation {r.InvocationIndex} did not preserve context. Expected '{r.ExpectedFunctionName}', got: [{string.Join(", ", r.FunctionNamesAtAwaitPoints)}]"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void AsyncContextPreservation_InHandler_ShouldPreserveContext(int concurrencyLevel)
    {
        var results = new ConcurrentBag<HandlerAsyncResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"HandlerAsync_{invocationIndex}_{invocationId}";
                var expectedResult = $"result_{invocationId}";
                
                var result = new HandlerAsyncResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedResult = expectedResult
                };

                try
                {
                    var payload = CreatePayload(invocationId, $"async_data_{invocationIndex}");
                    var context = CreateTestContext(expectedFunctionName, invocationId);
                    
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    Func<Task<string>> targetFunc = async () =>
                    {
                        var ctx1 = IdempotencyLib.Idempotency.Instance.LambdaContext;
                        result.ContextsAtAwaitPoints.Add(ctx1?.FunctionName ?? "null");
                        
                        await Task.Delay(Random.Shared.Next(5, 15));
                        
                        var ctx2 = IdempotencyLib.Idempotency.Instance.LambdaContext;
                        result.ContextsAtAwaitPoints.Add(ctx2?.FunctionName ?? "null");
                        
                        await Task.Delay(Random.Shared.Next(5, 15));
                        
                        var ctx3 = IdempotencyLib.Idempotency.Instance.LambdaContext;
                        result.ContextsAtAwaitPoints.Add(ctx3?.FunctionName ?? "null");
                        
                        return expectedResult;
                    };

                    barrier.SignalAndWait();

                    var handler = new IdempotencyAspectHandler<string>(
                        targetFunc, $"HandlerAsyncFunction_{invocationIndex}", null, payload, context);

                    result.ActualResult = await handler.Handle();
                    result.ResultMatched = result.ActualResult == expectedResult;
                    result.ContextPreserved = result.ContextsAtAwaitPoints.All(fn => fn == expectedFunctionName);
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results.Add(result);
            });
        }

        Task.WaitAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ResultMatched, $"Expected '{r.ExpectedResult}' but got '{r.ActualResult}'"));
        Assert.All(results, r => Assert.True(r.ContextPreserved,
            $"Context not preserved: [{string.Join(", ", r.ContextsAtAwaitPoints)}]"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task AsyncContext_WithConfigureAwaitFalse_ShouldPreserveContext(int concurrencyLevel)
    {
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"ConfigureAwaitFalse_{invocationIndex}_{invocationId}";
                
                var result = new AsyncContextResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedFunctionName = expectedFunctionName
                };

                try
                {
                    var context = CreateTestContext(expectedFunctionName, invocationId);
                    
                    barrier.SignalAndWait();
                    
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    var ctxBefore = IdempotencyLib.Idempotency.Instance.LambdaContext;
                    result.FunctionNamesAtAwaitPoints.Add(ctxBefore?.FunctionName ?? "null");

                    await Task.Delay(Random.Shared.Next(10, 30)).ConfigureAwait(false);

                    var ctxAfter = IdempotencyLib.Idempotency.Instance.LambdaContext;
                    result.FunctionNamesAtAwaitPoints.Add(ctxAfter?.FunctionName ?? "null");

                    await Task.Delay(Random.Shared.Next(10, 30)).ConfigureAwait(false);

                    var ctxFinal = IdempotencyLib.Idempotency.Instance.LambdaContext;
                    result.FunctionNamesAtAwaitPoints.Add(ctxFinal?.FunctionName ?? "null");

                    result.ContextPreservedAcrossAllAwaits = 
                        result.FunctionNamesAtAwaitPoints.All(fn => fn == expectedFunctionName);
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results.Add(result);
            });
        }

        await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ContextPreservedAcrossAllAwaits,
            $"Invocation {r.InvocationIndex} did not preserve context. Expected '{r.ExpectedFunctionName}', got: [{string.Join(", ", r.FunctionNamesAtAwaitPoints)}]"));
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(5, 3)]
    [InlineData(10, 2)]
    public async Task AsyncContext_NestedAsyncOperations_ShouldPreserveContext(int concurrencyLevel, int nestingDepth)
    {
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"NestedAsync_{invocationIndex}_{invocationId}";
                
                var result = new AsyncContextResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedFunctionName = expectedFunctionName
                };

                try
                {
                    var context = CreateTestContext(expectedFunctionName, invocationId);
                    
                    barrier.SignalAndWait();
                    
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    async Task NestedAsync(int depth)
                    {
                        if (depth <= 0) return;
                        
                        await Task.Delay(Random.Shared.Next(1, 5));
                        
                        var currentContext = IdempotencyLib.Idempotency.Instance.LambdaContext;
                        lock (result.FunctionNamesAtAwaitPoints)
                        {
                            result.FunctionNamesAtAwaitPoints.Add(currentContext?.FunctionName ?? "null");
                        }
                        
                        await NestedAsync(depth - 1);
                    }

                    await NestedAsync(nestingDepth);

                    result.ContextPreservedAcrossAllAwaits = 
                        result.FunctionNamesAtAwaitPoints.All(fn => fn == expectedFunctionName);
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results.Add(result);
            });
        }

        await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ContextPreservedAcrossAllAwaits,
            $"Invocation {r.InvocationIndex} did not preserve context in nested async. Expected '{r.ExpectedFunctionName}', got: [{string.Join(", ", r.FunctionNamesAtAwaitPoints)}]"));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    public async Task AsyncContext_HighConcurrency_ShouldPreserveContext(int concurrencyLevel)
    {
        var results = new ConcurrentBag<AsyncContextResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedFunctionName = $"StressAsync_{invocationIndex}_{invocationId}";
                
                var result = new AsyncContextResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedFunctionName = expectedFunctionName
                };

                try
                {
                    var context = CreateTestContext(expectedFunctionName, invocationId);
                    
                    barrier.SignalAndWait();
                    
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    for (int awaitPoint = 0; awaitPoint < 5; awaitPoint++)
                    {
                        await Task.Delay(Random.Shared.Next(1, 10));
                        var currentContext = IdempotencyLib.Idempotency.Instance.LambdaContext;
                        result.FunctionNamesAtAwaitPoints.Add(currentContext?.FunctionName ?? "null");
                    }

                    result.ContextPreservedAcrossAllAwaits = 
                        result.FunctionNamesAtAwaitPoints.All(fn => fn == expectedFunctionName);
                }
                catch (Exception ex)
                {
                    result.ExceptionThrown = true;
                    result.ExceptionType = ex.GetType().Name;
                    result.ExceptionMessage = ex.Message;
                }

                results.Add(result);
            });
        }

        await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.False(r.ExceptionThrown, r.ExceptionMessage));
        Assert.All(results, r => Assert.True(r.ContextPreservedAcrossAllAwaits,
            $"Invocation {r.InvocationIndex} did not preserve context under stress. Expected '{r.ExpectedFunctionName}', got: [{string.Join(", ", r.FunctionNamesAtAwaitPoints)}]"));
    }
}
