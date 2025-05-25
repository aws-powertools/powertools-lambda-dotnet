using System.Diagnostics.CodeAnalysis;
using AWS.Lambda.Powertools.EventHandler.Internal;
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace AWS.Lambda.Powertools.EventHandler.Tests;

[SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
public class RouteHandlerRegistryTests
{
    [Theory]
    [InlineData("/default/channel", true)]
    [InlineData("/default/*", true)]
    [InlineData("/*", true)]
    [InlineData("/a/b/c", true)]
    [InlineData("/a/*/c", false)] // Wildcard in the middle is invalid
    [InlineData("*/default", false)] // Wildcard at the beginning is invalid
    [InlineData("default/*", false)] // Not starting with slash
    [InlineData("", false)] // Empty path
    [InlineData(null, false)] // Null path
    public void IsValidPath_ShouldValidateCorrectly(string? path, bool expected)
    {
        // Create a private method accessor to test private IsValidPath method
        var registry = new RouteHandlerRegistry<object, object>();
        var isValidPathMethod = typeof(RouteHandlerRegistry<object, object>)
            .GetMethod("IsValidPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (bool)isValidPathMethod.Invoke(null, new object[] { path });
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Register_ShouldNotAddInvalidPath()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<object, object>();
        
        // Act
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/invalid/*/path", // Invalid path with wildcard in the middle
            Handler = (_, _) => Task.FromResult<object>(null)
        });
        
        // Assert - Try to resolve an invalid path
        var result = registry.ResolveFirst("/invalid/test/path");
        Assert.Null(result); // Should not find any handler
    }

    [Fact]
    public void Register_ShouldReplaceExistingHandler()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<object, object>();
        int firstHandlerCalled = 0;
        int secondHandlerCalled = 0;
        
        // Act
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/test/path",
            Handler = (_, _) => {
                firstHandlerCalled++;
                return Task.FromResult<object>("first");
            }
        });
        
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/test/path", // Same path, should replace first handler
            Handler = (_, _) => {
                secondHandlerCalled++;
                return Task.FromResult<object>("second");
            }
        });
        
        // Assert
        var handler = registry.ResolveFirst("/test/path");
        Assert.NotNull(handler);
        var result = handler.Handler(null, null).Result;
        Assert.Equal("second", result);
        Assert.Equal(0, firstHandlerCalled);
        Assert.Equal(1, secondHandlerCalled);
    }

    [Fact]
    public async Task ResolveFirst_ShouldReturnMostSpecificHandler()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<object, object>();
        
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/*",
            Handler = (_, _) => Task.FromResult<object>("least-specific")
        });
        
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/default/*",
            Handler = (_, _) => Task.FromResult<object>("more-specific")
        });
        
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/default/channel",
            Handler = (_, _) => Task.FromResult<object>("most-specific")
        });
        
        // Act - Test various paths
        var exactMatch = registry.ResolveFirst("/default/channel");
        var wildcardMatch = registry.ResolveFirst("/default/something");
        var rootMatch = registry.ResolveFirst("/something");
        
        // Assert
        Assert.NotNull(exactMatch);
        Assert.Equal("most-specific", await exactMatch.Handler(null, null));
        
        Assert.NotNull(wildcardMatch);
        Assert.Equal("more-specific", await wildcardMatch.Handler(null, null));
        
        Assert.NotNull(rootMatch);
        Assert.Equal("least-specific", await rootMatch.Handler(null, null));
    }

    [Fact]
    public void ResolveFirst_ShouldReturnNullWhenNoMatch()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<object, object>();
        
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/default/*",
            Handler = (_, _) => Task.FromResult<object>("test")
        });
        
        // Act
        var result = registry.ResolveFirst("/other/path");
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFirst_ShouldUseCacheForRepeatedPaths()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<object, object>();
        int handlerCallCount = 0;
        
        registry.Register(new RouteHandlerOptions<object, object>
        {
            Path = "/test/*",
            Handler = (_, _) => {
                handlerCallCount++;
                return Task.FromResult<object>("cached");
            }
        });
        
        // Act - Resolve the same path multiple times
        var first = registry.ResolveFirst("/test/path");
        var firstResult = first.Handler(null, null).Result;
        
        // Should use cached result
        var second = registry.ResolveFirst("/test/path");
        var secondResult = second.Handler(null, null).Result;
        
        // Assert
        Assert.Equal("cached", firstResult);
        Assert.Equal("cached", secondResult);
        Assert.Equal(2, handlerCallCount); // Handler should be called twice because handlers are executed
                                          // even though the path resolution is cached
        
        // The objects should be the same instance
        Assert.Same(first, second);
    }

    [Fact]
    public void LRUCache_ShouldEvictOldestItemsWhenFull()
    {
        // Arrange - Create a cache with size 2
        var cache = new LruCache<string, string>(2);
        
        // Act
        cache.Set("key1", "value1");
        cache.Set("key2", "value2");
        cache.Set("key3", "value3"); // Should evict key1
        
        // Assert
        Assert.False(cache.TryGet("key1", out _)); // Should be evicted
        Assert.True(cache.TryGet("key2", out var value2));
        Assert.Equal("value2", value2);
        Assert.True(cache.TryGet("key3", out var value3));
        Assert.Equal("value3", value3);
    }

    [Fact]
    public void IsWildcardMatch_ShouldMatchPathsCorrectly()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<object, object>();
        var isWildcardMatchMethod = typeof(RouteHandlerRegistry<object, object>)
            .GetMethod("IsWildcardMatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Test cases
        var testCases = new[]
        {
            (pattern: "/default/*", path: "/default/channel", expected: true),
            (pattern: "/default/*", path: "/default/other", expected: true),
            (pattern: "/default/*", path: "/default/nested/path", expected: true),
            (pattern: "/default/channel", path: "/default/channel", expected: true),
            (pattern: "/default/channel", path: "/default/other", expected: false),
            (pattern: "/*", path: "/anything", expected: true),
            (pattern: "/*", path: "/default/nested/deep", expected: true)
        };
        
        foreach (var (pattern, path, expected) in testCases)
        {
            // Act
            var result = (bool)isWildcardMatchMethod.Invoke(registry, new object[] { pattern, path });
            
            // Assert
            Assert.Equal(expected, result);
        }
    }
}