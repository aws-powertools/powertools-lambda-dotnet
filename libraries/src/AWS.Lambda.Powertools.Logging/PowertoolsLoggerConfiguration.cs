using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Serializers;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Configuration for the Powertools Logger.
/// </summary>
///
/// <example>
///     Basic logging configuration:
///     <code>
///     builder.Logging.AddPowertoolsLogger(options => 
///     {
///         options.Service = "OrderService";
///         options.MinimumLogLevel = LogLevel.Information;
///         options.LoggerOutputCase = LoggerOutputCase.CamelCase;
///     });
///     </code>
///     
///     Using with log buffering:
///     <code>
///     builder.Logging.AddPowertoolsLogger(options => 
///     {
///         options.LogBuffering.Enabled = true;
///         options.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
///         options.LogBuffering.FlushOnErrorLog = true;
///     });
///     </code>
///     
///     Custom JSON formatting:
///     <code>
///     builder.Logging.AddPowertoolsLogger(options => 
///     {
///         options.JsonOptions = new JsonSerializerOptions
///         {
///             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
///             WriteIndented = true
///         };
///     });
///     </code>
/// </example>
public class PowertoolsLoggerConfiguration : IOptions<PowertoolsLoggerConfiguration>
{
    /// <summary>
    ///     The configuration section name used when retrieving configuration from appsettings.json
    ///     or other configuration providers.
    /// </summary>
    public const string ConfigurationSectionName = "AWS.Lambda.Powertools.Logging.Logger";

    /// <summary>
    ///     Specifies the service name that will be added to all logs to improve discoverability.
    ///     This value can also be set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
    /// </summary>
    /// <example>
    ///     <code>
    ///     options.Service = "OrderProcessingService";
    ///     </code>
    /// </example>
    public string Service { get; set; } = null;

    /// <summary>
    ///     Defines the format for timestamps in log entries. Supports standard .NET date format strings.
    ///     When not specified, the default ISO 8601 format is used.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Use specific format
    ///     options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
    ///     
    ///     // Use ISO 8601 with milliseconds
    ///     options.TimestampFormat = "o";
    ///     </code>
    /// </example>
    public string TimestampFormat { get; set; }

    /// <summary>
    ///     Defines the minimum log level that will be processed by the logger.
    ///     Messages below this level will be ignored. Defaults to LogLevel.None, which means
    ///     the minimum level is determined by other configuration mechanisms.
    ///     This can also be set using the environment variable <c>POWERTOOLS_LOG_LEVEL</c>.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Only log warnings and above
    ///     options.MinimumLogLevel = LogLevel.Warning;
    ///     
    ///     // Log everything including trace messages
    ///     options.MinimumLogLevel = LogLevel.Trace;
    ///     </code>
    /// </example>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.None;

    /// <summary>
    ///     Sets a percentage (0.0 to 1.0) of logs that will be dynamically elevated to DEBUG level,
    ///     allowing for production debugging without increasing log verbosity for all requests.
    ///     This can also be set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Sample 10% of logs to DEBUG level
    ///     options.SamplingRate = 0.1;
    ///     
    ///     // Sample 100% (all logs) to DEBUG level
    ///     options.SamplingRate = 1.0;
    ///     </code>
    /// </example>
    public double SamplingRate { get; set; }

    /// <summary>
    ///     Controls the case format used for log field names in the JSON output.
    ///     Available options are Default, CamelCase, PascalCase, or SnakeCase.
    ///     This can also be set using the environment variable <c>POWERTOOLS_LOGGER_CASE</c>.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Use camelCase for JSON field names
    ///     options.LoggerOutputCase = LoggerOutputCase.CamelCase;
    ///     
    ///     // Use snake_case for JSON field names
    ///     options.LoggerOutputCase = LoggerOutputCase.SnakeCase;
    ///     </code>
    /// </example>
    public LoggerOutputCase LoggerOutputCase { get; set; } = LoggerOutputCase.Default;

    /// <summary>
    /// Internal key used for log level in output
    /// </summary>
    internal string LogLevelKey { get; set; } = "level";

