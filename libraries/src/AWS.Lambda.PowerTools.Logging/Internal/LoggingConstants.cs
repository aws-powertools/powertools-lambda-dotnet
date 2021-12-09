using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    internal static class LoggingConstants
    {
        internal const LogLevel DefaultLogLevel = LogLevel.Information;
        internal const string KeyColdStart = "ColdStart";
        internal const string KeyFunctionName = "FunctionName";
        internal const string KeyFunctionVersion = "FunctionVersion";
        internal const string KeyFunctionMemorySize = "FunctionMemorySize";
        internal const string KeyFunctionArn = "FunctionArn";
        internal const string KeyFunctionRequestId = "FunctionRequestId";
        internal const string KeyXRayTraceId = "xray_trace_id";
        internal const string KeyCorrelationId = "CorrelationId";
        internal const string KeyTimestamp = "Timestamp";
        internal const string KeyLogLevel = "Level";
        internal const string KeyServiceName = "Service";
        internal const string KeyLoggerName = "Name";
        internal const string KeyMessage = "Message";
        internal const string KeySamplingRate = "SamplingRate";
        internal const string KeyException = "Exception";
        internal const string KeyState = "State";
    }
}