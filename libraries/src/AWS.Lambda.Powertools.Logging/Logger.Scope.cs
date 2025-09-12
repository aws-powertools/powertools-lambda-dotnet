using System;
using System.Collections.Generic;
using System.Linq;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;

namespace AWS.Lambda.Powertools.Logging;

public static partial class Logger
{
    /// <summary>
    ///     Gets the scope.
    /// </summary>
    /// <value>The scope.</value>
    private static IDictionary<string, object> Scope { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

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
    ///     Removes all additional keys from the log context.
    /// </summary>
    internal static void RemoveAllKeys()
    {
        Scope.Clear();
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
