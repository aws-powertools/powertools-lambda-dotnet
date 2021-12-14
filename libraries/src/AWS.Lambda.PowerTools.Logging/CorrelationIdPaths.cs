namespace AWS.Lambda.PowerTools.Logging
{
    /// <summary>
    /// Supported Event types from which Correlation ID can be extracted
    /// </summary>
    public static class CorrelationIdPaths
    {
        internal const char Separator = '/';
        
        /// <summary>
        /// To use when function is expecting API Gateway Rest API Request event
        /// </summary>
        public const string API_GATEWAY_REST = "/RequestContext/RequestId";
        
        /// <summary>
        /// To use when function is expecting API Gateway HTTP API Request event
        /// </summary>
        public const string API_GATEWAY_HTTP = "/RequestContext/RequestId";
        
        /// <summary>
        /// To use when function is expecting Application Load balancer Request event
        /// </summary>
        public const string APPLICATION_LOAD_BALANCER = "/Headers/x-amzn-trace-id";
        
        /// <summary>
        /// To use when function is expecting Event Bridge Request event
        /// </summary>
        public const string EVENT_BRIDGE = "/Id";
    }
}