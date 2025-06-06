namespace AWS.Lambda.Powertools.Kafka;

/// <summary>
/// Represents a single record consumed from a Kafka topic.
/// </summary>
/// <typeparam name="T">The type of the record's value.</typeparam>
public class ConsumerRecord<T>
{
    /// <summary>
    /// Gets the Kafka topic name from which the record was consumed.
    /// </summary>
    public string Topic { get; internal set; }

    /// <summary>
    /// Gets the Kafka partition from which the record was consumed.
    /// </summary>
    public int Partition { get; internal set; }

    /// <summary>
    /// Gets the offset of the record within its Kafka partition.
    /// </summary>
    public long Offset { get; internal set; }

    /// <summary>
    /// Gets the timestamp of the record (typically in Unix time).
    /// </summary>
    public long Timestamp { get; internal set; }

    /// <summary>
    /// Gets the type of timestamp (e.g., "CREATE_TIME" or "LOG_APPEND_TIME").
    /// </summary>
    public string TimestampType { get; internal set; }

    /// <summary>
    /// Gets the key of the record (often used for partitioning).
    /// </summary>
    public string Key { get; internal set; }

    /// <summary>
    /// Gets the deserialized value of the record.
    /// </summary>
    public T Value { get; internal set; }

    /// <summary>
    /// Gets the headers associated with the record.
    /// </summary>
    public Dictionary<string, string> Headers { get; internal set; }
}
