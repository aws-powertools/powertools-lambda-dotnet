namespace AWS.Lambda.PowerTools.Core
{
    public class PowerToolsConfigurations : IPowerToolsConfigurations
    {
        public const int MaxDimensions = 9;
        public const int MaxMetrics = 100;
        
        private static IPowerToolsConfigurations _instance;
        public static IPowerToolsConfigurations Instance => _instance ??= new PowerToolsConfigurations(SystemWrapper.Instance);

        private readonly ISystemWrapper _systemWrapper;

        internal PowerToolsConfigurations(ISystemWrapper systemWrapper)
        {
            _systemWrapper = systemWrapper;
        }

        public string GetEnvironmentVariable(string variable)
        {
            return _systemWrapper.GetEnvironmentVariable(variable);
        }
        
        public string GetEnvironmentVariableOrDefault(string variable, string defaultValue)
        {
            var result = _systemWrapper.GetEnvironmentVariable(variable);
            return string.IsNullOrWhiteSpace(result) ? defaultValue : result;
        }

        public bool GetEnvironmentVariableOrDefault(string variable, bool defaultValue)
        {
            return bool.TryParse(_systemWrapper.GetEnvironmentVariable(variable), out var result)
                ? result
                : defaultValue;
        }

        public string ServiceName =>
            GetEnvironmentVariableOrDefault(Constants.SERVICE_NAME_ENV, "service_undefined");

        public bool IsServiceNameDefined =>
            !string.IsNullOrWhiteSpace(GetEnvironmentVariable(Constants.SERVICE_NAME_ENV));

        public bool TracerCaptureResponse =>
            GetEnvironmentVariableOrDefault(Constants.TRACER_CAPTURE_RESPONSE_ENV, true);
        
        public bool TracerCaptureError =>
            GetEnvironmentVariableOrDefault(Constants.TRACER_CAPTURE_ERROR_ENV, true);
        
        public bool IsSamLocal =>
            GetEnvironmentVariableOrDefault(Constants.SAM_LOCAL_ENV, false);
        
        public string MetricsNamespace =>
            GetEnvironmentVariable(Constants.METRICS_NAMESPACE_ENV);
        
        public string LogLevel =>
            GetEnvironmentVariable(Constants.LOG_LEVEL_NAME_ENV);

        public double? LoggerSampleRate =>
            double.TryParse(_systemWrapper.GetEnvironmentVariable(Constants.LOGGER_SAMPLE_RATE_NAME_ENV), out var result)
                ? result
                : null;
    }
}