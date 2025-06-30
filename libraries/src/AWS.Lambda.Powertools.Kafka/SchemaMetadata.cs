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

#if KAFKA_JSON
namespace AWS.Lambda.Powertools.Kafka.Json;
#elif KAFKA_AVRO
namespace AWS.Lambda.Powertools.Kafka.Avro;
#elif KAFKA_PROTOBUF
namespace AWS.Lambda.Powertools.Kafka.Protobuf;
#else
namespace AWS.Lambda.Powertools.Kafka;
#endif

/// <summary>
/// Represents metadata about the schema used for serializing the record's value or key.
/// </summary>
public class SchemaMetadata
{
    /// <summary>
    /// Gets or sets the format of the data (e.g., "JSON", "AVRO" "Protobuf").
    /// /// </summary>
    public string DataFormat { get; internal set; } = null!;
    
    /// <summary>
    /// Gets or sets the schema ID associated with the record's value or key.
    /// </summary>
    public string SchemaId { get; internal set; } = null!;
}