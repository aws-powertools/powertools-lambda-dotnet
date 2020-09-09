namespace Amazon.LambdaPowertools.Logging
{
    public interface ILogEntry {
        bool ColdStart { get; set; }
        string FunctionArn { get; set; }
        string FunctionName { get; set; }
        string FunctionMemorySize { get; set; }
        string FunctionRequestId { get; set; }
        string Level { get; set; }
        string Location { get; set; }
        string Message { get; set; }
        double SamplingRate { get; set; }
        string Timestamp { get; set; }
    }
}