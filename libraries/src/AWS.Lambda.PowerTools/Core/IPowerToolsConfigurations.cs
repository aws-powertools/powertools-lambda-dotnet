namespace AWS.Lambda.PowerTools.Core
{
    public interface IPowerToolsConfigurations
    {
        string ServiceName { get; }
        bool TracerCaptureResponse{ get; }
        bool TracerCaptureError{ get; }
        bool IsSamLocal{ get; }
        
        string GetEnvironmentVariable(string variable);
        string GetEnvironmentVariableOrDefault(string variable, string defaultValue);
        bool GetEnvironmentVariableOrDefault(string variable, bool defaultValue);
    }
}