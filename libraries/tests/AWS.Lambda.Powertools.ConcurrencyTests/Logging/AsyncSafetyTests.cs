using AWS.Lambda.Powertools.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Logging;

/// <summary>
/// Tests for validating async safety of Logger keys.
/// These tests verify that keys flow correctly across async/await boundaries
/// using AsyncLocal storage.
/// </summary>
public class AsyncSafetyTests
{
    /// <summary>
    /// Verifies that keys persist across await boundaries within the same execution context.
    /// This is the critical test for AsyncLocal correctness.
    /// </summary>
    [Fact]
    public async Task AppendKey_AcrossAwaitBoundary_ShouldPersist()
    {
        // Arrange
        ClearAllLoggerState();
        var testKey = "async_test_key";
        var testValue = "async_test_value";

        // Act
        Logger.AppendKey(testKey, testValue);
        
        // Force thread switch
        await Task.Delay(10).ConfigureAwait(false);
        await Task.Yield();
        
        // Assert - key should still be present after await
        var keys = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        
        Assert.True(keys.ContainsKey(testKey), "Key should persist across await boundary");
        Assert.Equal(testValue, keys[testKey]);
        
        // Cleanup
        Logger.RemoveKey(testKey);
    }

    /// <summary>
    /// Verifies that keys persist across multiple await boundaries.
    /// </summary>
    [Fact]
    public async Task AppendKey_AcrossMultipleAwaits_ShouldPersist()
    {
        // Arrange
        ClearAllLoggerState();
        var keys = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };

        // Act - add keys with awaits in between
        Logger.AppendKey("key1", "value1");
        await Task.Delay(5).ConfigureAwait(false);
        
        Logger.AppendKey("key2", "value2");
        await Task.Yield();
        
        Logger.AppendKey("key3", "value3");
        await Task.Delay(5).ConfigureAwait(false);

        // Assert
        var allKeys = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        
        foreach (var (key, value) in keys)
        {
            Assert.True(allKeys.ContainsKey(key), $"Key '{key}' should persist");
            Assert.Equal(value, allKeys[key]);
        }

        // Cleanup
        Logger.RemoveKeys(keys.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that parallel async operations maintain isolated scopes.
    /// Uses Logger.UseScope() to simulate Lambda multi-threaded mode.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task ParallelAsyncOperations_ShouldMaintainIsolation(int parallelCount)
    {
        // Arrange
        var results = new AsyncIsolationResult[parallelCount];
        var startSignal = new TaskCompletionSource<bool>();

        // Act
        var tasks = Enumerable.Range(0, parallelCount).Select(async index =>
        {
            var uniqueKey = $"parallel_key_{index}";
            var uniqueValue = $"parallel_value_{index}";

            await startSignal.Task; // Wait for all tasks to be ready

            // Simulate Lambda invocation starting fresh with isolated scope
            using (Logger.UseScope())
            {
                Logger.AppendKey(uniqueKey, uniqueValue);
                
                // Multiple awaits to force potential thread switches
                await Task.Delay(Random.Shared.Next(5, 20)).ConfigureAwait(false);
                await Task.Yield();
                await Task.Delay(Random.Shared.Next(5, 20)).ConfigureAwait(false);

                var allKeys = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);

                results[index] = new AsyncIsolationResult
                {
                    Index = index,
                    ExpectedKey = uniqueKey,
                    ExpectedValue = uniqueValue,
                    AllKeys = allKeys,
                    HasOwnKey = allKeys.ContainsKey(uniqueKey),
                    OwnKeyValue = allKeys.TryGetValue(uniqueKey, out var val) ? val?.ToString() : null
                };

                Logger.RemoveKey(uniqueKey);
            }
        }).ToArray();

        // Start all tasks simultaneously
        startSignal.SetResult(true);
        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.True(result.HasOwnKey, $"Task {result.Index} should have its own key");
            Assert.Equal(result.ExpectedValue, result.OwnKeyValue);
            
            // Verify no other task's keys leaked in
            var foreignKeys = result.AllKeys.Keys
                .Where(k => k.StartsWith("parallel_key_") && k != result.ExpectedKey)
                .ToList();
            
            Assert.Empty(foreignKeys);
        }
    }

    /// <summary>
    /// Verifies that nested async calls maintain the same scope.
    /// </summary>
    [Fact]
    public async Task NestedAsyncCalls_ShouldShareScope()
    {
        // Arrange
        ClearAllLoggerState();
        var outerKey = "outer_key";
        var innerKey = "inner_key";

        // Act
        Logger.AppendKey(outerKey, "outer_value");
        
        await NestedAsyncMethod(innerKey);
        
        // Assert - both keys should be present
        var allKeys = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        
        Assert.True(allKeys.ContainsKey(outerKey), "Outer key should be present");
        Assert.True(allKeys.ContainsKey(innerKey), "Inner key should be present after nested call");

        // Cleanup
        Logger.RemoveKeys(outerKey, innerKey);
    }

    private async Task NestedAsyncMethod(string key)
    {
        await Task.Delay(5).ConfigureAwait(false);
        Logger.AppendKey(key, "inner_value");
        await Task.Delay(5).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies ConfigureAwait(false) doesn't break key persistence.
    /// </summary>
    [Fact]
    public async Task ConfigureAwaitFalse_ShouldNotBreakKeyPersistence()
    {
        // Arrange
        ClearAllLoggerState();
        var testKey = "configureawait_test";

        // Act
        Logger.AppendKey(testKey, "before_await");
        
        await Task.Delay(10).ConfigureAwait(false);
        
        var keysAfterAwait = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        
        // Modify after ConfigureAwait(false)
        Logger.AppendKey(testKey, "after_await");
        
        await Task.Delay(10).ConfigureAwait(false);
        
        var keysFinal = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);

        // Assert
        Assert.True(keysAfterAwait.ContainsKey(testKey));
        Assert.True(keysFinal.ContainsKey(testKey));
        Assert.Equal("after_await", keysFinal[testKey]);

        // Cleanup
        Logger.RemoveKey(testKey);
    }

    private static void ClearAllLoggerState()
    {
        var existingKeys = Logger.GetAllKeys()
            .Select(k => k.Key)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToArray();
        if (existingKeys.Length > 0)
        {
            Logger.RemoveKeys(existingKeys);
        }
    }

    private class AsyncIsolationResult
    {
        public int Index { get; set; }
        public string ExpectedKey { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public Dictionary<string, object> AllKeys { get; set; } = new();
        public bool HasOwnKey { get; set; }
        public string? OwnKeyValue { get; set; }
    }
}
