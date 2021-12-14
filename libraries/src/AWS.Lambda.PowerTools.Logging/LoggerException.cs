using System;

namespace AWS.Lambda.PowerTools.Logging
{
    public class LoggerException : Exception
    {
        public LoggerException(string message) : base(message)
        {
        }
    }
}