namespace Amazon.LambdaPowertools.Logging
{
    public class LoggerOptions : ILoggerOptions
    {
        public LogLevel Level { get; }
        public double SamplingRate { get; set; }
        public string Service { get; set; }
    }
}