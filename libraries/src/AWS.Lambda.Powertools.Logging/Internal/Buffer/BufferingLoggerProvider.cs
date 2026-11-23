using System.Collections.Concurrent;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Logger provider that supports buffering logs
/// </summary>
[ProviderAlias("PowertoolsBuffering")]
internal class BufferingLoggerProvider : PowertoolsLoggerProvider
{
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;
    private readonly ConcurrentDictionary<string, PowertoolsBufferingLogger> _loggers = new();

    internal BufferingLoggerProvider(
        PowertoolsLoggerConfiguration config,
        IPowertoolsConfigurations powertoolsConfigurations)
        : base(config, powertoolsConfigurations)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
        // Register with the buffer manager
        LogBufferManager.RegisterProvider(this);

        WarnIfAlcFiltersBufferedLogs();
    }

    /// <summary>
    /// Warning written when buffering is set up while ALC would filter the buffered entries.
    /// </summary>
    internal const string AlcFiltersBufferWarning =
        "Advanced Logging Controls (ALC) log level is less verbose than the log buffering level. Buffered logs will be filtered by ALC.";

    /// <summary>
    /// Warning written when buffered entries are flushed while ALC filters them.
    /// </summary>
    internal const string AlcFiltersFlushWarning =
        "Some buffered logs might be missing because the Advanced Logging Controls (ALC) log level is less verbose than the log buffering level.";

    /// <summary>
    /// Returns true when Advanced Logging Controls are enabled with a level that is less verbose than
    /// the buffer level. Flushed entries below the ALC level are then dropped by Lambda.
    /// </summary>
    internal static bool AlcFiltersBufferedLogs(
        IPowertoolsConfigurations powertoolsConfigurations,
        LogBufferingOptions bufferingOptions)
    {
        if (powertoolsConfigurations == null || bufferingOptions?.Enabled != true)
        {
            return false;
        }

        return powertoolsConfigurations.LambdaLogLevelEnabled() &&
               powertoolsConfigurations.GetLambdaLogLevel() > bufferingOptions.BufferAtLogLevel;
    }

    private void WarnIfAlcFiltersBufferedLogs()
    {
        var config = GetCurrentConfig();
        if (AlcFiltersBufferedLogs(_powertoolsConfigurations, config?.LogBuffering) &&
            OneTimeWarnings.ShouldWrite(AlcFiltersBufferWarning))
        {
            config.LogOutput.WriteLine(AlcFiltersBufferWarning);
        }
    }

    public override ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(
            categoryName,
            name => new PowertoolsBufferingLogger(
                base.CreateLogger(name), // Use the parent's logger creation
                GetCurrentConfig,
                _powertoolsConfigurations));
    }

    /// <summary>
    /// Flush all buffered logs
    /// </summary>
    internal void FlushBuffers()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.FlushBuffer();
        }
    }

    /// <summary>
    /// Clear all buffered logs
    /// </summary>
    internal void ClearBuffers()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.ClearBuffer();
        }
    }

    /// <summary>
    /// Clear buffered logs for the current invocation only
    /// </summary>
    internal void ClearCurrentBuffer()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.ClearCurrentInvocation();
        }
    }

    public override void Dispose()
    {
        // Flush all buffers before disposing
        foreach (var logger in _loggers.Values)
        {
            logger.FlushBuffer();
        }
        
        // Unregister from buffer manager
        LogBufferManager.UnregisterProvider(this);

        _loggers.Clear();
        base.Dispose();
    }
}