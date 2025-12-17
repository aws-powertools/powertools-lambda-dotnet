using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;

namespace AWS.Lambda.Powertools.Logging;

public static partial class Logger
{
    /// <summary>
    ///     Thread-safe dictionary for per-thread scope storage.
    ///     Uses ManagedThreadId as key to ensure isolation when Lambda processes
    ///     multiple concurrent requests (AWS_LAMBDA_MAX_CONCURRENCY > 1).
    /// </summary>
    private static readonly ConcurrentDictionary<int, Dictionary<string, object>> _threadScopes = new();

    /// <summary>
    ///     Gets the scope for the current thread.
    ///     Creates a new dictionary if one doesn't exist for this thread.
    /// </summary>
    /// <value>The scope.</value>
    private static IDictionary<string, object> Scope
    {
        get
        {
            var threadId = Environment.CurrentManagedThreadId;
            return _threadScopes.GetOrAdd(threadId, _ => new Dictionary<string, object>(StringComparer.Ordinal));
        }
    }

    /// <summary>
    ///     Gets the correlation identifier from the log context.
    /// </summary>
    /// <value>The correlation identifier, or null if not set.</value>
    public static string CorrelationId
    {
        get
        {
            if (Scope.TryGetValue(LoggingConstants.KeyCorrelationId, out var value))
            {
                return value?.ToString();
            }
            return null;
        }
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="System.ArgumentNullException">key</exception>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public static void AppendKey(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
            
        Scope[key] = PowertoolsLoggerHelpers.ObjectToDictionary(value) ??
                     throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, object>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, string>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Remove additional keys from the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void RemoveKeys(params string[] keys)
    {
        if (keys == null) return;
        foreach (var key in keys)
            if (Scope.ContainsKey(key))
                Scope.Remove(key);
    }

    /// <summary>
    ///     Returns all additional keys added to the log context.
    /// </summary>
    /// <returns>IEnumerable&lt;KeyValuePair&lt;System.String, System.Object&gt;&gt;.</returns>
    public static IEnumerable<KeyValuePair<string, object>> GetAllKeys()
    {
        return Scope.AsEnumerable();
    }

    /// <summary>
    ///     Removes all additional keys from the log context for the current thread.
    /// </summary>
    internal static void RemoveAllKeys()
    {
        var threadId = Environment.CurrentManagedThreadId;
        if (_threadScopes.TryGetValue(threadId, out var scope))
        {
            scope.Clear();
        }
    }
    
    /// <summary>
    ///     Removes a key from the log context.
    /// </summary>
    public static void RemoveKey(string key)
    {
        if (Scope.ContainsKey(key))
            Scope.Remove(key);
    }
}
