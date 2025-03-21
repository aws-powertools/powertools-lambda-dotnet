using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal.Helpers;

/// <summary>
/// Extension methods for handling configuration copying between PowertoolsLogger configurations
/// </summary>
internal static class ConfigurationExtensions
{
    /// <summary>
    /// Copies configuration values from source to destination configuration
    /// </summary>
    /// <param name="destination">The destination configuration to copy values to</param>
    /// <param name="source">The source configuration to copy values from</param>
    /// <returns>The updated destination configuration</returns>
    public static PowertoolsLoggerConfiguration CopyFrom(this PowertoolsLoggerConfiguration destination, PowertoolsLoggerConfiguration source)
    {
        destination.Service = source.Service;
        destination.SamplingRate = source.SamplingRate;
        destination.MinimumLogLevel = source.MinimumLogLevel;
        destination.LoggerOutputCase = source.LoggerOutputCase;
        destination.LoggerOutput = source.LoggerOutput;
        destination.JsonOptions = source.JsonOptions;
        destination.TimestampFormat = source.TimestampFormat;
        destination.LogFormatter = source.LogFormatter;
        destination.LogLevelKey = source.LogLevelKey;
        destination.LogBufferingOptions = source.LogBufferingOptions;
        
        return destination;
    }
}