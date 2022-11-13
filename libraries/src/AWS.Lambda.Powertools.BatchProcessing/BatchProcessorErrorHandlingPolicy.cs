namespace AWS.Lambda.Powertools.BatchProcessing;

public enum BatchProcessorErrorHandlingPolicy
{
    ContinueOnBatchItemFailure,
    StopOnFirstBatchItemFailure
}
