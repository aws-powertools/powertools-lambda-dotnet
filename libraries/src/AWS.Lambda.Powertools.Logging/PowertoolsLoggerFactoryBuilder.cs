using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

public class PowertoolsLoggerFactoryBuilder
{
    private readonly PowertoolsLoggerConfiguration _configuration = new();

    // public PowertoolsLoggerFactoryBuilder UseEnvironmentVariables(bool enabled)
    // {
    //     _configuration.UseEnvironmentVariables = enabled;
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactoryBuilder SetLogLevelColor(AppLogLevel level, ConsoleColor color)
    // {
    //     _configuration.LogLevelToColorMap[level] = color;
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactoryBuilder SetEventId(int eventId)
    // {
    //     _configuration.EventId = eventId;
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactoryBuilder SetJsonOptions(JsonSerializerOptions options)
    // {
    //     _configuration.JsonOptions = options;
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactoryBuilder UseJsonOutput(bool enabled = true)
    // {
    //     _configuration.UseJsonOutput = enabled;
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactoryBuilder SetTimestampFormat(string format)
    // {
    //     _configuration.TimestampFormat = format;
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactoryBuilder UseJsonContext(JsonSerializerContext context)
    // {
    //     _configuration.JsonContext = context;
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactoryBuilder AddJsonContext(JsonSerializerContext context)
    // {
    //     _configuration.AddJsonContext(context);
    //     return this;
    // }
    //
    // public PowertoolsLoggerFactory Build()
    // {
    //     var factory = LoggerFactory.Create(builder =>
    //     {
    //         builder.AddPowertoolsLogger(config =>
    //         {
    //             config.UseEnvironmentVariables = _configuration.UseEnvironmentVariables;
    //             config.EventId = _configuration.EventId;
    //             config.JsonOptions = _configuration.JsonOptions;
    //             config.UseJsonOutput = _configuration.UseJsonOutput;     // Add this line
    //             config.TimestampFormat = _configuration.TimestampFormat; // Add this line
    //             config.JsonContext = _configuration.JsonContext;
    //
    //             foreach (var kvp in _configuration.LogLevelToColorMap)
    //             {
    //                 config.LogLevelToColorMap[kvp.Key] = kvp.Value;
    //             }
    //         });
    //     });
    //
    //     Logger.Configure(factory); // Configure the static logger
    //     return new PowertoolsLoggerFactory(factory);
    // }
}