namespace AWS.Lambda.PowerTools.Logging
{
    public static class CorrelationIdPaths
    {
        public const string API_GATEWAY_REST = "RequestContext.RequestId";
        public const string API_GATEWAY_HTTP = "RequestContext.RequestId";
        public const string APPLICATION_LOAD_BALANCER = "Headers.x-amzn-trace-id";
        public const string EVENT_BRIDGE = "Id";
    }
}