    /// <summary>
    ///     Provides a custom log formatter implementation to control how log entries are formatted.
    ///     Set this to override the default JSON formatting with your own custom format.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Use a custom formatter implementation
    ///     options.LogFormatter = new MyCustomLogFormatter();
    ///     
    ///     // Example with a simple custom formatter class this will just return a string:
    ///     public class MyCustomLogFormatter : ILogFormatter
    ///     {
    ///         public object FormatLog(LogEntry entry)
    ///         {
    ///             // Custom formatting logic here
    ///             return $"{logEntry.Timestamp}: [{logEntry.Level}] {logEntry.Message}";
    ///         }
    ///     }
    ///     // Example with a complete formatter class this will just return a json object:
    ///     public object FormatLogEntry(LogEntry logEntry)
    ///     {
    ///         return new
    ///         {
    ///             Message = logEntry.Message,
    ///             Service = logEntry.Service,
    ///             CorrelationIds = new
    ///             {
    ///                 AwsRequestId = logEntry.LambdaContext?.AwsRequestId,
    ///                 XRayTraceId = logEntry.XRayTraceId,
    ///                 CorrelationId = logEntry.CorrelationId
    ///             },
    ///             LambdaFunction = new
    ///             {
    ///                 Name = logEntry.LambdaContext?.FunctionName,
    ///                 Arn = logEntry.LambdaContext?.InvokedFunctionArn,
    ///                 MemoryLimitInMB = logEntry.LambdaContext?.MemoryLimitInMB,
    ///                 Version = logEntry.LambdaContext?.FunctionVersion,
    ///                 ColdStart = true,
    ///             },
    ///             Level = logEntry.Level.ToString(),
    ///             Timestamp = new DateTime(2024, 1, 1).ToString("o"),
    ///             Logger = new
    ///             {
    ///                 Name = logEntry.Name,
    ///                 SampleRate = logEntry.SamplingRate
    ///             },
    ///     };
    ///     }
    ///     </code>
    /// </example>
    public ILogFormatter LogFormatter { get; set; }

    private JsonSerializerOptions _jsonOptions;

    /// <summary>
    ///     Configures the JSON serialization options used when converting log entries to JSON.
    ///     This allows customization of property naming, indentation, and other serialization behaviors.
    ///     Setting this property automatically updates the internal serializer.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // DictionaryNamingPolicy allows you to control the naming policy for dictionary keys
    ///     options.JsonOptions = new JsonSerializerOptions
    ///     {
    ///         DictionaryNamingPolicy = JsonNamingPolicy.CamelCase
    ///     };
    ///     // Pretty-print JSON logs with indentation
    ///     options.JsonOptions = new JsonSerializerOptions
    ///     {
    ///         WriteIndented = true,
    ///         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    ///     };
    ///     
    ///     // Configure to ignore null values in output
    ///     options.JsonOptions = new JsonSerializerOptions
    ///     {
    ///         DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    ///     };
    ///     </code>
    /// </example>
    public JsonSerializerOptions JsonOptions
    {
        get => _jsonOptions;
        set
        {
            _jsonOptions = value;
            if (_jsonOptions != null && _serializer != null)
            {
                _serializer.SetOptions(_jsonOptions);
            }
        }
    }

    /// <summary>
    /// Configuration options for log buffering. Logs below the specified level will be buffered
    /// until the buffer is flushed or an error occurs.
    /// Buffer logs at the WARNING, INFO, and DEBUG levels and reduce CloudWatch costs by decreasing the number of emitted log messages.
    /// <para></para>
    /// Log buffering is disabled by default. Set <see cref="LogBufferingOptions.Enabled"/> to true to enable it.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Enable buffering with default options
    ///     options.LogBuffering.Enabled = true;
    ///     
    ///     // Enable buffering for debug logs with custom settings
    ///     options.LogBuffering.Enabled = true;
    ///     options.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
    ///     options.LogBuffering.FlushOnErrorLog = true;
    ///     
    ///     // Buffer all logs below Error level
    ///     options.LogBuffering.Enabled = true;
    ///     options.LogBuffering.BufferAtLogLevel = LogLevel.Warning;
    ///     </code>
    /// </example>
    public LogBufferingOptions LogBuffering { get; set; } = new LogBufferingOptions();

