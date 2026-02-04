using AWS.Lambda.Powertools.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests.Logging;

/// <summary>
/// Tests for the ExtraKeys feature which provides scoped temporary logging keys.
/// Keys added via ExtraKeys are automatically removed when the scope is disposed.
/// </summary>
public class ExtraKeysTests
{
    /// <summary>
    /// Verifies basic ExtraKeys functionality - keys are added and removed on dispose.
    /// </summary>
    [Fact]
    public void ExtraKeys_BasicUsage_ShouldAddAndRemoveKeys()
    {
        // Arrange
        ClearAllLoggerState();
        var testKey = "extra_key";
        var testValue = "extra_value";

        // Act & Assert - key present inside scope
        using (Logger.ExtraKeys(new Dictionary<string, object> { { testKey, testValue } }))
        {
            var keysInScope = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.True(keysInScope.ContainsKey(testKey));
            Assert.Equal(testValue, keysInScope[testKey]);
        }

        // Assert - key removed after scope
        var keysAfterScope = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.False(keysAfterScope.ContainsKey(testKey));
    }

    /// <summary>
    /// Verifies ExtraKeys with tuple syntax.
    /// </summary>
    [Fact]
    public void ExtraKeys_TupleSyntax_ShouldWork()
    {
        // Arrange
        ClearAllLoggerState();

        // Act & Assert
        using (Logger.ExtraKeys(("key1", "value1"), ("key2", 42), ("key3", true)))
        {
            var keys = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.Equal(3, keys.Count);
            Assert.Equal("value1", keys["key1"]);
            Assert.Equal(42, keys["key2"]);
            Assert.Equal(true, keys["key3"]);
        }

        var keysAfter = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.Empty(keysAfter);
    }

    /// <summary>
    /// Verifies ExtraKeys works correctly across await boundaries.
    /// </summary>
    [Fact]
    public async Task ExtraKeys_AcrossAwaitBoundary_ShouldPersistAndCleanup()
    {
        // Arrange
        ClearAllLoggerState();
        var testKey = "async_extra_key";

        // Act
        using (Logger.ExtraKeys(new Dictionary<string, object> { { testKey, "async_value" } }))
        {
            // Key should be present before await
            var keysBefore = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.True(keysBefore.ContainsKey(testKey));

            await Task.Delay(10).ConfigureAwait(false);
            await Task.Yield();

            // Key should still be present after await
            var keysAfterAwait = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.True(keysAfterAwait.ContainsKey(testKey));
        }

        // Key should be removed after dispose
        var keysAfterDispose = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.False(keysAfterDispose.ContainsKey(testKey));
    }

    /// <summary>
    /// Verifies nested ExtraKeys scopes work correctly.
    /// </summary>
    [Fact]
    public void ExtraKeys_NestedScopes_ShouldWorkCorrectly()
    {
        // Arrange
        ClearAllLoggerState();

        // Act & Assert
        using (Logger.ExtraKeys(("outer", "outer_value")))
        {
            var keysOuter = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.Single(keysOuter);
            Assert.Equal("outer_value", keysOuter["outer"]);

            using (Logger.ExtraKeys(("inner", "inner_value")))
            {
                var keysInner = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
                Assert.Equal(2, keysInner.Count);
                Assert.Equal("outer_value", keysInner["outer"]);
                Assert.Equal("inner_value", keysInner["inner"]);
            }

            // Inner key should be removed, outer should remain
            var keysAfterInner = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.Single(keysAfterInner);
            Assert.Equal("outer_value", keysAfterInner["outer"]);
            Assert.False(keysAfterInner.ContainsKey("inner"));
        }

        // All keys should be removed
        var keysFinal = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.Empty(keysFinal);
    }

    /// <summary>
    /// Verifies ExtraKeys overwrites existing keys and removes them on dispose.
    /// Following Python's behavior: keys added in scope are removed, even if they existed before.
    /// </summary>
    [Fact]
    public void ExtraKeys_OverwriteExistingKey_ShouldRemoveOnDispose()
    {
        // Arrange
        ClearAllLoggerState();
        var sharedKey = "shared_key";
        
        Logger.AppendKey(sharedKey, "original_value");

        // Act
        using (Logger.ExtraKeys(new Dictionary<string, object> { { sharedKey, "temporary_value" } }))
        {
            var keysInScope = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.Equal("temporary_value", keysInScope[sharedKey]);
        }

        // Assert - key is removed (following Python's behavior)
        var keysAfter = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.False(keysAfter.ContainsKey(sharedKey));
    }

