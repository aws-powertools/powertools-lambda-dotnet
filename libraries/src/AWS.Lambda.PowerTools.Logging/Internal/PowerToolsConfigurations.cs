using System;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    public static class PowerToolsConfigurationsExtension
    {
        public static LogLevel GetLogLevel(this IPowerToolsConfigurations powerToolsConfigurations, LogLevel? logLevel = null)
        {
            if (logLevel.HasValue) 
                return logLevel.Value;
            
            if (Enum.TryParse((powerToolsConfigurations.LogLevel ?? "").Trim(), true, out LogLevel result))
                return result;

            return LoggingConstants.DefaultLogLevel;
        }
    }
}