using System.Text.RegularExpressions;

namespace AWS.Lambda.Powertools.EventHandler.Internal;

/// <summary>
/// Registry for storing route handlers for path-based routing operations.
/// Handles path matching, caching, and handler resolution.
/// </summary>
internal class RouteHandlerRegistry<TEvent, TResult>
{
    /// <summary>
    /// Dictionary of registered handlers, keyed by regex pattern
    /// </summary>
    private readonly Dictionary<string, RouteHandlerOptions<TEvent, TResult>> _resolvers = new();

    /// <summary>
    /// Cache for resolved routes to improve performance
    /// </summary>
    private readonly LRUCache<string, RouteHandlerOptions<TEvent, TResult>> _resolverCache;

    /// <summary>
    /// Set to track already logged warnings to prevent duplicates
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
        LogDebug($"Registering route handler for path \"{options.Path}\" with aggregate \"{options.Aggregate}\"");

        if (!IsValidPath(options.Path))
        {
            LogWarning($"The path \"{options.Path}\" is not valid and will be skipped. " +
                      "A path should always have a namespace starting with \"/\". A path can have multiple namespaces, " +
                      "all separated by \"/\". Wildcards are allowed only at the end of the path.");
            return;
        }

        string regex = PathToRegexString(options.Path);

        if (_resolvers.ContainsKey(regex))
        {
            LogWarning($"A route handler for path \"{options.Path}\" is already registered. " +
                      "The previous handler will be replaced.");
        }

        _resolvers[regex] = options;
    }

    /// <summary>
    /// Find the most specific handler for a given path.
    /// </summary>
    /// <param name="path">The path to match against registered routes</param>
    /// <returns>Most specific matching handler or null if no match</returns>
    public RouteHandlerOptions<TEvent, TResult> Resolve(string path)
    {
        // First check cache
        if (_resolverCache.TryGetValue(path, out var cachedHandler))
        {
            return cachedHandler;
        }

        LogDebug($"Resolving handler for path \"{path}\"");

        RouteHandlerOptions<TEvent, TResult> mostSpecificHandler = null;
        int mostSpecificRouteLength = 0;

        foreach (var (pattern, handlerOptions) in _resolvers)
        {
            if (Regex.IsMatch(path, pattern))
            {
                // Calculate specificity (length of path minus wildcard)
                int specificityLength = handlerOptions.Path.Length -
                                       (handlerOptions.Path.EndsWith("*") ? 1 : 0);

                if (specificityLength > mostSpecificRouteLength)
                {
                    mostSpecificRouteLength = specificityLength;
                    mostSpecificHandler = handlerOptions;
                    _resolverCache.Add(path, handlerOptions);
                }
            }
        }

        // Log warning if no handler found
        if (mostSpecificHandler == null && !_warnedPaths.Contains(path))
        {
            LogWarning($"No route handler found for path \"{path}\".");
            _warnedPaths.Add(path);
        }

        return mostSpecificHandler;
    }

    /// <summary>
    /// Find all handlers that match the given path.
    /// Returns them sorted by specificity (most specific first).
    /// </summary>
    /// <param name="path">Path to match</param>
    /// <returns>List of matching handlers in order of specificity</returns>
    public List<RouteHandlerOptions<TEvent, TResult>> ResolveAll(string path)
    {
        var matches = new List<(RouteHandlerOptions<TEvent, TResult> Handler, int Specificity)>();

        foreach (var (pattern, handlerOptions) in _resolvers)
        {
            if (Regex.IsMatch(path, pattern))
            {
                int specificityLength = handlerOptions.Path.Length -
                                      (handlerOptions.Path.EndsWith("*") ? 1 : 0);
                matches.Add((handlerOptions, specificityLength));
            }
        }

        return matches
            .OrderByDescending(x => x.Specificity)
            .Select(x => x.Handler)
            .ToList();
    }

    /// <summary>
    /// Check if a path pattern is valid according to routing rules.
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <returns>Whether the path is valid</returns>
    public static bool IsValidPath(string path)
    {
        if (path == "/*") return true;
        return Regex.IsMatch(path, @"^\/([^\/\*]+)(\/[^\/\*]+)*(\/\*)?$");
    }

    /// <summary>
    /// Converts a path pattern to a regex string for matching.
    /// </summary>
    /// <param name="path">Path pattern to convert</param>
    /// <returns>Regular expression string</returns>
    public static string PathToRegexString(string path)
    {
        string escapedPath = Regex.Escape(path);
        return $"^{escapedPath.Replace("\\*", ".*")}$";
    }

    private void LogDebug(string message)
    {
        Console.WriteLine(message);
    }

    private void LogWarning(string message)
    {
        Console.WriteLine($"Warning: {message}");
    }
}