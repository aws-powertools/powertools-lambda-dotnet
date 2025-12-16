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
/// Tests for validating IdempotencyAspectHandler isolation under concurrent execution scenarios.
/// **Feature: idempotency-thread-safety, Property 5: IdempotencyAspectHandler Isolation**
/// **Validates: Requirements 4.1, 4.2, 4.3, 4.4**
/// </summary>
[Collection("Idempotency Handler Isolation Tests")]
public class IdempotencyHandlerIsolationTests : IDisposable
{
    private readonly ThreadSafeInMemoryPersistenceStore _store;

    public IdempotencyHandlerIsolationTests()
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

    private class HandlerInvocationResult
    {
        public int InvocationIndex { get; set; }
        public string InvocationId { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string? ActualResult { get; set; }
        public bool ResultMatched { get; set; }
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

    private class TestResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public string ProcessedData { get; set; } = string.Empty;
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
    /// **Feature: idempotency-thread-safety, Property 5: IdempotencyAspectHandler Isolation**
    /// **Validates: Requirements 4.1, 4.2, 4.3, 4.4**
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void HandlerIsolation_ConcurrentInvocations_ShouldProcessIndependently(int concurrencyLevel)
    {
        var results = new ConcurrentBag<HandlerInvocationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var expectedResult = $"processed_{invocationId}";
                var result = new HandlerInvocationResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedResult = expectedResult
                };

                try
                {
                    var payload = CreatePayload(invocationId, $"data_{invocationIndex}");
                    var context = CreateTestContext($"Function_{invocationIndex}", invocationId);
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    Func<Task<TestResponse>> targetFunc = async () =>
                    {
                        await Task.Delay(Random.Shared.Next(5, 20));
                        return new TestResponse { RequestId = invocationId, ProcessedData = expectedResult };
                    };

                    barrier.SignalAndWait();

                    var handler = new IdempotencyAspectHandler<TestResponse>(
                        targetFunc, $"TestFunction_{invocationIndex}", null, payload, context);

                    var response = await handler.Handle();
                    result.ActualResult = response?.ProcessedData;
                    result.ResultMatched = response?.ProcessedData == expectedResult && response?.RequestId == invocationId;
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

        Assert.All(results, r => Assert.False(r.ExceptionThrown, $"{r.ExceptionType}: {r.ExceptionMessage}"));
        Assert.All(results, r => Assert.True(r.ResultMatched, $"Expected '{r.ExpectedResult}' but got '{r.ActualResult}'"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void HandlerIsolation_MultipleHandlerInstances_ShouldOperateIndependently(int concurrencyLevel)
    {
        var results = new ConcurrentBag<HandlerInvocationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new HandlerInvocationResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedResult = $"result_{invocationId}"
                };

                try
                {
                    var payload = CreatePayload(invocationId, $"data_{invocationIndex}");
                    var context = CreateTestContext($"IndependentFunction_{invocationIndex}", invocationId);
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    Func<Task<string>> targetFunc = async () =>
                    {
                        await Task.Delay(Random.Shared.Next(1, 10));
                        return result.ExpectedResult;
                    };

                    barrier.SignalAndWait();

                    var handler = new IdempotencyAspectHandler<string>(
                        targetFunc, $"IndependentFunction_{invocationIndex}", null, payload, context);

                    result.ActualResult = await handler.Handle();
                    result.ResultMatched = result.ActualResult == result.ExpectedResult;
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
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void HandlerIsolation_ConcurrentHandleExecution_ShouldNotCrossContaminate(int concurrencyLevel)
    {
        var processedIds = new ConcurrentBag<string>();
        var results = new ConcurrentBag<HandlerInvocationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new HandlerInvocationResult { InvocationIndex = invocationIndex, InvocationId = invocationId };

                try
                {
                    var payload = CreatePayload(invocationId, $"concurrent_data_{invocationIndex}");
                    var context = CreateTestContext($"ConcurrentFunction", invocationId);
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    string capturedId = invocationId;
                    Func<Task<string>> targetFunc = async () =>
                    {
                        await Task.Delay(Random.Shared.Next(5, 15));
                        processedIds.Add(capturedId);
                        return capturedId;
                    };

                    barrier.SignalAndWait();

                    var handler = new IdempotencyAspectHandler<string>(
                        targetFunc, $"ConcurrentFunction_{invocationIndex}", null, payload, context);

                    result.ActualResult = await handler.Handle();
                    result.ExpectedResult = invocationId;
                    result.ResultMatched = result.ActualResult == invocationId;
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
        Assert.Equal(concurrencyLevel, processedIds.Distinct().Count());
    }

    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    public async Task HandlerIsolation_HighConcurrency_ShouldMaintainIsolation(int concurrencyLevel)
    {
        var results = new ConcurrentBag<HandlerInvocationResult>();
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var invocationId = Guid.NewGuid().ToString("N");
                var result = new HandlerInvocationResult
                {
                    InvocationIndex = invocationIndex,
                    InvocationId = invocationId,
                    ExpectedResult = $"stress_result_{invocationId}"
                };

                try
                {
                    var payload = CreatePayload(invocationId, $"stress_data_{invocationIndex}");
                    var context = CreateTestContext($"StressFunction_{invocationIndex}", invocationId);
                    IdempotencyLib.Idempotency.RegisterLambdaContext(context);

                    Func<Task<string>> targetFunc = async () =>
                    {
                        await Task.Delay(Random.Shared.Next(1, 5));
                        return result.ExpectedResult;
                    };

                    barrier.SignalAndWait();

                    var handler = new IdempotencyAspectHandler<string>(
                        targetFunc, $"StressFunction_{invocationIndex}", null, payload, context);

                    result.ActualResult = await handler.Handle();
                    result.ResultMatched = result.ActualResult == result.ExpectedResult;
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
        Assert.All(results, r => Assert.True(r.ResultMatched, $"Expected '{r.ExpectedResult}' but got '{r.ActualResult}'"));
    }
}
