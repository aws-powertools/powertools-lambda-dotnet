using System;
using System.Reflection;
using System.Text.Json;

namespace Amazon.LambdaPowertools.Logging
{
    public class Logger
    {
        private bool _coldStart;
        public Logger()
        {
        }

        public void Log(string message, LogLevel logLevel = LogLevel.Information)
        {
            LogEntry logEntry = new LogEntry()
            {
                ColdStart = _coldStart,
                FunctionArn = "functionArn",
                FunctionName = "functionName",
                FunctionMemorySize = "FunctionMemorySize",
                FunctionRequestId = "FunctionRequestId",
                Level = logLevel,
                Location = Assembly.GetCallingAssembly().FullName,
                Message = message,
                SamplingRate = 0.0,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            
            Console.WriteLine(JsonSerializer.Serialize(logEntry));
        }
    }
}