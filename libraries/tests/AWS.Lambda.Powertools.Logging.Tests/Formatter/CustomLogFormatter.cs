using System;

namespace AWS.Lambda.Powertools.Logging.Tests.Formatter;

public class CustomLogFormatter : ILogFormatter
{
    public object FormatLogEntry(LogEntry logEntry)
    {
        return new
        {
            Message = logEntry.Message,
            Service = logEntry.Service,
            CorrelationIds = new
            {
                AwsRequestId = logEntry.LambdaContext?.AwsRequestId,
                XRayTraceId = logEntry.XRayTraceId,
                CorrelationId = logEntry.CorrelationId
            },
            LambdaFunction = new
            {
                Name = logEntry.LambdaContext?.FunctionName,
                Arn = logEntry.LambdaContext?.InvokedFunctionArn,
                MemoryLimitInMB = logEntry.LambdaContext?.MemoryLimitInMB,
                Version = logEntry.LambdaContext?.FunctionVersion,
                ColdStart = true,
            },
            Level = logEntry.Level.ToString(),
            Timestamp = new DateTime(2024, 1, 1).ToString("o"),
            Logger = new
            {
                Name = logEntry.Name,
                SampleRate = logEntry.SamplingRate
            },
        };
    }
}