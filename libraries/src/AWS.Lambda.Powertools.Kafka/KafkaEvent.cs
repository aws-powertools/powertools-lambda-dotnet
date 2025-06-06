using System.Text.Json;

namespace AWS.Lambda.Powertools.Kafka;

public class KafkaEvent<T>
{
    public string EventSource { get; set; }
    public string EventSourceArn { get; set; }
    public string BootstrapServers { get; set; }
    public Dictionary<string, List<KafkaRecord<T>>> Records { get; set; } = new();
}

public class KafkaRecord<T>
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