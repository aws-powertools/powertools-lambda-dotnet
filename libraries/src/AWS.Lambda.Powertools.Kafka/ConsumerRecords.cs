/*
 * Copyright JsonCons.Net authors. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Collections;

namespace AWS.Lambda.Powertools.Kafka;

/// <summary>
/// Represents a collection of Kafka consumer records that can be enumerated.
/// Contains event metadata and records organized by topics.
/// </summary>
/// <typeparam name="T">The type of the record values from the event.</typeparam>
/// <typeparam name="TK">The type of Key values from the event.</typeparam>
public class ConsumerRecords<TK, T> : IEnumerable<ConsumerRecord<TK, T>>
{
    /// <summary>
    /// Gets the event source (typically "aws:kafka").
    /// </summary>
    public string EventSource { get; internal set; } = null!;

    /// <summary>
    /// Gets the ARN of the event source (MSK cluster or Self-managed Kafka).
    /// </summary>
    public string EventSourceArn { get; internal set; } = null!;

    /// <summary>
    /// Gets the Kafka bootstrap servers connection string.
    /// </summary>
    public string BootstrapServers { get; internal set; } = null!;

    internal Dictionary<string, List<ConsumerRecord<TK, T>>> Records { get; set; } = new();

    /// <summary>
    /// Returns an enumerator that iterates through all consumer records across all topics.
    /// </summary>
    /// <returns>An enumerator of ConsumerRecord&lt;T&gt; objects.</returns>
    public IEnumerator<ConsumerRecord<TK, T>> GetEnumerator()
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