namespace AWS.Lambda.Powertools.Kafka;

public class ConsumerRecord<T>
{
    public string Topic { get; set; }
    public int Partition { get; set; }
    public long Offset { get; set; }
    public long Timestamp { get; set; }
    public string TimestampType { get; set; }
    public string Key { get; set; }
    public T Value { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}