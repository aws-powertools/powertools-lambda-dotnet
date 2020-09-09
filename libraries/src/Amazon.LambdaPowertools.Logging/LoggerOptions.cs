namespace Amazon.LambdaPowertools.Logging
{
    public class LoggerOptions : ILoggerOptions
    {
        public string Level { get; }
        public double SamplingRate { get; set; }
        public string Service { get; set; }
    }
}