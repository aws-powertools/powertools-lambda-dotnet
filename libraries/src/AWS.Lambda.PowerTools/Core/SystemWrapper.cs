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
        
        public void Log(string value)
        {
            Console.Write(value);
        }
        
        public void LogLine(string value)
        {
            Console.WriteLine(value);
        }

        public double GetRandom()
        {
            return new Random().NextDouble();
        }
    }
}