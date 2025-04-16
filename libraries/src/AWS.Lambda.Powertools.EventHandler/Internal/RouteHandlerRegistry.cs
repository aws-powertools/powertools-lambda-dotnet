using System.Text.RegularExpressions;

namespace AWS.Lambda.Powertools.EventHandler.Internal;

/// <summary>
/// Registry for storing route handlers for path-based routing operations.
/// Handles path matching, caching, and handler resolution.
/// </summary>
internal class RouteHandlerRegistry<TEvent, TResult>
{
    /// <summary>
    /// Dictionary of registered handlers
    /// </summary>
    private readonly Dictionary<string, RouteHandlerOptions<TEvent, TResult>> _resolvers = new();

    /// <summary>
    /// Cache for resolved routes to improve performance
    /// </summary>
    private readonly LRUCache<string, RouteHandlerOptions<TEvent, TResult>> _resolverCache;

    /// <summary>
    /// Set to track already logged warnings
    /// </summary>
    private readonly HashSet<string> _warnedPaths = new();

    /// <summary>
    /// Initialize a new registry for route handlers
    /// </summary>
    /// <param name="cacheSize">Max size of LRU cache (default 100)</param>
    public RouteHandlerRegistry(int cacheSize = 100)
    {
        _resolverCache = new LRUCache<string, RouteHandlerOptions<TEvent, TResult>>(cacheSize);
    }

    /// <summary>
    /// Register a handler for a specific path pattern.
    /// </summary>
    /// <param name="options">Options for the route handler</param>
    public void Register(RouteHandlerOptions<TEvent, TResult> options)
    {
        if (!IsValidPath(options.Path))
        {
            LogWarning($"The path \"{options.Path}\" is not valid and will be skipped. " +
                      "Wildcards are allowed only at the end of the path.");
            return;
        }

        // Clear cache when registering new handlers
        _resolverCache.Clear();
        _resolvers[options.Path] = options;
    }

    /// <summary>
    /// Find the most specific handler for a given path.
    /// </summary>
    /// <param name="path">The path to match against registered routes</param>
    /// <returns>Most specific matching handler or null if no match</returns>
    public RouteHandlerOptions<TEvent, TResult>? ResolveFirst(string path)
    {
        if (_resolverCache.TryGet(path, out var cachedHandler))
        {
            return cachedHandler;
        }

        // First try for exact match
        if (_resolvers.TryGetValue(path, out var exactMatch))
        {
            _resolverCache.Set(path, exactMatch);
            return exactMatch;
        }

        // Then try wildcard matches, sorted by specificity (most segments first)
        var wildcardMatches = _resolvers.Keys
            .Where(pattern => IsWildcardMatch(pattern, path))
            .OrderByDescending(pattern => pattern.Count(c => c == '/'))
            .ThenByDescending(pattern => pattern.Length);

        var bestMatch = wildcardMatches.FirstOrDefault();

        if (bestMatch != null)
        {
            var handler = _resolvers[bestMatch];
            _resolverCache.Set(path, handler);
            return handler;
        }

        return null;
    }

    /// <summary>
    /// Check if a path pattern is valid according to routing rules.
    /// </summary>
    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/"))
            return false;

        // Check for invalid wildcard usage
        return !path.Contains("*/");
    }

    /// <summary>
    /// Check if a wildcard pattern matches the given path
    /// </summary>
    private bool IsWildcardMatch(string pattern, string path)
    {
        if (!pattern.Contains('*'))
            return pattern == path;

        var patternSegments = pattern.Split('/');
        var pathSegments = path.Split('/');

        if (patternSegments.Length > pathSegments.Length)
            return false;

        for (var i = 0; i < patternSegments.Length; i++)
        {
            // If we've reached the wildcard segment, it matches the rest
            if (patternSegments[i] == "*")
                return true;

            // Otherwise, segments must match exactly
            if (patternSegments[i] != pathSegments[i])
                return false;
        }

        return patternSegments.Length == pathSegments.Length;
    }

    private void LogWarning(string message)
    {
        if (!_warnedPaths.Contains(message))
        {
            _warnedPaths.Add(message);
            Console.WriteLine($"Warning: {message}");
        }
    }
}