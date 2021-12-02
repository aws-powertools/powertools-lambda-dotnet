using System;

namespace AWS.Lambda.PowerTools.Core
{
    public class SystemWrapper : ISystemWrapper
    {
        private static ISystemWrapper _instance;
        public static ISystemWrapper Instance => _instance ??= new SystemWrapper();

        private SystemWrapper() { }
        
        public string GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
    }
}