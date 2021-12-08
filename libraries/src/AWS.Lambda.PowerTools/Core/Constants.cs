namespace AWS.Lambda.PowerTools.Core
{
    internal static class Constants
    {
        internal const string SERVICE_NAME_ENV = "POWERTOOLS_SERVICE_NAME";
        internal const string SAM_LOCAL_ENV = "AWS_SAM_LOCAL";
        internal const string TRACER_CAPTURE_RESPONSE_ENV = "POWERTOOLS_TRACER_CAPTURE_RESPONSE";
        internal const string TRACER_CAPTURE_ERROR_ENV = "POWERTOOLS_TRACER_CAPTURE_ERROR";
        internal const string METRICS_NAMESPACE_ENV = "POWERTOOLS_METRICS_NAMESPACE";
        internal const string LOG_LEVEL_NAME_ENV = "LOG_LEVEL";
        internal const string LOGGER_SAMPLE_RATE_NAME_ENV = "POWERTOOLS_LOGGER_SAMPLE_RATE";
        internal const string LOGGER_LOG_EVENT_NAME_ENV = "POWERTOOLS_LOGGER_LOG_EVENT";
    }
}