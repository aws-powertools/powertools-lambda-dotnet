using System.Collections;
using System.Text.Json;

namespace AWS.Lambda.Powertools.Kafka;

public class ConsumerRecords<T> : IEnumerable<ConsumerRecord<T>>
{
    public string EventSource { get; set; }
    public string EventSourceArn { get; set; }
    public string BootstrapServers { get; set; }
    internal Dictionary<string, List<ConsumerRecord<T>>> Records { get; set; } = new();
    
    public IEnumerator<ConsumerRecord<T>> GetEnumerator()
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