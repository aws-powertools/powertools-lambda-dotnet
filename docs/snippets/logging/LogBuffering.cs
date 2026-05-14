// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

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
    }
}
// --8<-- [end:buffering_options]

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
    }
}
// --8<-- [end:buffer_at_log_level]

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
    }
}
// --8<-- [end:flush_on_error_log]

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
    }
}
// --8<-- [end:flush_buffer_on_uncaught_error]
