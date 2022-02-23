using System;
using System.Collections.Generic;

namespace AWS.Lambda.Powertools.Logging.Internal;

internal class PowertoolsLoggerScope : IDisposable
{
    private readonly PowertoolsLogger _logger;
    internal Dictionary<string, object> ExtraKeys { get; }

    internal PowertoolsLoggerScope(PowertoolsLogger logger, Dictionary<string, object> extraKeys)
    {
        _logger = logger;
        ExtraKeys = extraKeys;
    }

    public void Dispose()
    {
        _logger?.EndScope();
    }
}