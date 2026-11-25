using System;
using System.Collections.Concurrent;

namespace AWS.Lambda.Powertools.Logging.Internal.Helpers;

/// <summary>
/// Keeps track of configuration warnings that should be written once per execution environment.
/// The state is static because providers and loggers can be recreated during the lifetime of the
/// environment, for example when the Logging attribute or Logger.Configure applies new settings.
/// </summary>
internal static class OneTimeWarnings
{
    private static readonly ConcurrentDictionary<string, byte> Written = new(StringComparer.Ordinal);

    /// <summary>
    /// Records the warning and returns true the first time it is seen, so the caller writes it once.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <returns>True if the warning has not been written before.</returns>
    internal static bool ShouldWrite(string message)
    {
        return Written.TryAdd(message, 0);
    }

    /// <summary>
    /// Forgets the warnings written so far. Used by tests.
    /// </summary>
    internal static void Reset()
    {
        Written.Clear();
    }
}
