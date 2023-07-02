namespace AWS.Lambda.Powertools.BatchProcessing;

public enum BatchProcessorErrorHandlingPolicy
{
    DeriveFromEvent,
    ContinueOnBatchItemFailure,
    StopOnFirstBatchItemFailure
}
