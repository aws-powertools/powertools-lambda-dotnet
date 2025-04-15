using System.Text.RegularExpressions;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Internal;

namespace AWS.Lambda.Powertools.EventHandler.Tests;

public class RouteHandlerRegistryTests
{
    [Theory]
    [InlineData("/default/channel", true)]
    [InlineData("/default/*", true)]
    [InlineData("/*", true)]
    [InlineData("/a/b/c", true)]
    [InlineData("/a/b/c/*", true)]
    [InlineData("default/channel", false)] // Missing leading slash
    [InlineData("/default/*/channel", false)] // Wildcard in middle
    [InlineData("/default/**", false)] // Double wildcard
    [InlineData("/*a", false)] // Invalid wildcard usage
    [InlineData("", false)] // Empty
    public void IsValidPath_ShouldValidateCorrectly(string path, bool expected)
    {
        // Act
        var result = RouteHandlerRegistry<object, object>.IsValidPath(path);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("/default/channel", @"^/default/channel$")]
    [InlineData("/default/*", @"^/default/.*$")]
    [InlineData("/*", @"^/.*$")]
    [InlineData("/a/b+c", @"^/a/b\+c$")] // Test escaping special characters
    public void PathToRegexString_ShouldConvertCorrectly(string path, string expected)
    {
        // Act
        var result = RouteHandlerRegistry<object, object>.PathToRegexString(path);
        
        // Assert
        Assert.Equal(expected, result);
        Assert.True(Regex.IsMatch(path.Replace("*", "anything"), result));
    }
    
    [Fact]
    public void Register_ShouldNotAddInvalidPath()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<string, bool>();
        var called = false;
        
        // Act
        registry.Register(new RouteHandlerOptions<string, bool> 
        { 
            Path = "/invalid/*/path", 
            Handler = (_, __) => 
            {
                called = true;
                return Task.FromResult(true);
            }
        });
        
        var result = registry.Resolve("/invalid/something/path");
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void Register_ShouldReplaceExistingHandler()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<string, string>();
        
        // Act
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/test/path",
            Handler = (_, __) => Task.FromResult("first")
        });
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/test/path",
            Handler = (_, __) => Task.FromResult("second")
        });
        
        var handler = registry.Resolve("/test/path");
        
        // Assert
        Assert.NotNull(handler);
        var result = handler.Handler("test", new TestLambdaContext()).Result;
        Assert.Equal("second", result);
    }
    
    [Fact]
    public void Resolve_ShouldReturnMostSpecificHandler()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<string, string>();
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/*",
            Handler = (_, __) => Task.FromResult("wildcard")
        });
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/test/*",
            Handler = (_, __) => Task.FromResult("test-wildcard")
        });
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/test/exact",
            Handler = (_, __) => Task.FromResult("exact")
        });
        
        // Act & Assert
        var handler1 = registry.Resolve("/test/exact");
        Assert.Equal("exact", handler1.Handler("test", new TestLambdaContext()).Result);
        
        var handler2 = registry.Resolve("/test/other");
        Assert.Equal("test-wildcard", handler2.Handler("test", new TestLambdaContext()).Result);
        
        var handler3 = registry.Resolve("/other/path");
        Assert.Equal("wildcard", handler3.Handler("test", new TestLambdaContext()).Result);
    }
    
    [Fact]
    public void Resolve_ShouldReturnNullWhenNoMatch()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<string, string>();
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/test/*",
            Handler = (_, __) => Task.FromResult("test-wildcard")
        });
        
        // Act
        var handler = registry.Resolve("/other/path");
        
        // Assert
        Assert.Null(handler);
    }
    
    [Fact]
    public void ResolveAll_ShouldReturnAllMatchingHandlersInOrder()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<string, string>();
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/*",
            Handler = (_, __) => Task.FromResult("global")
        });
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/test/*",
            Handler = (_, __) => Task.FromResult("test-wildcard")
        });
        
        registry.Register(new RouteHandlerOptions<string, string>
        {
            Path = "/test/exact",
            Handler = (_, __) => Task.FromResult("exact")
        });
        
        // Act
        var handlers = registry.ResolveAll("/test/exact");
        
        // Assert
        Assert.Equal(3, handlers.Count);
        Assert.Equal("exact", handlers[0].Handler("test", new TestLambdaContext()).Result); // Most specific
        Assert.Equal("test-wildcard", handlers[1].Handler("test", new TestLambdaContext()).Result);
        Assert.Equal("global", handlers[2].Handler("test", new TestLambdaContext()).Result); // Least specific
    }
    
    [Fact]
    public void Resolve_ShouldUseCacheForRepeatedPaths()
    {
        // Arrange
        var registry = new RouteHandlerRegistry<string, int>(3); // Small cache size
        var callCount = 0;
        
        registry.Register(new RouteHandlerOptions<string, int>
        {
            Path = "/path1",
            Handler = (_, __) => { callCount++; return Task.FromResult(1); }
        });
        
        // Act
        var handler1 = registry.Resolve("/path1");
        var result1 = handler1.Handler("test", new TestLambdaContext()).Result;
        
        var handler2 = registry.Resolve("/path1"); // Should use cache
        var result2 = handler2.Handler("test", new TestLambdaContext()).Result;
        
        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(1, result2);
        Assert.Equal(2, callCount); // Handler called twice, but resolution only happened once
    }
    
    [Fact]
    public void Cache_ShouldEvictOldestItemsWhenFull()
    {
        // Arrange - Create a cache with size 2
        var cache = new LRUCache<string, string>(2);
        
        // Act
        cache.Add("key1", "value1");
        cache.Add("key2", "value2");
        cache.Add("key3", "value3"); // Should evict key1
        
        // Assert
        string value;
        Assert.False(cache.TryGetValue("key1", out value)); // Should be evicted
        Assert.True(cache.TryGetValue("key2", out value));
        Assert.Equal("value2", value);
        Assert.True(cache.TryGetValue("key3", out value));
        Assert.Equal("value3", value);
    }
}