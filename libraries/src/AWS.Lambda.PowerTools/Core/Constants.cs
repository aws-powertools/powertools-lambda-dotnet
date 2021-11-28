namespace AWS.Lambda.PowerTools.Core
{
    public static class Constants
    {
        public const string TRACER_CAPTURE_RESPONSE_ENV = "POWERTOOLS_TRACER_CAPTURE_RESPONSE";
        public const string TRACER_CAPTURE_ERROR_ENV = "POWERTOOLS_TRACER_CAPTURE_ERROR";
        public const string TRACER_DISABLED_ENV = "POWERTOOLS_TRACE_DISABLED";
        public const string LOGGER_LOG_SAMPLING_RATE = "POWERTOOLS_LOGGER_SAMPLE_RATE";
        public const string LOGGER_LOG_EVENT_ENV = "POWERTOOLS_LOGGER_LOG_EVENT";
        public const string LOGGER_LOG_DEDUPLICATION_ENV = "POWERTOOLS_LOG_DEDUPLICATION_DISABLED";
        public const string MIDDLEWARE_FACTORY_TRACE_ENV = "POWERTOOLS_TRACE_MIDDLEWARES";
        public const string METRICS_NAMESPACE_ENV = "POWERTOOLS_METRICS_NAMESPACE";
        public const string SAM_LOCAL_ENV = "AWS_SAM_LOCAL";
        public const string CHALICE_LOCAL_ENV = "AWS_CHALICE_CLI_MODE";
        public const string SERVICE_NAME_ENV = "POWERTOOLS_SERVICE_NAME";
        public const string XRAY_TRACE_ID_ENV = "_X_AMZN_TRACE_ID";
    }
}