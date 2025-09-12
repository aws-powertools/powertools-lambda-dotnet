

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

/// <summary>
/// Enum representing the different batch event types.
/// </summary>
internal enum BatchEventType
{
    /// <summary>
    /// SQS event.
    /// </summary>
    Sqs,

    /// <summary>
    /// DynamoDB Stream event.
    /// </summary>
    DynamoDbStream,

    /// <summary>
    /// Kinesis Data Stream event.
    /// </summary>
    KinesisDataStream
}
