using System.Collections;
using System.Text.Json;

namespace AWS.Lambda.Powertools.Kafka;

public class KafkaEvent<T> : IEnumerable<KafkaRecord<T>>
{
    public string EventSource { get; set; }
    public string EventSourceArn { get; set; }
    public string BootstrapServers { get; set; }
    internal Dictionary<string, List<KafkaRecord<T>>> Records { get; set; } = new();
    
    public IEnumerator<KafkaRecord<T>> GetEnumerator()
    {
        foreach (var topicRecords in Records)
        {
            foreach (var record in topicRecords.Value)
            {
                yield return record;
            }
        }
    }
    
    // Implement non-generic IEnumerable (required)
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
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