using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace Docs.LoggingSnippets.LogBuffering.BufferingOptions
{
// --8<-- [start:buffering_options]
public class Function
{
    public Function()
    {
      Logger.Configure(logger =>
      {
          logger.Service = "MyServiceName";
          logger.LogBuffering.Enabled = true;
          logger.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
          logger.LogBuffering.MaxBytes = 20480; // Default is 20KB (20480 bytes)
          logger.LogBuffering.FlushOnErrorLog = true; // default true
      });

      Logger.LogDebug("This is a debug message"); // This is NOT buffered
    }

    [Logging]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Logger.LogDebug("This is a debug message"); // This is buffered
        Logger.LogInformation("This is an info message");

        // your business logic here

        Logger.LogError("This is an error message"); // This also flushes the buffer
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:buffering_options]
}

namespace Docs.LoggingSnippets.LogBuffering.BufferAtLogLevel
{
// --8<-- [start:buffer_at_log_level]
public class Function
{
    public Function()
    {
      Logger.Configure(logger =>
      {
          logger.Service = "MyServiceName";
          logger.LogBuffering.Enabled = true;
          logger.LogBuffering.BufferAtLogLevel = LogLevel.Warning;
      });
    }

    [Logging]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
      // All logs below are buffered
      Logger.LogDebug("This is a debug message");
      Logger.LogInformation("This is an info message");
      Logger.LogWarning("This is a warn message");

      Logger.ClearBuffer(); // This will clear the buffer without emitting the logs
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:buffer_at_log_level]
}

namespace Docs.LoggingSnippets.LogBuffering.FlushOnErrorLog
{
// --8<-- [start:flush_on_error_log]
public class Function
{
    public Function()
    {
      Logger.Configure(logger =>
      {
          logger.Service = "MyServiceName";
          logger.LogBuffering.Enabled = true;
          logger.LogBuffering.FlushOnErrorLog = false;
      });
    }

    [Logging]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
      Logger.LogDebug("This is a debug message"); // this is buffered

      try
      {
          throw new Exception();
      }
      catch (Exception e)
      {
          Logger.LogError(e.Message); // this does NOT flush the buffer
      }

      Logger.LogDebug("Debug!!"); // this is buffered

      try
      {
          throw new Exception();
      }
      catch (Exception e)
      {
          Logger.LogError(e.Message); // this does NOT flush the buffer
          Logger.FlushBuffer(); // Manually flush
      }
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:flush_on_error_log]
}

namespace Docs.LoggingSnippets.LogBuffering.FlushBufferOnUncaughtError
{
// --8<-- [start:flush_buffer_on_uncaught_error]
public class Function
{
    public Function()
    {
      Logger.Configure(logger =>
      {
          logger.Service = "MyServiceName";
          logger.LogBuffering.Enabled = true;
          logger.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
      });
    }

    [Logging(FlushBufferOnUncaughtError = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
      Logger.LogDebug("This is a debug message");

      throw new Exception(); // This causes the buffer to be flushed
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:flush_buffer_on_uncaught_error]
}
