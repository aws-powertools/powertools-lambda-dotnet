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
    ///     AsyncLocal storage for per-invocation scope.
    ///     Uses AsyncLocal to ensure keys flow correctly across async/await boundaries.
    ///     
    ///     In Lambda's multi-threaded mode, each invocation starts with a fresh execution
    ///     context, providing natural isolation between concurrent invocations.
    ///     Keys added via AppendKey flow across async/await within the same invocation.
    /// </summary>
    private static readonly AsyncLocal<ConcurrentDictionary<string, object>> _asyncScope = new();

    /// <summary>
    ///     Gets the scope for the current async execution context.
    ///     Creates a new dictionary if one doesn't exist for this context.
    /// </summary>
    /// <value>The scope.</value>
    private static ConcurrentDictionary<string, object> Scope
    {
        get
        {
            var scope = _asyncScope.Value;
            if (scope == null)
            {
                scope = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
                _asyncScope.Value = scope;
            }
            return scope;
        }
    }
    
    /// <summary>
    ///     Creates a new isolated scope for the current execution context.
    ///     Used internally to simulate Lambda invocation isolation in tests.
    /// </summary>
    /// <returns>An IDisposable that restores the previous scope when disposed.</returns>
    internal static IDisposable UseScope()
    {
        return new LoggerScopeContext();
    }

    /// <summary>
    ///     Manages a new isolated scope context.
    /// </summary>
    private sealed class LoggerScopeContext : IDisposable
    {
        private readonly ConcurrentDictionary<string, object> _previousScope;
        private bool _disposed;

        public LoggerScopeContext()
        {
            _previousScope = _asyncScope.Value;
            _asyncScope.Value = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _asyncScope.Value = _previousScope;
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
    ///     Appends a key-value pair to the log context.
    ///     Keys persist across async/await boundaries within the same execution context.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="System.ArgumentNullException">key</exception>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public static void AppendKey(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        
        var convertedValue = PowertoolsLoggerHelpers.ObjectToDictionary(value) ??
                     throw new ArgumentNullException(nameof(value));
        Scope[key] = convertedValue;
    }

    /// <summary>
    ///     Appends multiple keys to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, object>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Appends multiple keys to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, string>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Removes keys from the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void RemoveKeys(params string[] keys)
    {
        if (keys == null) return;
        foreach (var key in keys)
            Scope.TryRemove(key, out _);
    }

    /// <summary>
    ///     Returns all keys added to the log context.
    ///     Returns a snapshot to ensure thread-safety during enumeration.
    /// </summary>
    /// <returns>IEnumerable&lt;KeyValuePair&lt;System.String, System.Object&gt;&gt;.</returns>
    public static IEnumerable<KeyValuePair<string, object>> GetAllKeys()
    {
        // Return a snapshot to avoid concurrent modification issues
        return Scope.ToArray();
    }

    /// <summary>
    ///     Removes all keys from the log context for the current execution context.
    /// </summary>
    internal static void RemoveAllKeys()
    {
        _asyncScope.Value?.Clear();
    }
    
    /// <summary>
    ///     Removes a key from the log context.
    /// </summary>
    public static void RemoveKey(string key)
    {
        Scope.TryRemove(key, out _);
    }

    /// <summary>
    ///     Adds temporary keys to the log context that are automatically removed when disposed.
    ///     Safe to use across async/await boundaries.
    /// </summary>
    /// <param name="keys">The keys to add temporarily.</param>
    /// <returns>An IDisposable that removes the keys when disposed.</returns>
    /// <remarks>
    ///     <para>
    ///         <b>Important:</b> If a key already exists in the context, it will be overwritten
    ///         and then removed when the scope is disposed. The original value is NOT restored.
    ///     </para>
    ///     <para>
    ///         For example, if "orderId" = "A" exists and you create a scope with "orderId" = "B",
    ///         disposing the scope will remove "orderId" entirely, not restore it to "A".
    ///     </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using (Logger.ExtraKeys(new Dictionary&lt;string, object&gt; { {"orderId", "123"} }))
    /// {
    ///     await ProcessOrderAsync();
    ///     Logger.LogInformation("Order processed"); // includes orderId
    /// }
    /// // orderId is automatically removed
    /// </code>
    /// </example>
    public static IDisposable ExtraKeys(IEnumerable<KeyValuePair<string, object>> keys)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        return new LoggerExtraKeysScope(keys);
    }

    /// <summary>
    ///     Adds temporary keys to the log context that are automatically removed when disposed.
    ///     Safe to use across async/await boundaries.
    /// </summary>
    /// <param name="keys">The keys to add temporarily as tuples.</param>
    /// <returns>An IDisposable that removes the keys when disposed.</returns>
    /// <remarks>
    ///     <para>
    ///         <b>Important:</b> If a key already exists in the context, it will be overwritten
    ///         and then removed when the scope is disposed. The original value is NOT restored.
    ///     </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using (Logger.ExtraKeys(("orderId", "123"), ("customerId", "456")))
    /// {
    ///     Logger.LogInformation("Processing"); // includes orderId and customerId
    /// }
    /// </code>
    /// </example>
    public static IDisposable ExtraKeys(params (string Key, object Value)[] keys)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        return new LoggerExtraKeysScope(keys.Select(k => new KeyValuePair<string, object>(k.Key, k.Value)));
    }

    /// <summary>
    ///     Scope that manages temporary logging keys.
    ///     Keys are added on construction and removed on disposal.
    /// </summary>
    private sealed class LoggerExtraKeysScope : IDisposable
    {
        private readonly string[] _keysToRemove;
        private bool _disposed;

        public LoggerExtraKeysScope(IEnumerable<KeyValuePair<string, object>> keys)
        {
            var keyList = new List<string>();
            
            foreach (var (key, value) in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                
                AppendKey(key, value);
                keyList.Add(key);
            }
            
            _keysToRemove = keyList.ToArray();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            RemoveKeys(_keysToRemove);
        }
    }
}
