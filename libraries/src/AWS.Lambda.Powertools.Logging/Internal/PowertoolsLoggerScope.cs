using System;
using System.Collections.Generic;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class PowertoolsLoggerScope.
/// </summary>
internal class PowertoolsLoggerScope : IDisposable
{
    /// <summary>
    ///     The associated logger
    /// </summary>
    private readonly PowertoolsLogger _logger;
    
    /// <summary>
    ///     The provided extra keys
    /// </summary>
    internal Dictionary<string, object> ExtraKeys { get; }

    /// <summary>
    ///     Creates a PowertoolsLoggerScope object
    /// </summary>
    internal PowertoolsLoggerScope(PowertoolsLogger logger, Dictionary<string, object> extraKeys)
    {
        _logger = logger;
        ExtraKeys = extraKeys;
    }

    /// <summary>
    ///     Implements IDisposable interface
    /// </summary>
    public void Dispose()
    {
        _logger?.EndScope();
    }
}