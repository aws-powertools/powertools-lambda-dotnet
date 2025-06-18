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

namespace AWS.Lambda.Powertools.Kafka;

/// <summary>
/// Represents a single record consumed from a Kafka topic.
/// </summary>
/// <typeparam name="T">The type of the record's value.</typeparam>
/// <typeparam name="TK">The type of the key value</typeparam>
/// <example>
/// <code>
/// var record = new ConsumerRecord&lt;Customer&gt;
/// {
///     Topic = "customers",
///     Partition = 0,
///     Offset = 42,
///     Value = new Customer { Id = 123, Name = "John Doe" }
/// };
/// </code>
/// </example>
public class ConsumerRecord<TK, T>
{
    /// <summary>
    /// Gets or sets the Kafka topic name from which the record was consumed.
    /// </summary>
    public string Topic { get; internal set; } = null!;

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
    public string TimestampType { get; internal set; } = null!;

    /// <summary>
    /// Gets the key of the record (often used for partitioning).
    /// </summary>
    public TK Key { get; internal set; } = default!;

    /// <summary>
    /// Gets the deserialized value of the record.
    /// </summary>
    public T Value { get; internal set; } = default!;

    /// <summary>
    /// Gets the headers associated with the record.
    /// </summary>
    public Dictionary<string, byte[]> Headers { get; internal set; } = null!;
}