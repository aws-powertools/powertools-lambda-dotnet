namespace Amazon.LambdaPowertools.Logging
{
    public class LogEntry {
        public bool ColdStart { get; set; }
        public string FunctionArn { get; set; }
        public string FunctionName { get; set; }
        public string FunctionMemorySize { get; set; }
        public string FunctionRequestId { get; set; }
        public LogLevel Level { get; set; }
        public string Location { get; set; }
        public string Message { get; set; }
        public double SamplingRate { get; set; }
        public string Timestamp { get; set; }
    }
}