    /// <summary>
    /// Verifies ExtraKeys with multiple keys.
    /// </summary>
    [Fact]
    public void ExtraKeys_MultipleKeys_ShouldAllBeAddedAndRemoved()
    {
        // Arrange
        ClearAllLoggerState();
        var testKeys = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 },
            { "key3", true },
            { "key4", new { nested = "object" } }
        };

        // Act & Assert
        using (Logger.ExtraKeys(testKeys))
        {
            var keysInScope = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.Equal(4, keysInScope.Count);
        }

        var keysAfter = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.Empty(keysAfter);
    }

    /// <summary>
    /// Verifies ExtraKeys is safe with concurrent async operations.
    /// Uses Logger.UseScope() to simulate Lambda multi-threaded mode.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ExtraKeys_ConcurrentAsyncOperations_ShouldMaintainIsolation(int parallelCount)
    {
        // Arrange
        var results = new ExtraKeysAsyncResult[parallelCount];
        var startSignal = new TaskCompletionSource<bool>();

        // Act
        var tasks = Enumerable.Range(0, parallelCount).Select(async index =>
        {
            var uniqueKey = $"concurrent_extra_{index}";
            var uniqueValue = $"value_{index}";

            await startSignal.Task; // Wait for all tasks to be ready

            // Simulate Lambda invocation starting fresh with isolated scope
            using (Logger.UseScope())
            {
                using (Logger.ExtraKeys((uniqueKey, uniqueValue)))
                {
                    await Task.Delay(Random.Shared.Next(10, 30)).ConfigureAwait(false);

                    var keysInScope = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);

                    results[index] = new ExtraKeysAsyncResult
                    {
                        Index = index,
                        ExpectedKey = uniqueKey,
                        ExpectedValue = uniqueValue,
                        KeysInScope = keysInScope,
                        HasOwnKey = keysInScope.ContainsKey(uniqueKey)
                    };

                    await Task.Delay(Random.Shared.Next(5, 15)).ConfigureAwait(false);
                }

                // Verify key is removed after scope
                var keysAfterScope = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
                results[index].KeyRemovedAfterScope = !keysAfterScope.ContainsKey(uniqueKey);
            }
        }).ToArray();

        // Start all tasks simultaneously
        startSignal.SetResult(true);
        await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.True(result.HasOwnKey, $"Task {result.Index} should have its key in scope");
            Assert.True(result.KeyRemovedAfterScope, $"Task {result.Index} key should be removed after scope");

            // Verify no other task's keys leaked in
            var foreignKeys = result.KeysInScope.Keys
                .Where(k => k.StartsWith("concurrent_extra_") && k != result.ExpectedKey)
                .ToList();

            Assert.Empty(foreignKeys);
        }
    }

    /// <summary>
    /// Verifies ExtraKeys handles exceptions correctly - keys should still be removed.
    /// </summary>
    [Fact]
    public void ExtraKeys_WithException_ShouldStillRemoveKeys()
    {
        // Arrange
        ClearAllLoggerState();
        var testKey = "exception_test_key";

        // Act
        try
        {
            using (Logger.ExtraKeys((testKey, "value")))
            {
                var keysInScope = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
                Assert.True(keysInScope.ContainsKey(testKey));

                throw new InvalidOperationException("Test exception");
            }
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert - key should still be removed despite exception
        var keysAfter = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.False(keysAfter.ContainsKey(testKey));
    }

    /// <summary>
    /// Verifies ExtraKeys handles null/empty keys gracefully.
    /// </summary>
    [Fact]
    public void ExtraKeys_WithEmptyKeys_ShouldNotThrow()
    {
        // Arrange
        ClearAllLoggerState();

        // Act & Assert - should not throw
        using (Logger.ExtraKeys(new Dictionary<string, object>()))
        {
            var keys = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
            Assert.Empty(keys);
        }
    }

    /// <summary>
    /// Verifies ExtraKeys throws on null input.
    /// </summary>
    [Fact]
    public void ExtraKeys_WithNullInput_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => 
            Logger.ExtraKeys((IEnumerable<KeyValuePair<string, object>>)null!));
    }

    /// <summary>
    /// Verifies double dispose is safe.
    /// </summary>
    [Fact]
    public void ExtraKeys_DoubleDispose_ShouldBeSafe()
    {
        // Arrange
        ClearAllLoggerState();
        var scope = Logger.ExtraKeys(("key", "value"));

        // Act - dispose twice
        scope.Dispose();
        scope.Dispose(); // Should not throw

        // Assert
        var keys = Logger.GetAllKeys().ToDictionary(k => k.Key, k => k.Value);
        Assert.Empty(keys);
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

    private class ExtraKeysAsyncResult
    {
        public int Index { get; set; }
        public string ExpectedKey { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public Dictionary<string, object> KeysInScope { get; set; } = new();
        public bool HasOwnKey { get; set; }
        public bool KeyRemovedAfterScope { get; set; }
    }
}
