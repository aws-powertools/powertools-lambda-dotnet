namespace AWS.Lambda.PowerTools.Core
{
    public interface IPowerToolsConfigurations
    {
        string Service { get; }
        bool IsServiceDefined { get; }
        bool TracerCaptureResponse { get; }
        bool TracerCaptureError { get; }
        bool IsSamLocal { get; }
        string MetricsNamespace { get; }
        string LogLevel { get; }
        double? LoggerSampleRate { get; }
        bool LoggerLogEvent { get; }
        string XRayTraceId { get; }
        
        string GetEnvironmentVariable(string variable);
        string GetEnvironmentVariableOrDefault(string variable, string defaultValue);
        bool GetEnvironmentVariableOrDefault(string variable, bool defaultValue);
    }
}