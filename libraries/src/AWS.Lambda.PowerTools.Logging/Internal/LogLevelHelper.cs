using System;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    internal class LogLevelHelper
    {
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        internal const LogLevel DefaultLogLevel = LogLevel.Information;

        internal LogLevelHelper(IPowerToolsConfigurations powerToolsConfigurations)
        {
            _powerToolsConfigurations = powerToolsConfigurations;
        }

        internal LogLevel GetLogLevel(LogLevel? logLevel = null)
        {
            if (logLevel.HasValue) 
                return logLevel.Value;
            
            if (Enum.TryParse((_powerToolsConfigurations.LogLevel ?? "").Trim(), true, out LogLevel result))
                return result;

            return DefaultLogLevel;
        }
    }
}