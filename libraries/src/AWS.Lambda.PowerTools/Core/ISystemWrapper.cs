namespace AWS.Lambda.PowerTools.Core
{
    public interface ISystemWrapper
    {
        string GetEnvironmentVariable(string variable);
        void Log(string value);
        void LogLine(string value);
        double GetRandom();
    }
}