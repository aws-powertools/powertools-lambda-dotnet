namespace AWS.Lambda.PowerTools.Core
{
    public interface ISystemWrapper
    {
        string GetEnvironmentVariable(string variable);
    }
}