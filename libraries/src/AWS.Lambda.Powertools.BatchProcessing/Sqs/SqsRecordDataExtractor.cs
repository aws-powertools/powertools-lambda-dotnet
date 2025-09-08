

using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// Extracts data from SQS message records for deserialization.
/// </summary>
public class SqsRecordDataExtractor : IRecordDataExtractor<SQSEvent.SQSMessage>
{
    /// <summary>
    /// The singleton instance of the SQS record data extractor.
    /// </summary>
    public static readonly SqsRecordDataExtractor Instance = new();

    /// <summary>
    /// Extracts the message body from an SQS message record.
    /// </summary>
    /// <param name="record">The SQS message record.</param>
    /// <returns>The message body string.</returns>
    public string ExtractData(SQSEvent.SQSMessage record)
    {
        return record?.Body ?? string.Empty;
    }
}