    /// <summary>
    /// Serializer instance for this configuration
    /// </summary>
    private PowertoolsLoggingSerializer _serializer;

    /// <summary>
    /// Gets the serializer instance for this configuration
    /// </summary>
    internal PowertoolsLoggingSerializer Serializer => _serializer ??= InitializeSerializer();

    /// <summary>
    ///     Specifies the console output wrapper used for writing logs. This property allows
    ///     redirecting log output for testing or specialized handling scenarios.
    ///     Defaults to standard console output via ConsoleWrapper.
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Using TestLoggerOutput
    ///     options.LogOutput = new TestLoggerOutput();
    ///
    ///     // Custom console output for testing
    ///     options.LogOutput = new TestConsoleWrapper();
    ///     
    ///     // Example implementation for testing:
    ///     public class TestConsoleWrapper : IConsoleWrapper
    ///     {
    ///         public List&lt;string&gt; CapturedOutput { get; } = new();
    ///         
    ///         public void WriteLine(string message)
    ///         {
    ///             CapturedOutput.Add(message);
    ///         }
    ///     }
    ///     </code>
    /// </example>
    public IConsoleWrapper LogOutput { get; set; } = new ConsoleWrapper();

    /// <summary>
    /// Initialize serializer with the current configuration
    /// </summary>
    private PowertoolsLoggingSerializer InitializeSerializer()
    {
        var serializer = new PowertoolsLoggingSerializer();
        if (_jsonOptions != null)
        {
            serializer.SetOptions(_jsonOptions);
        }

        serializer.ConfigureNamingPolicy(LoggerOutputCase);
        return serializer;
    }

    // IOptions implementation
    PowertoolsLoggerConfiguration IOptions<PowertoolsLoggerConfiguration>.Value => this;

    internal string XRayTraceId { get; set; }
    internal bool LogEvent { get; set; }

    internal int SamplingRefreshCount { get; set; } = 0;
    internal LogLevel InitialLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    ///     Gets random number
    /// </summary>
    /// <returns>System.Double.</returns>
    internal virtual double GetRandom()
    {
        return GetSafeRandom();
    }

    /// <summary>
    ///   Refresh the sampling calculation and update the minimum log level if needed
    /// </summary>
    /// <returns>True if debug sampling was enabled, false otherwise</returns>
    internal bool RefreshSampleRateCalculation()
    {
        return RefreshSampleRateCalculation(out _);
    }

    /// <summary>
    ///   Refresh the sampling calculation and update the minimum log level if needed
    /// </summary>
    /// <param name="samplerValue"></param>
    /// <returns>True if debug sampling was enabled, false otherwise</returns>
    internal bool RefreshSampleRateCalculation(out double samplerValue)
    {
        samplerValue = 0.0;

        if (SamplingRate <= 0)
            return false;

        // Increment counter at the beginning for proper cold start protection
        SamplingRefreshCount++;

        // Skip first call for cold start protection
        if (SamplingRefreshCount == 1)
        {
            return false;
        }

        var shouldEnableDebugSampling = ShouldEnableDebugSampling(out samplerValue);

        if (shouldEnableDebugSampling && MinimumLogLevel > LogLevel.Debug)
        {
            MinimumLogLevel = LogLevel.Debug;
            return true;
        }
        else if (!shouldEnableDebugSampling)
        {
            MinimumLogLevel = InitialLogLevel;
        }

        return shouldEnableDebugSampling;
    }


    internal bool ShouldEnableDebugSampling()
    {
        return ShouldEnableDebugSampling(out _);
    }

    internal bool ShouldEnableDebugSampling(out double samplerValue)
    {
        samplerValue = 0.0;
        if (SamplingRate <= 0) return false;

        samplerValue = GetRandom();
        return samplerValue <= SamplingRate;
    }

    internal static double GetSafeRandom()
    {
        var randomGenerator = RandomNumberGenerator.Create();
        byte[] data = new byte[4];
        randomGenerator.GetBytes(data);
        uint randomUInt = BitConverter.ToUInt32(data, 0);
        return (double)randomUInt / uint.MaxValue;
    }
}